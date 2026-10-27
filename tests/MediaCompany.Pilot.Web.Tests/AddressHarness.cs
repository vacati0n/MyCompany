using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>One socket-table row owned by a job member: the owner, its name, the protocol and both addresses.</summary>
internal sealed record SocketRow(int Owner, string OwnerName, string Protocol, IPAddress Local, int LocalPort, IPAddress? Remote, int RemotePort, int State)
{
    public const int Listen = 2;

    /// <summary>A loopback row: the local address is loopback, and the remote one, for TCP, is loopback or the any-address of a listener.</summary>
    public bool IsLoopback =>
        IPAddress.IsLoopback(Local)
        && (Remote is null || IPAddress.IsLoopback(Remote) || (State == Listen && (Remote.Equals(IPAddress.Any) || Remote.Equals(IPAddress.IPv6Any))));

    public override string ToString() =>
        $"{Protocol} {Local}:{LocalPort} -> {(Remote is null ? "-" : $"{Remote}:{RemotePort}")} state {State} owned by {OwnerName} ({Owner})";
}

/// <summary>
/// The MC-16 socket-table harness (tests/MediaCompany.Pilot.Tests/SocketTableHarness.cs, observing by job membership) with the
/// addresses kept: the harness owns a job object with no limits; each process the test starts is assigned to it right after it starts,
/// and every process that one starts is a member from its creation (nested jobs included). Every 20 ms it reads the job's process list,
/// then the machine's TCP and UDP tables (IPv4 and IPv6) with their address columns, and keeps every distinct row owned by a member.
/// Processes are never matched by name. Residual: a socket opened and closed between two samples is not seen.
/// A row is kept only when its owner is in the job's process list read in the same sample (just before or just after the tables), never
/// because the id was a member once: Windows reuses process ids, and a process outside the job that is given the id of a member that has
/// ended (the headless browser, say) is not a member. Each member is recorded once per incarnation (id and start time), so a reused id
/// inside the job is counted as the new process it is.
/// </summary>
/// <summary>One process observed in the harness job: its id, its start time (the incarnation of the id) and its name.</summary>
internal sealed record JobMember(int Id, DateTime Started, string Name);

internal sealed class AddressHarness : IDisposable
{
    private const int InterNetwork = 2;
    private const int InterNetworkV6 = 23;
    private const int TcpOwnerPidAll = 5;
    private const int UdpOwnerPid = 1;
    private const int BasicProcessIdList = 3;
    private const int ListCapacity = 4096;

    private readonly List<JobMember> _members = [];
    private readonly Dictionary<int, JobMember> _current = [];
    private readonly HashSet<string> _seen = [];
    private readonly List<SocketRow> _rows = [];
    private readonly CancellationTokenSource _stop = new();
    private readonly Thread _thread;
    private readonly IntPtr _job;
    private bool _disposed;

    public AddressHarness()
    {
        _job = CreateJobObjectW(IntPtr.Zero, null);
        if (_job == IntPtr.Zero)
        {
            throw new InvalidOperationException($"the harness job object could not be created (error {Marshal.GetLastPInvokeError()})");
        }

        _thread = new Thread(Sample) { IsBackground = true, Name = "address harness" };
        _thread.Start();
    }

    public int Samples { get; private set; }

    public IReadOnlyList<SocketRow> Rows
    {
        get
        {
            lock (_rows)
            {
                return [.. _rows];
            }
        }
    }

    /// <summary>Every process observed in the job, one entry per incarnation (a reused id appears once per process that held it).</summary>
    public IReadOnlyList<JobMember> Members
    {
        get
        {
            lock (_members)
            {
                return [.. _members];
            }
        }
    }

    /// <summary>
    /// The rows a sample keeps: those whose owner is in the job's process list read just before or just after the tables. An id that was
    /// a member earlier but is in neither list belongs to some other process now and is not kept.
    /// </summary>
    internal static IEnumerable<SocketRow> OwnedByMembers(IEnumerable<SocketRow> rows, IReadOnlyCollection<int> before, IReadOnlyCollection<int> after) =>
        rows.Where(row => before.Contains(row.Owner) || after.Contains(row.Owner));

    /// <summary>Test hook for the id-reuse case: records <paramref name="id"/> as a member that has ended, without adding it to the job.</summary>
    internal void RecordEndedMember(int id, string name)
    {
        lock (_members)
        {
            var member = new JobMember(id, DateTime.MinValue, name);
            _members.Add(member);
            _current[id] = member;
        }
    }

    public void Track(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        if (!AssignProcessToJobObject(_job, process.Handle))
        {
            throw new InvalidOperationException($"process {process.Id} could not be assigned to the harness job (error {Marshal.GetLastPInvokeError()})");
        }

        Add(process.Id);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _stop.Cancel();
        _thread.Join();
        _stop.Dispose();
        CloseHandle(_job);
    }

    private void Add(int id)
    {
        DateTime started;
        string name;
        try
        {
            using var process = Process.GetProcessById(id);
            name = process.ProcessName;
            started = process.StartTime;
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            name = "(ended before its name was read)";
            started = DateTime.MinValue;
        }

        lock (_members)
        {
            // An id still listed by the job after its process ended cannot be looked up; that is the member already recorded, not a new one.
            if (_current.TryGetValue(id, out var known) && (known.Started == started || started == DateTime.MinValue))
            {
                return;
            }

            var member = new JobMember(id, started, name);
            _members.Add(member);
            _current[id] = member;
        }
    }

    private string NameOf(int id)
    {
        lock (_members)
        {
            return _current.TryGetValue(id, out var member) ? member.Name : "?";
        }
    }

    private void Sample()
    {
        while (!_stop.IsCancellationRequested)
        {
            var before = JobMembers();
            foreach (var id in before)
            {
                Add(id);
            }

            var table = Tables().ToList();
            var after = JobMembers();
            foreach (var id in after)
            {
                Add(id);
            }

            foreach (var row in OwnedByMembers(table, before.ToHashSet(), after.ToHashSet()))
            {
                var named = row with { OwnerName = NameOf(row.Owner) };
                var key = named.ToString();
                lock (_rows)
                {
                    if (_seen.Add(key))
                    {
                        _rows.Add(named);
                    }
                }
            }

            Samples++;
            Thread.Sleep(20);
        }
    }

    private List<int> JobMembers()
    {
        var ids = new List<int>();
        var size = 8 + (IntPtr.Size * ListCapacity);
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (!QueryInformationJobObject(_job, BasicProcessIdList, buffer, (uint)size, IntPtr.Zero))
            {
                return ids;
            }

            var listed = Marshal.ReadInt32(buffer, 4);
            for (var i = 0; i < listed; i++)
            {
                ids.Add((int)Marshal.ReadIntPtr(buffer, 8 + (i * IntPtr.Size)));
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return ids;
    }

    private static IEnumerable<SocketRow> Tables()
    {
        foreach (var family in new[] { InterNetwork, InterNetworkV6 })
        {
            foreach (var row in Read(family, tcp: true))
            {
                yield return row;
            }

            foreach (var row in Read(family, tcp: false))
            {
                yield return row;
            }
        }
    }

    /// <summary>
    /// The rows of one table. Layouts: MIB_TCPROW_OWNER_PID (24 bytes: state, local address, local port, remote address, remote port,
    /// owner), MIB_TCP6ROW_OWNER_PID (56: local address 16, scope, port, remote address 16, scope, port, state, owner),
    /// MIB_UDPROW_OWNER_PID (12: address, port, owner), MIB_UDP6ROW_OWNER_PID (28: address 16, scope, port, owner). Ports are in
    /// network order in the low two bytes.
    /// </summary>
    private static List<SocketRow> Read(int family, bool tcp)
    {
        var rows = new List<SocketRow>();
        var size = 0;
        _ = tcp ? GetExtendedTcpTable(IntPtr.Zero, ref size, false, family, TcpOwnerPidAll, 0) : GetExtendedUdpTable(IntPtr.Zero, ref size, false, family, UdpOwnerPid, 0);
        size += 8192;
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            var result = tcp ? GetExtendedTcpTable(buffer, ref size, false, family, TcpOwnerPidAll, 0) : GetExtendedUdpTable(buffer, ref size, false, family, UdpOwnerPid, 0);
            if (result != 0)
            {
                return rows;
            }

            var count = Marshal.ReadInt32(buffer);
            var v6 = family == InterNetworkV6;
            var rowSize = (tcp, v6) switch
            {
                (true, false) => 24,
                (true, true) => 56,
                (false, false) => 12,
                (false, true) => 28,
            };
            var protocol = (tcp ? "tcp" : "udp") + (v6 ? "6" : string.Empty);
            for (var i = 0; i < count; i++)
            {
                var at = buffer + 4 + (i * rowSize);
                rows.Add((tcp, v6) switch
                {
                    (true, false) => new SocketRow(Marshal.ReadInt32(at, 20), string.Empty, protocol, V4(at, 4), Port(at, 8), V4(at, 12), Port(at, 16), Marshal.ReadInt32(at, 0)),
                    (true, true) => new SocketRow(Marshal.ReadInt32(at, 52), string.Empty, protocol, V6(at, 0), Port(at, 20), V6(at, 24), Port(at, 44), Marshal.ReadInt32(at, 48)),
                    (false, false) => new SocketRow(Marshal.ReadInt32(at, 8), string.Empty, protocol, V4(at, 0), Port(at, 4), null, 0, 0),
                    (false, true) => new SocketRow(Marshal.ReadInt32(at, 24), string.Empty, protocol, V6(at, 0), Port(at, 20), null, 0, 0),
                });
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return rows;
    }

    private static IPAddress V4(IntPtr at, int offset) => new((uint)Marshal.ReadInt32(at, offset));

    private static IPAddress V6(IntPtr at, int offset)
    {
        var bytes = new byte[16];
        Marshal.Copy(at + offset, bytes, 0, 16);
        return new IPAddress(bytes);
    }

    private static int Port(IntPtr at, int offset)
    {
        var raw = (uint)Marshal.ReadInt32(at, offset);
        return (int)(((raw & 0xFF) << 8) | ((raw >> 8) & 0xFF));
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedTcpTable(IntPtr table, ref int size, [MarshalAs(UnmanagedType.Bool)] bool order, int family, int tableClass, uint reserved);

    [DllImport("iphlpapi.dll", SetLastError = true)]
    private static extern uint GetExtendedUdpTable(IntPtr table, ref int size, [MarshalAs(UnmanagedType.Bool)] bool order, int family, int tableClass, uint reserved);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateJobObjectW(IntPtr attributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryInformationJobObject(IntPtr job, int informationClass, IntPtr information, uint length, IntPtr returnLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
