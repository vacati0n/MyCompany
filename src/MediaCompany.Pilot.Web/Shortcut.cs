using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace MediaCompany.Pilot.Web;

/// <summary>
/// The desktop shortcut (design decision D-013), created only when the operator confirms it at setup. It starts this app's own
/// executable from its build folder; the platform shell's link object writes the file. Nothing is downloaded or registered.
/// </summary>
public static class Shortcut
{
    public const string FileName = "Thí điểm video MediaCompany.lnk";

    public static string Create(string folder, string target)
    {
        if (!File.Exists(target))
        {
            throw new WebRefusal("shortcut-target-missing", $"Không thấy tệp chạy của ứng dụng ở {target}. Báo người phụ trách kỹ thuật.");
        }

        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, FileName);
        var link = (IShellLinkW)new ShellLink();
        try
        {
            link.SetPath(target);
            link.SetWorkingDirectory(Path.GetDirectoryName(target)!);
            link.SetDescription("Mở ứng dụng thí điểm video trên máy này (chỉ địa chỉ 127.0.0.1).");
            ((IPersistFile)link).Save(path, true);
        }
        catch (Exception error) when (error is COMException or UnauthorizedAccessException or IOException)
        {
            throw new WebRefusal("shortcut-failed",
                $"Không tạo được biểu tượng trên màn hình nền ({error.Message}). Cài đặt không bị ảnh hưởng. Mở ứng dụng bằng tệp {Path.GetFileName(target)} trong thư mục {Path.GetDirectoryName(target)}, hoặc thử lại bằng nút tạo biểu tượng ở màn hình chính.");
        }
        finally
        {
            if (OperatingSystem.IsWindows())
            {
                Marshal.FinalReleaseComObject(link);
            }
        }

        return path;
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    private class ShellLink;

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder file, int length, IntPtr data, int flags);
        void GetIDList(out IntPtr list);
        void SetIDList(IntPtr list);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder name, int length);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string name);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder directory, int length);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string directory);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder arguments, int length);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string arguments);
        void GetHotkey(out short key);
        void SetHotkey(short key);
        void GetShowCmd(out int show);
        void SetShowCmd(int show);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder path, int length, out int index);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string path, int index);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string path, int reserved);
        void Resolve(IntPtr window, int flags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string file);
    }
}
