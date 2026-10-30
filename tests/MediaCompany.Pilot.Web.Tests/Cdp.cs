using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>
/// A minimal DevTools protocol client over the loopback debugging port of the digest-checked headless shell (MC-19 measurement and
/// button cases). Base class library only: one <see cref="ClientWebSocket"/> per target on 127.0.0.1, commands sent one at a time, events
/// skipped. The shell is started with <c>--remote-debugging-port=0</c> and writes the port it chose into its profile folder.
/// </summary>
internal sealed class Cdp : IDisposable
{
    private readonly ClientWebSocket _socket = new();
    private int _next;

    private Cdp(Uri address)
    {
        _socket.Options.Proxy = null;
        _socket.ConnectAsync(address, CancellationToken.None).Wait(TimeSpan.FromSeconds(20));
        Expect.True(_socket.State == WebSocketState.Open, $"DevTools socket {address} open");
    }

    /// <summary>The debugging port the shell wrote to its profile folder, and its browser target path.</summary>
    public static (int Port, string BrowserPath) Port(string profile, Process browser)
    {
        var file = Path.Combine(profile, "DevToolsActivePort");
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(30) && !browser.HasExited)
        {
            try
            {
                var lines = File.ReadAllLines(file);
                if (lines.Length >= 2 && int.TryParse(lines[0], out var port))
                {
                    return (port, lines[1]);
                }
            }
            catch (IOException)
            {
                // Not written yet.
            }

            Thread.Sleep(100);
        }

        throw new CaseFailure("the headless shell wrote no DevToolsActivePort");
    }

    public static Cdp Browser(int port, string path) => new(new Uri($"ws://127.0.0.1:{port}{path}"));

    /// <summary>The page target whose address starts with the given text.</summary>
    public static Cdp Page(int port, string addressStart)
    {
        using var http = new HttpClient(new SocketsHttpHandler { UseProxy = false });
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(30))
        {
            var list = JsonNode.Parse(http.GetStringAsync($"http://127.0.0.1:{port}/json/list").Result)!.AsArray();
            foreach (var target in list)
            {
                if ((string?)target!["type"] == "page" && ((string?)target["url"] ?? string.Empty).StartsWith(addressStart, StringComparison.Ordinal))
                {
                    return new Cdp(new Uri((string)target["webSocketDebuggerUrl"]!));
                }
            }

            Thread.Sleep(200);
        }

        throw new CaseFailure($"no page target at {addressStart}");
    }

    /// <summary>The protocol events received while waiting for answers (for example Network.requestWillBeSent once Network is enabled).</summary>
    public List<JsonNode> Events { get; } = [];

    /// <summary>The page's network requests seen so far, as "METHOD address".</summary>
    public IReadOnlyList<string> Requests()
    {
        Send("Runtime.evaluate", new { expression = "1" });
        lock (Events)
        {
            return [.. Events.Where(e => (string?)e["method"] == "Network.requestWillBeSent")
                .Select(e => $"{(string?)e["params"]?["request"]?["method"]} {(string?)e["params"]?["request"]?["url"]}")];
        }
    }

    /// <summary>Sends one command and returns its result; a protocol error fails the case.</summary>
    public JsonNode Send(string method, object? parameters = null)
    {
        var id = Interlocked.Increment(ref _next);
        var message = JsonSerializer.Serialize(new { id, method, @params = parameters ?? new { } });
        _socket.SendAsync(Encoding.UTF8.GetBytes(message), WebSocketMessageType.Text, true, CancellationToken.None).Wait(TimeSpan.FromSeconds(20));
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(60))
        {
            var node = Receive();
            if (node?["method"] is not null && node["id"] is null)
            {
                lock (Events)
                {
                    Events.Add(node);
                }

                continue;
            }

            if (node?["id"] is { } got && (int)got == id)
            {
                if (node["error"] is { } error)
                {
                    throw new CaseFailure($"DevTools {method}: {error.ToJsonString()}");
                }

                return node["result"] ?? new JsonObject();
            }
        }

        throw new CaseFailure($"DevTools {method}: no answer");
    }

    /// <summary>Evaluates an expression in the page (awaiting a promise) and returns its value as JSON.</summary>
    public JsonNode? Eval(string expression, bool gesture = false)
    {
        var result = Send("Runtime.evaluate", new { expression, awaitPromise = true, returnByValue = true, userGesture = gesture });
        if (result["exceptionDetails"] is { } failure)
        {
            throw new CaseFailure($"page script failed: {failure.ToJsonString()}");
        }

        return result["result"]?["value"];
    }

    public string Text(string expression, bool gesture = false) => Eval(expression, gesture)?.ToString() ?? string.Empty;

    /// <summary>Waits until the expression is true in the page.</summary>
    public void Until(string expression, string what, double seconds = 20)
    {
        var watch = Stopwatch.StartNew();
        while (watch.Elapsed < TimeSpan.FromSeconds(seconds))
        {
            if (Eval($"!!({expression})")?.GetValue<bool>() == true)
            {
                return;
            }

            Thread.Sleep(150);
        }

        throw new CaseFailure($"timed out waiting for {what}");
    }

    /// <summary>A real mouse click at the page point (a user activation, as the operator's press gives).</summary>
    public void Click(double x, double y)
    {
        Send("Input.dispatchMouseEvent", new { type = "mousePressed", x, y, button = "left", clickCount = 1 });
        Send("Input.dispatchMouseEvent", new { type = "mouseReleased", x, y, button = "left", clickCount = 1 });
    }

    /// <summary>Ctrl and a letter, carrying the editing command the key runs in Chrome (copy or paste).</summary>
    public void Chord(string key, string command)
    {
        var code = "Key" + key.ToUpperInvariant();
        var keyCode = (int)char.ToUpperInvariant(key[0]);
        Send("Input.dispatchKeyEvent", new { type = "rawKeyDown", modifiers = 2, key, code, windowsVirtualKeyCode = keyCode, commands = new[] { command } });
        Send("Input.dispatchKeyEvent", new { type = "keyUp", modifiers = 2, key, code, windowsVirtualKeyCode = keyCode });
    }

    private JsonNode? Receive()
    {
        var buffer = new byte[1 << 16];
        using var memory = new MemoryStream();
        while (true)
        {
            var task = _socket.ReceiveAsync(buffer, CancellationToken.None);
            if (!task.Wait(TimeSpan.FromSeconds(60)))
            {
                throw new CaseFailure("DevTools socket: no message in 60 s");
            }

            var got = task.Result;
            if (got.MessageType == WebSocketMessageType.Close)
            {
                throw new CaseFailure("DevTools socket closed");
            }

            memory.Write(buffer, 0, got.Count);
            if (got.EndOfMessage)
            {
                return JsonNode.Parse(memory.ToArray());
            }
        }
    }

    public void Dispose()
    {
        try
        {
            if (_socket.State == WebSocketState.Open)
            {
                _socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None).Wait(TimeSpan.FromSeconds(2));
            }
        }
        catch (AggregateException)
        {
            // The shell may already be gone.
        }

        _socket.Dispose();
    }
}
