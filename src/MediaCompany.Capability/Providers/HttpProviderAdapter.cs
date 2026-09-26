using System.Diagnostics;
using System.Net;
using MediaCompany.Credentials;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Capability.Providers;

/// <summary>
/// The one adapter shape this wave delivers: a metered HTTP provider reached over a transport the
/// credential is attached to at send time.
///
/// It is internal and sealed. Nothing outside this assembly can name it, construct it, or reach a
/// provider through it, which is the property decision D-001 requires and the architecture
/// boundary test asserts.
///
/// No account is created and no spend is committed by this wave (constraint C-013). The adapter
/// is exercised in tests against a stub transport; its live use waits on the account provisioning
/// the CEO holds.
/// </summary>
internal sealed class HttpProviderAdapter : IProviderAdapter
{
    private readonly HttpClient _http;
    private readonly ICredentialExchange _exchange;
    private readonly Uri _endpoint;

    internal HttpProviderAdapter(
        ProviderAccountId providerAccount,
        Uri endpoint,
        HttpClient http,
        ICredentialExchange exchange)
    {
        ProviderAccount = providerAccount;
        _endpoint = endpoint;
        _http = http;
        _exchange = exchange;
    }

    public ProviderAccountId ProviderAccount { get; }

    public async Task<ProviderAttempt> InvokeAsync(
        RouteTarget.ProviderRoute route,
        CapabilityRequest request,
        ScopedHandle handle,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handle);

        var stopwatch = Stopwatch.StartNew();
        using var message = new HttpRequestMessage(HttpMethod.Post, _endpoint);

        // The request object is fully assembled before the credential is attached, so no code path
        // exists in which the credential is in scope while a prompt or an artifact is being built.
        message.Content = new StringContent(
            $"{{\"capability\":\"{route.Model}\",\"tier\":\"{request.ReasoningTier}\"}}",
            System.Text.Encoding.UTF8,
            "application/json");

        var attached = await _exchange.AttachAsync(handle, message, cancellationToken).ConfigureAwait(false);
        if (attached is CredentialOutcome.Refused refused)
        {
            stopwatch.Stop();
            return new ProviderAttempt(
                false,
                UnitCounts.None,
                CostBasis.Measurement,
                stopwatch.Elapsed,
                $"credential refused: {refused.Reason} — {refused.Detail}",
                ProviderFailureSignal.Transient);
        }

        try
        {
            using var response = await _http.SendAsync(message, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            if (response.IsSuccessStatusCode)
            {
                var (units, basis) = ReadUsage(response);
                return new ProviderAttempt(true, units, basis, stopwatch.Elapsed, null, null);
            }

            var signal = response.StatusCode switch
            {
                HttpStatusCode.TooManyRequests => ProviderFailureSignal.RateLimited,
                HttpStatusCode.PaymentRequired => ProviderFailureSignal.QuotaExhausted,
                HttpStatusCode.ServiceUnavailable => ProviderFailureSignal.Outage,
                HttpStatusCode.BadGateway => ProviderFailureSignal.Outage,
                HttpStatusCode.GatewayTimeout => ProviderFailureSignal.Outage,
                _ => ProviderFailureSignal.Transient,
            };

            return new ProviderAttempt(
                false,
                UnitCounts.None,
                CostBasis.Measurement,
                stopwatch.Elapsed,
                $"provider returned {(int)response.StatusCode}",
                signal);
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            return new ProviderAttempt(
                false,
                UnitCounts.None,
                CostBasis.Measurement,
                stopwatch.Elapsed,
                ex.Message,
                ProviderFailureSignal.Outage);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new ProviderAttempt(
                false,
                UnitCounts.None,
                CostBasis.Measurement,
                stopwatch.Elapsed,
                ex.Message,
                ProviderFailureSignal.Outage);
        }
    }

    /// <summary>
    /// Reads the per-operation usage counts assumption A-002 expects. When a provider returns
    /// none, the counts fall back to the request's own estimate and the basis is recorded as
    /// <see cref="CostBasis.Estimate"/>, so the report states which it was (risk R-009).
    /// </summary>
    private static (UnitCounts Units, CostBasis Basis) ReadUsage(HttpResponseMessage response)
    {
        var input = ReadHeader(response, "x-usage-input-units");
        var output = ReadHeader(response, "x-usage-output-units");
        var cached = ReadHeader(response, "x-usage-cached-units");

        if (input is null && output is null && cached is null)
        {
            return (UnitCounts.None, CostBasis.Estimate);
        }

        return (new UnitCounts(input ?? 0, output ?? 0, cached ?? 0, 0), CostBasis.Measurement);
    }

    private static long? ReadHeader(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues(name, out var values))
        {
            return null;
        }

        var first = values.FirstOrDefault();
        return long.TryParse(first, out var parsed) ? parsed : null;
    }
}
