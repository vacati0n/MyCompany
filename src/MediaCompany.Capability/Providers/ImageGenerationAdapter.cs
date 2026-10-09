using System.Net;
using System.Text.Json;
using MediaCompany.Credentials;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Capability.Providers;

/// <summary>
/// The image path of the speech-and-image vendor (the production change, decision D-002 of its design). Built and
/// proven against a recorded response and its fake; item 001's run calls it never.
///
/// VENDOR SHAPE, recorded by the implementing role from its own knowledge of the vendor's published reference on
/// 2026-10-09 and NOT verified first hand inside the phase:
///   * POST {endpoint}v1/images/generations, authorization by a bearer value;
///   * JSON body: model, prompt, size, n = 1;
///   * response body: data[0].b64_json holds the image, and usage holds input_tokens and output_tokens.
/// The content is the DECODED image, one buffer, hashed as received from the decode. A body without usage leaves
/// the charge unknown.
/// </summary>
internal sealed class ImageGenerationAdapter : IProviderAdapter
{
    internal const string Path = "v1/images/generations";

    private readonly HttpClient _http;
    private readonly ICredentialExchange _exchange;
    private readonly Uri _endpoint;

    internal ImageGenerationAdapter(ProviderAccountId account, Uri endpoint, HttpClient http, ICredentialExchange exchange)
    {
        ProviderAccount = account;
        _endpoint = new Uri(endpoint, Path);
        _http = http;
        _exchange = exchange;
    }

    public ProviderAccountId ProviderAccount { get; }

    public bool Serves(CapabilityClass capability) => capability == CapabilityClass.StillImages;

    public async Task<ProviderAttempt> InvokeAsync(
        RouteTarget.ProviderRoute route,
        CapabilityRequest request,
        ScopedHandle handle,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handle);

        if (request.Payload is not CapabilityPayload.StillImage image)
        {
            return VendorCalls.WrongPayload("image");
        }

        using var message = VendorCalls.Json(_endpoint, new Dictionary<string, object>
        {
            ["model"] = route.Model.Value,
            ["prompt"] = image.Prompt,
            ["size"] = image.Size,
            ["n"] = 1,
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

        try
        {
            using var document = JsonDocument.Parse(sent.Body);
            var root = document.RootElement;
            var data = root.GetProperty("data");
            var encoded = data[0].GetProperty("b64_json").GetString()
                ?? throw new FormatException("the image response carries no image");
            var bytes = Convert.FromBase64String(encoded);
            var content = new ProducedContent(ContentKind.Image, "image/png", bytes);

            var usage = root.TryGetProperty("usage", out var u) ? u : default;
            var input = VendorCalls.Usage(usage, "input_tokens");
            var output = VendorCalls.Usage(usage, "output_tokens");
            if (input is null || output is null)
            {
                return new ProviderAttempt(
                    true, UnitCounts.None, CostBasis.Estimate, sent.Elapsed,
                    "the image response states no usage; booked at the admitted worst case", null)
                {
                    Content = content,
                    ChargeKnown = false,
                };
            }

            return new ProviderAttempt(
                true,
                new UnitCounts(input.Value, output.Value, 0, 0) { ImageUnits = data.GetArrayLength() },
                CostBasis.Measurement,
                sent.Elapsed,
                null,
                null)
            {
                Content = content,
            };
        }
        catch (Exception malformed) when (malformed is JsonException or FormatException or KeyNotFoundException or InvalidOperationException or IndexOutOfRangeException)
        {
            return VendorCalls.Unknown(sent.Elapsed, $"the image response could not be read ({malformed.GetType().Name})", ProviderFailureSignal.Transient);
        }
    }
}
