using System.Net;
using MediaCompany.Credentials;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Capability.Providers;

/// <summary>
/// The speech path of the speech-and-image vendor (the production change, decision D-002 of its design).
///
/// VENDOR SHAPE, recorded by the implementing role from its own knowledge of the vendor's published reference on
/// 2026-10-09 and NOT verified first hand inside the phase; first-hand verification of the path, the body, the
/// authentication, the response and the price is the orchestrator's precondition of the go:
///   * POST {endpoint}v1/audio/speech, authorization by a bearer value;
///   * JSON body: model, input (the exact text), voice, response_format;
///   * the response body IS the audio, in the requested format, and states NO usage;
///   * the vendor bills the request by the characters of its input.
/// Because the body states no usage, every completed speech attempt has an UNKNOWN charge and is booked at the
/// admitted worst case, the request's characters, labelled estimate: exact for a character-billed request, and
/// never a zero measurement.
/// </summary>
internal sealed class SpeechAudioAdapter : IProviderAdapter
{
    /// <summary>The endpoint path, relative to the configured endpoint.</summary>
    internal const string Path = "v1/audio/speech";

    private readonly HttpClient _http;
    private readonly ICredentialExchange _exchange;
    private readonly Uri _endpoint;

    internal SpeechAudioAdapter(ProviderAccountId account, Uri endpoint, HttpClient http, ICredentialExchange exchange)
    {
        ProviderAccount = account;
        _endpoint = new Uri(endpoint, Path);
        _http = http;
        _exchange = exchange;
    }

    public ProviderAccountId ProviderAccount { get; }

    public bool Serves(CapabilityClass capability) => capability == CapabilityClass.Narration;

    public async Task<ProviderAttempt> InvokeAsync(
        RouteTarget.ProviderRoute route,
        CapabilityRequest request,
        ScopedHandle handle,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handle);

        if (request.Payload is not CapabilityPayload.Narration narration)
        {
            return VendorCalls.WrongPayload("speech");
        }

        using var message = VendorCalls.Json(_endpoint, new Dictionary<string, object>
        {
            ["model"] = route.Model.Value,
            ["input"] = narration.Text,
            ["voice"] = narration.Voice,
            ["response_format"] = narration.Format,
        });

        var (sent, ended) = await VendorCalls.SendAsync(_http, _exchange, handle, message, cancellationToken).ConfigureAwait(false);
        if (ended is not null)
        {
            return ended;
        }

        if (sent!.Status != HttpStatusCode.OK)
        {
            return VendorCalls.NonSuccess(sent);
        }

        // The body is the audio. It states no usage, so the charge is not known from the response: the boundary
        // books the admitted worst case, which for a character-billed request is its exact characters.
        return new ProviderAttempt(
            true, UnitCounts.None, CostBasis.Estimate, sent.Elapsed,
            "the speech response states no usage; booked at the admitted worst case, the request's characters", null)
        {
            Content = new ProducedContent(ContentKind.Audio, sent.MediaType ?? "audio/" + narration.Format, sent.Body),
            ChargeKnown = false,
        };
    }
}
