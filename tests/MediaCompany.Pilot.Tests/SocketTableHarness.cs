using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediaCompany.Pilot.Tests;

/// <summary>
/// The test harness of design D-005: while a run is under way it reads the machine's TCP and UDP tables (IPv4 and IPv6) every
/// few milliseconds and keeps every row owned by a watched process: the started toolchain process and any media-tool or probe
/// process. Any row is a socket the run opened.
/// </summary>
internal sealed class SocketTableHarness : IDisposable
{
    private const int InterNetwork = 2;
    private const int InterNetworkV6 = 23;
    private const int TcpOwnerPidAll = 5;
    private const int UdpOwnerPid = 1;
    private static readonly string[] WatchedNames = ["ffmpeg", "ffprobe", "MediaCompany.Pilot"];

    private readonly HashSet<int> _watched = [];
    private readonly List<string> _rows = [];
    private readonly CancellationTokenSource _stop = new();
    private readonly Thread _thread;
    private bool _disposed;

    public SocketTableHarness()
    {
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

    public void Watch(int processId)
    {
        lock (_watched)
        {
            _watched.Add(processId);
        }
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
    }

    private void Sample()
    {
        while (!_stop.IsCancellationRequested)
        {
            foreach (var name in WatchedNames)
            {
                foreach (var process in Process.GetProcessesByName(name))
                {
                    Watch(process.Id);
                    process.Dispose();
                }
            }

            int[] watched;
            lock (_watched)
            {
                watched = [.. _watched];
            }

            foreach (var (protocol, family, owner) in Tables())
            {
                if (watched.Contains(owner))
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
}
