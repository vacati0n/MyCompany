using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MediaCompany.Pilot;

/// <summary>
/// A job object set to end every member when it is closed, the rule ExternalMediaTool keeps for its own tool starts: whatever the
/// started tool starts is in the job too, and closing the job ends the whole set.
/// </summary>
internal sealed class JobObject : IDisposable
{
    private const int ExtendedLimitInformation = 9;
    private const int BasicAccountingInformation = 1;
    private const uint KillOnJobClose = 0x2000;
    private IntPtr _handle;

    private JobObject(IntPtr handle) => _handle = handle;

    public static JobObject Create()
    {
        var handle = CreateJobObjectW(IntPtr.Zero, null);
        if (handle == IntPtr.Zero)
        {
            throw new PilotToolFailure("job", $"a job object could not be created (error {Marshal.GetLastPInvokeError()})");
        }

        var limits = new ExtendedLimits { Basic = new BasicLimits { LimitFlags = KillOnJobClose } };
        if (!SetInformationJobObject(handle, ExtendedLimitInformation, ref limits, (uint)Marshal.SizeOf<ExtendedLimits>()))
        {
            var error = Marshal.GetLastPInvokeError();
            CloseHandle(handle);
            throw new PilotToolFailure("job", $"the job could not be set to end its members on close (error {error})");
        }

        return new JobObject(handle);
    }

    public void Assign(Process process)
    {
        if (!AssignProcessToJobObject(_handle, process.Handle))
        {
            throw new PilotToolFailure("job", $"the process could not be put into its job (error {Marshal.GetLastPInvokeError()})");
        }
    }

    /// <summary>The processes still in the job; -1 where the job cannot be read.</summary>
    public int ActiveProcesses() =>
        _handle != IntPtr.Zero && QueryInformationJobObject(_handle, BasicAccountingInformation, out var accounting, (uint)Marshal.SizeOf<Accounting>(), IntPtr.Zero)
            ? (int)accounting.ActiveProcesses
            : -1;

    public void Close()
    {
        if (_handle != IntPtr.Zero)
        {
            CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }
    }

    public void Dispose() => Close();

    [StructLayout(LayoutKind.Sequential)]
    private struct BasicLimits
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ExtendedLimits
    {
        public BasicLimits Basic;
        public IoCounters Io;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Accounting
    {
        public long TotalUserTime;
        public long TotalKernelTime;
        public long ThisPeriodTotalUserTime;
        public long ThisPeriodTotalKernelTime;
        public uint TotalPageFaultCount;
        public uint TotalProcesses;
        public uint ActiveProcesses;
        public uint TotalTerminatedProcesses;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateJobObjectW(IntPtr attributes, string? name);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetInformationJobObject(IntPtr job, int informationClass, ref ExtendedLimits information, uint length);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryInformationJobObject(IntPtr job, int informationClass, out Accounting information, uint length, IntPtr returned);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr handle);
}
