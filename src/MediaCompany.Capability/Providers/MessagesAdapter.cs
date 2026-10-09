using System.Net;
using System.Text;
using System.Text.Json;
using MediaCompany.Credentials;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Capability.Providers;

/// <summary>
/// The messages path of the reasoning vendor (the production change, decision D-002 of its design). Built and
/// proven against a recorded response and its fake; item 001's run calls it never, and no report calls it (the
/// owner's decision of 2026-10-09 keeps the executive-report rule in force).
///
/// VENDOR SHAPE, recorded by the implementing role from its own knowledge of the vendor's published reference on
/// 2026-10-09 and NOT verified first hand inside the phase:
///   * POST {endpoint}v1/messages, the secret in the vendor's key header, and the vendor's non-secret version
///     header;
///   * JSON body: model, max_tokens, messages = [{ role: user, content: the prompt }];
///   * response body: content[] of text blocks, and usage holding input_tokens, output_tokens and
///     cache_read_input_tokens.
/// </summary>
internal sealed class MessagesAdapter : IProviderAdapter
{
    internal const string Path = "v1/messages";

    /// <summary>The vendor's key header. Not a secret: it names where the secret goes.</summary>
    internal const string KeyHeader = "x-api-key";

    /// <summary>The vendor's version header and the version the recorded shape is of. Not secrets.</summary>
    internal const string VersionHeader = "anthropic-version";

    internal const string Version = "2023-06-01";

    private readonly HttpClient _http;
    private readonly ICredentialExchange _exchange;
    private readonly Uri _endpoint;

    internal MessagesAdapter(ProviderAccountId account, Uri endpoint, HttpClient http, ICredentialExchange exchange)
    {
        ProviderAccount = account;
        _endpoint = new Uri(endpoint, Path);
        _http = http;
        _exchange = exchange;
    }

    public ProviderAccountId ProviderAccount { get; }

    public bool Serves(CapabilityClass capability) =>
        capability is CapabilityClass.EditorialReasoning or CapabilityClass.HighStakesReview or CapabilityClass.BulkClassification;

    public string? KeyHeaderName => KeyHeader;

    public async Task<ProviderAttempt> InvokeAsync(
        RouteTarget.ProviderRoute route,
        CapabilityRequest request,
        ScopedHandle handle,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(handle);

        if (request.Payload is not CapabilityPayload.Messages messages)
        {
            return VendorCalls.WrongPayload("messages");
        }

        using var message = VendorCalls.Json(_endpoint, new Dictionary<string, object>
        {
            ["model"] = route.Model.Value,
            ["max_tokens"] = messages.MaxOutputUnits,
            ["messages"] = new[] { new Dictionary<string, string> { ["role"] = "user", ["content"] = messages.Prompt } },
        });
        message.Headers.TryAddWithoutValidation(VersionHeader, Version);

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
            var text = new StringBuilder();
            foreach (var block in root.GetProperty("content").EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) && type.GetString() == "text")
                {
                    text.Append(block.GetProperty("text").GetString());
                }
            }

            var content = new ProducedContent(ContentKind.Text, "text/plain; charset=utf-8", VendorCalls.Utf8(text.ToString()));
            var usage = root.TryGetProperty("usage", out var u) ? u : default;
            var input = VendorCalls.Usage(usage, "input_tokens");
            var output = VendorCalls.Usage(usage, "output_tokens");
            if (input is null || output is null)
            {
                return new ProviderAttempt(
                    true, UnitCounts.None, CostBasis.Estimate, sent.Elapsed,
                    "the messages response states no usage; booked at the admitted worst case", null)
                {
                    Content = content,
                    ChargeKnown = false,
                };
            }

            var cached = VendorCalls.Usage(usage, "cache_read_input_tokens") ?? 0;
            return new ProviderAttempt(true, new UnitCounts(input.Value, output.Value, cached, 0), CostBasis.Measurement, sent.Elapsed, null, null)
            {
                Content = content,
            };
        }
        catch (Exception malformed) when (malformed is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return VendorCalls.Unknown(sent.Elapsed, $"the messages response could not be read ({malformed.GetType().Name})", ProviderFailureSignal.Transient);
        }
    }
}
