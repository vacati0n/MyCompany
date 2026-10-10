using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
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
internal static class VendorCalls
{
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

    /// <summary>A non-success status: failed, its charge unknown, its signal mapped.</summary>
    internal static ProviderAttempt NonSuccess(Sent sent) =>
        Unknown(sent.Elapsed, $"the vendor returned status {(int)sent.Status}", sent.Status switch
        {
            HttpStatusCode.TooManyRequests => ProviderFailureSignal.RateLimited,
            HttpStatusCode.PaymentRequired => ProviderFailureSignal.QuotaExhausted,
            HttpStatusCode.ServiceUnavailable => ProviderFailureSignal.Outage,
            HttpStatusCode.BadGateway => ProviderFailureSignal.Outage,
            HttpStatusCode.GatewayTimeout => ProviderFailureSignal.Outage,
            _ => ProviderFailureSignal.Transient,
        });

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
