using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace MediaCompany.Pilot.Web;

/// <summary>
/// THE START-UP (design decisions D-001 and D-003). One instance per application folder: a second start opens the running instance's
/// address and exits. The listener is bound to 127.0.0.1 in code; after start the bound addresses are read back and the app stops with
/// exit 4 if any is not 127.0.0.1. The ready line "MC17-WEB-READY http://127.0.0.1:port" is printed only after that check.
/// </summary>
public static class Program
{
    public const int DefaultPort = 47817;
    public const string ReadyPrefix = "MC17-WEB-READY ";
    public const int ExitUsage = 1;
    public const int ExitAddress = 4;

    public static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = new UTF8Encoding(false);
        if (!WebOptions.TryParse(args, out var options))
        {
            Console.Out.Write(WebOptions.Usage);
            return ExitUsage;
        }

        Directory.CreateDirectory(options.AppFolder);
        var mutexName = "MediaCompanyPilotWeb-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(options.AppFolder.ToUpperInvariant())))[..32];
        using var mutex = new Mutex(initiallyOwned: false, mutexName);
        bool owned;
        try
        {
            owned = mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            owned = true;
        }

        var runningFile = Path.Combine(options.AppFolder, AppRecordStore.RunningFileName);
        if (!owned)
        {
            var address = File.Exists(runningFile) ? File.ReadAllText(runningFile).Trim() : string.Empty;
            Console.Out.WriteLine($"Ứng dụng đã đang chạy ở {address}. Mở trang đó trong trình duyệt.");
            if (options.OpenBrowser && address.StartsWith("http://127.0.0.1:", StringComparison.Ordinal))
            {
                OpenBrowser(address);
            }

            return 0;
        }

        // The mutex is not released by hand: the awaits may resume on another thread, and closing the handle at exit frees it.
        return await Serve(options, runningFile);
    }

    private static async Task<int> Serve(WebOptions options, string runningFile)
    {
        var guard = new GuardState();
        var web = new WebContext(options, guard);
        var port = options.Port ?? web.Record?.Port ?? DefaultPort;

        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = [], ContentRootPath = AppContext.BaseDirectory });
        builder.Logging.ClearProviders();
        builder.WebHost.ConfigureKestrel(kestrel =>
        {
            kestrel.AddServerHeader = false;
            kestrel.Listen(IPAddress.Loopback, port);
        });

        await using var app = builder.Build();
        Routes.Map(app, web);
        try
        {
            await app.StartAsync();
        }
        catch (IOException error)
        {
            Console.Out.WriteLine($"LỖI ỨNG DỤNG [port-busy]: cổng {port} đang bị chương trình khác dùng ({error.Message}). Đóng chương trình đó, hoặc chạy lại với --port <số khác>.");
            return ExitAddress;
        }

        var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()?.Addresses ?? [];
        var bound = new List<int>();
        foreach (var address in addresses)
        {
            var uri = new Uri(address);
            if (!IPAddress.TryParse(uri.Host, out var ip) || !ip.Equals(IPAddress.Loopback))
            {
                Console.Out.WriteLine($"LỖI ỨNG DỤNG [address-not-local]: ứng dụng nghe ở {address}, không phải 127.0.0.1. Ứng dụng dừng lại. Báo người phụ trách kỹ thuật.");
                await app.StopAsync();
                return ExitAddress;
            }

            bound.Add(uri.Port);
        }

        if (bound.Count != 1)
        {
            Console.Out.WriteLine("LỖI ỨNG DỤNG [address-not-local]: không đọc được địa chỉ ứng dụng đang nghe. Ứng dụng dừng lại. Báo người phụ trách kỹ thuật.");
            await app.StopAsync();
            return ExitAddress;
        }

        guard.Port = bound[0];
        var url = guard.Origin + "/";
        File.WriteAllText(runningFile, url, new UTF8Encoding(false));
        Console.Out.WriteLine(ReadyPrefix + guard.Origin);
        Console.Out.WriteLine($"Ứng dụng thí điểm đang chạy ở {url} (chỉ máy này mở được). Đóng cửa sổ này để tắt ứng dụng.");
        if (options.OpenBrowser)
        {
            OpenBrowser(url);
        }

        try
        {
            await app.WaitForShutdownAsync();
        }
        finally
        {
            File.Delete(runningFile);
        }

        return 0;
    }

    /// <summary>Opens the operator's default browser on the app's own loopback address; nothing else is ever opened.</summary>
    private static void OpenBrowser(string url)
    {
        try
        {
            using var _ = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Console.Out.WriteLine($"Không tự mở được trình duyệt. Mở trình duyệt và gõ {url}");
        }
    }
}
