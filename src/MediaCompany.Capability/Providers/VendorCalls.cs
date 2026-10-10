using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediaCompany.Credentials;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Capability.Providers;

/// <summary>
/// The one send discipline every vendor path shares (the production change, decision D-002 of its design):
/// the request is fully assembled before the credential is attached; the response body is read ONCE into one
/// buffer; and every outcome whose charge the vendor did not state is marked unknown, so it is booked at the
/// admitted worst case and never at zero units as a measurement. Nothing here logs, and no message it builds
/// carries a header, a body or a secret.
/// </summary>
internal static partial class VendorCalls
{
    /// <summary>The ruled length a screened vendor error text is truncated to (the Design Gate, 2026-10-10): a configured amount.</summary>
    internal const int ScreenedLength = 200;

    /// <summary>What replaces a removed secret, a bearer value or a token-shaped string.</summary>
    internal const string Redacted = "[redacted]";

    /// <summary>What one send returned: the status, the one body buffer, the declared media type and the elapsed time.</summary>
    internal sealed record Sent(HttpStatusCode Status, byte[] Body, string? MediaType, TimeSpan Elapsed);

    /// <summary>
    /// Attaches the credential and sends. Returns the response, or a finished attempt where nothing could be
    /// known: a credential refusal before sending (charge known, nothing sent), a transport failure or a timeout
    /// (charge unknown, because the request may have reached the vendor).
    /// </summary>
    internal static async Task<(Sent? Response, ProviderAttempt? Ended)> SendAsync(
        HttpClient http,
        ICredentialExchange exchange,
        ScopedHandle handle,
        HttpRequestMessage message,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var attached = await exchange.AttachAsync(handle, message, cancellationToken).ConfigureAwait(false);
        if (attached is CredentialOutcome.Refused refused)
        {
            stopwatch.Stop();
            return (null, new ProviderAttempt(
                false, UnitCounts.None, CostBasis.Measurement, stopwatch.Elapsed,
                $"credential refused before sending: {refused.Reason}", ProviderFailureSignal.Transient)
            {
                ChargeKnown = true,
            });
        }

        try
        {
            using var response = await http.SendAsync(message, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            return (new Sent(response.StatusCode, body, response.Content.Headers.ContentType?.MediaType, stopwatch.Elapsed), null);
        }
        catch (HttpRequestException transport)
        {
            stopwatch.Stop();
            return (null, Unknown(stopwatch.Elapsed, $"transport failure ({transport.HttpRequestError}); the request may have reached the vendor", ProviderFailureSignal.Outage));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return (null, Unknown(stopwatch.Elapsed, "the provider-call bound passed before the vendor answered; the vendor may have charged", ProviderFailureSignal.Outage));
        }
    }

    /// <summary>An attempt whose charge the vendor did not state.</summary>
    internal static ProviderAttempt Unknown(TimeSpan elapsed, string reason, ProviderFailureSignal signal) =>
        new(false, UnitCounts.None, CostBasis.Estimate, elapsed, reason + "; the charge is not known", signal)
        {
            ChargeKnown = false,
        };

    /// <summary>
    /// A non-success status: failed, its charge unknown, its signal mapped, and THE VENDOR'S CODE OR MESSAGE SCREENED (the
    /// own-voice change, decision D-013 of its design). The text joins the status in the one failure reason every sink
    /// receives — the availability record, the operation record, the audit entry, the stage summary and the console — so
    /// each receives only the screened text: every value the credential exchange attached to the sent message removed,
    /// bearer values and token-shaped strings redacted, control characters stripped, then truncated to the ruled length.
    /// </summary>
    internal static ProviderAttempt NonSuccess(Sent sent, HttpRequestMessage message) =>
        Unknown(sent.Elapsed, $"the vendor returned status {(int)sent.Status}: {ErrorText(sent.Body, Attached(message))}", sent.Status switch
        {
            HttpStatusCode.TooManyRequests => ProviderFailureSignal.RateLimited,
            HttpStatusCode.PaymentRequired => ProviderFailureSignal.QuotaExhausted,
            HttpStatusCode.ServiceUnavailable => ProviderFailureSignal.Outage,
            HttpStatusCode.BadGateway => ProviderFailureSignal.Outage,
            HttpStatusCode.GatewayTimeout => ProviderFailureSignal.Outage,
            _ => ProviderFailureSignal.Transient,
        });

    /// <summary>Every value the sent message carries in its request headers — the exchange attached the credential there — and each bearer value alone.</summary>
    internal static IReadOnlyList<string> Attached(HttpRequestMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var values = message.Headers.SelectMany(h => h.Value).Where(v => !string.IsNullOrEmpty(v)).ToList();
        if (message.Headers.Authorization is { Parameter: { Length: > 0 } parameter })
        {
            values.Add(parameter);
        }

        // The longest first, so a value containing another is removed whole.
        return values.Distinct(StringComparer.Ordinal).OrderByDescending(v => v.Length).ToArray();
    }

    /// <summary>
    /// The vendor's error code or message from the one body buffer, screened and truncated: the first string among an error
    /// object's code, type and message, or the top-level code, message and detail, of a JSON body; otherwise the body itself
    /// where it is UTF-8 text without control characters; otherwise a statement that nothing readable was returned.
    /// </summary>
    internal static string ErrorText(byte[] body, IReadOnlyList<string> secrets)
    {
        ArgumentNullException.ThrowIfNull(body);
        ArgumentNullException.ThrowIfNull(secrets);
        if (body.Length == 0)
        {
            return "no error code or message was readable (empty body)";
        }

        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(body);
        }
        catch (DecoderFallbackException)
        {
            return "no error code or message was readable (not text)";
        }

        if (text.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')))
        {
            return "no error code or message was readable (not text)";
        }

        var chosen = JsonCode(text) ?? text;
        return Screen(chosen, secrets);
    }

    /// <summary>Removes every attached value, redacts bearer values and token-shaped runs, strips control characters, then truncates.</summary>
    internal static string Screen(string text, IReadOnlyList<string> secrets)
    {
        var screened = text;
        foreach (var secret in secrets.Where(s => s.Length > 0))
        {
            screened = screened.Replace(secret, Redacted, StringComparison.Ordinal);
        }

        screened = Bearer().Replace(screened, "Bearer " + Redacted);
        screened = DottedToken().Replace(screened, Redacted);
        screened = TokenRun().Replace(screened, m => m.Value.Any(char.IsLetter) && m.Value.Any(char.IsDigit) ? Redacted : m.Value);
        screened = new string(screened.Select(c => char.IsControl(c) ? ' ' : c).ToArray()).Trim();
        return screened.Length > ScreenedLength ? screened[..ScreenedLength] : screened;
    }

    private static string? JsonCode(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (root.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString();
                }

                if (error.ValueKind == JsonValueKind.Object && First(error, "code", "type", "message") is { } inner)
                {
                    return inner;
                }
            }

            return First(root, "code", "message", "detail");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? First(JsonElement element, params string[] names) =>
        names.Select(n => element.TryGetProperty(n, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    [GeneratedRegex(@"Bearer\s+\S+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex Bearer();

    [GeneratedRegex(@"[A-Za-z0-9_-]{4,}\.[A-Za-z0-9_-]{4,}\.[A-Za-z0-9_-]{4,}", RegexOptions.CultureInvariant)]
    private static partial Regex DottedToken();

    [GeneratedRegex(@"[A-Za-z0-9_.\-]{20,}", RegexOptions.CultureInvariant)]
    private static partial Regex TokenRun();

    /// <summary>A request whose payload is not the one this vendor path takes: nothing is sent, so the charge is known to be none.</summary>
    internal static ProviderAttempt WrongPayload(string path) =>
        new(false, UnitCounts.None, CostBasis.Measurement, TimeSpan.Zero,
            $"the request carries no payload the {path} path takes; nothing was sent", ProviderFailureSignal.Transient)
        {
            ChargeKnown = true,
        };

    /// <summary>A JSON request body, built before any credential exists on the message.</summary>
    internal static HttpRequestMessage Json(Uri endpoint, object body)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, endpoint);
        message.Content = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(body));
        message.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        return message;
    }

    /// <summary>Reads a non-negative integer usage member, or null where it is absent or not a number.</summary>
    internal static long? Usage(JsonElement usage, string name) =>
        usage.ValueKind == JsonValueKind.Object
        && usage.TryGetProperty(name, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt64(out var count)
        && count >= 0
            ? count
            : null;

    /// <summary>The UTF-8 text of a string, as the bytes a text content carries.</summary>
    internal static byte[] Utf8(string text) => Encoding.UTF8.GetBytes(text);
}
