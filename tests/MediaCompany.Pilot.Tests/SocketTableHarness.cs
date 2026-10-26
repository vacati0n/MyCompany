using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediaCompany.Pilot.Tests;

/// <summary>
/// The test harness of design D-005, observing by job membership (correction request 12). The harness owns a job object with no
/// limits; each toolchain process the test starts is assigned to it right after it starts, and every process that one starts (the
/// media tool and the probe tool, whose own job nests inside this one) is a member from its creation. Every few milliseconds the
/// harness reads the job's process list, then the machine's TCP and UDP tables (IPv4 and IPv6), and keeps every row owned by a
/// member. Any row is a socket the run opened. Processes are never matched by name.
///
/// Residual, recorded in the implementation report: a socket opened and closed between two samples (about 20 ms apart) is not
/// seen, and a process the toolchain could start in the moment between its own start and its assignment is not a member.
/// </summary>
internal sealed class SocketTableHarness : IDisposable
{
    private const int InterNetwork = 2;
    private const int InterNetworkV6 = 23;
    private const int TcpOwnerPidAll = 5;
    private const int UdpOwnerPid = 1;
    private const int BasicProcessIdList = 3;
    private const int ListCapacity = 1024;

    private readonly HashSet<int> _members = [];
    private readonly Dictionary<int, string> _names = [];
    private readonly List<string> _rows = [];
    private readonly CancellationTokenSource _stop = new();
    private readonly Thread _thread;
    private readonly IntPtr _job;
    private bool _disposed;

    public SocketTableHarness()
    {
        _job = CreateJobObjectW(IntPtr.Zero, null);
        if (_job == IntPtr.Zero)
        {
            throw new InvalidOperationException($"the harness job object could not be created (error {Marshal.GetLastPInvokeError()})");
        }

        _thread = new Thread(Sample) { IsBackground = true, Name = "socket table harness" };
        _thread.Start();
    }

    public int Samples { get; private set; }

    public IReadOnlyList<string> Rows
    {
        get
        {
            lock (_rows)
            {
                return [.. _rows];
            }
        }
    }

    /// <summary>The distinct processes seen in the job, by the name each had when first seen.</summary>
    public IReadOnlyList<string> MemberNames
    {
        get
        {
            lock (_members)
            {
                return [.. _names.Values];
            }
        }
    }

    public int MemberCount
    {
        get
        {
            lock (_members)
            {
                return _members.Count;
            }
        }
    }

    /// <summary>Assigns a started process to the harness job; its later children are members from their creation.</summary>
    public void Track(Process process)
    {
        ArgumentNullException.ThrowIfNull(process);
        if (!AssignProcessToJobObject(_job, process.Handle))
        {
            throw new InvalidOperationException($"process {process.Id} could not be assigned to the harness job (error {Marshal.GetLastPInvokeError()})");
        }

        Add(process.Id);
    }

    /// <summary>The positive control: the test process itself joins the job, so a socket it opens must be seen through membership.</summary>
    public void TrackCurrentProcess()
    {
        using var current = Process.GetCurrentProcess();
        Track(current);
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
        lock (_members)
        {
            if (_members.Add(id))
            {
                try
                {
                    using var process = Process.GetProcessById(id);
                    _names[id] = process.ProcessName;
                }
                catch (ArgumentException)
                {
                    _names[id] = "(ended before its name was read)";
                }
                catch (InvalidOperationException)
                {
                    _names[id] = "(ended before its name was read)";
                }
            }
        }
    }

    private void Sample()
    {
        while (!_stop.IsCancellationRequested)
        {
            foreach (var id in JobMembers())
            {
                Add(id);
            }

            int[] members;
            lock (_members)
            {
                members = [.. _members];
            }

            foreach (var (protocol, family, owner) in Tables())
            {
                if (members.Contains(owner))
                {
                    lock (_rows)
                    {
                        _rows.Add($"{protocol}{(family == InterNetworkV6 ? "6" : string.Empty)} owned by process {owner}");
                    }
                }
            }

            Samples++;
            Thread.Sleep(20);
        }
    }

    /// <summary>The process identifiers now in the job, nested jobs' members included.</summary>
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

    private static IEnumerable<(string Protocol, int Family, int Owner)> Tables()
    {
        foreach (var family in new[] { InterNetwork, InterNetworkV6 })
        {
            foreach (var owner in Read(family, tcp: true))
            {
                yield return ("tcp", family, owner);
            }

            foreach (var owner in Read(family, tcp: false))
            {
                yield return ("udp", family, owner);
            }
        }
    }

    /// <summary>The owning process of every row of one table; the owner is the last field of each row layout.</summary>
    private static List<int> Read(int family, bool tcp)
    {
        var owners = new List<int>();
        var size = 0;
        _ = tcp ? GetExtendedTcpTable(IntPtr.Zero, ref size, false, family, TcpOwnerPidAll, 0) : GetExtendedUdpTable(IntPtr.Zero, ref size, false, family, UdpOwnerPid, 0);
        var buffer = Marshal.AllocHGlobal(size + 4096);
        try
        {
            size += 4096;
            var result = tcp ? GetExtendedTcpTable(buffer, ref size, false, family, TcpOwnerPidAll, 0) : GetExtendedUdpTable(buffer, ref size, false, family, UdpOwnerPid, 0);
            if (result != 0)
            {
                return owners;
            }

            var count = Marshal.ReadInt32(buffer);
            var rowSize = (tcp, family) switch
            {
                (true, InterNetwork) => 24,
                (true, _) => 56,
                (false, InterNetwork) => 12,
                (false, _) => 28,
            };
            for (var i = 0; i < count; i++)
            {
                owners.Add(Marshal.ReadInt32(buffer, 4 + (i * rowSize) + rowSize - 4));
            }
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }

        return owners;
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
