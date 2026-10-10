using System.Text.Json.Nodes;
using MediaCompany.Host;
using MediaCompany.Production;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// The produce command's settings file (correction CR-006): a vendor endpoint is reached over https only. An endpoint
/// that is not an absolute https address is refused by name; an unset placeholder is no endpoint; the demonstration
/// providers name fakes and are not endpoints. No datastore and no network is reached.
/// </summary>
public sealed class HostSettingsTests
{
    [Theory]
    [InlineData("http://speech.example.invalid/")]
    [InlineData("ftp://speech.example.invalid/")]
    [InlineData("speech.example.invalid/v1")]
    public void AnEndpointThatIsNotAnAbsoluteHttpsAddressIsRefusedByName(string endpoint)
    {
        var path = Write(endpoint);
        try
        {
            var refused = Assert.Throws<ProductionSettingRefusedException>(() => ProductionCommands.ReadSettings(path));
            Assert.Equal("endpoints", refused.Setting);
            Assert.Contains("https only", refused.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void AnHttpsEndpointIsReadAndAPlaceholderIsNoEndpoint()
    {
        var https = Write("https://speech.example.invalid/");
        var placeholder = Write("<the speech vendor's API base address, verified first hand>");
        try
        {
            var read = ProductionCommands.ReadSettings(https);
            Assert.Equal("https", Assert.Single(read.Endpoints).Endpoint.Scheme);
            Assert.Single(read.Fakes);

            Assert.Empty(ProductionCommands.ReadSettings(placeholder).Endpoints);
        }
        finally
        {
            File.Delete(https);
            File.Delete(placeholder);
        }
    }

    /// <summary>
    /// THE SAMPLE'S MODEL SECTION reads in full (the own-voice change, decision D-011 of its design): the files of the
    /// installation of 2026-10-10 with their expected hashes, the six generation settings, the part bound of 350 s and the two
    /// configured licences with their source; and NO VENDOR MEMBER IS REQUIRED: without the provider-call bound, the character
    /// maximum and the voice, the settings still read, each member reading not configured.
    /// </summary>
    [Fact]
    public void TheSamplesModelSectionReadsAndNoVendorMemberIsRequired()
    {
        var path = Write("https://speech.example.invalid/", sample =>
        {
            sample.Remove("providerCallBoundSeconds");
            sample.Remove("narrationCharacterMaximum");
            sample.Remove("narrationVoice");
        });
        try
        {
            var production = ProductionCommands.ReadSettings(path).Production;
            Assert.Null(production.ProviderCallBound);
            Assert.Null(production.NarrationCharacterMaximum);
            Assert.Null(production.NarrationVoice);
            Assert.Contains("provider call not configured", production.DescribeBounds(), StringComparison.Ordinal);
            Assert.Contains("narration character maximum not configured", production.DescribeBounds(), StringComparison.Ordinal);

            var model = production.InHouseModel!;
            Assert.Equal(TimeSpan.FromSeconds(350), model.PartBound);
            Assert.Equal(new MediaCompany.Application.Ports.VoiceGenerationSettings(1m, 0.667m, 0.8m, 0m, 1m, true), model.Generation);
            Assert.Equal("piper", model.RuntimeModule);
            Assert.EndsWith("en_GB-cori-high.onnx.json", model.Configuration.Path, StringComparison.Ordinal);
            Assert.Equal(model.Model.Path + ".json", model.Configuration.Path);
            Assert.Equal("470b4dd634c98f8a4850d7626ffc3dfc90774628eeef6605a6dd8f88f30a5903", model.Model.Sha256);
            Assert.Equal(6, model.DependencyRecords.Count);
            Assert.Equal("MIT", model.WeightsLicence!.Value);
            Assert.Contains("c10ece1aade47bb51c153c893d14e5bf8e5b7117", model.VoiceLicence!.Source, StringComparison.Ordinal);
            Assert.All(new[] { model.Interpreter, model.EnvironmentConfiguration, model.Model, model.Configuration, model.ModelCard, model.RuntimeRecord }.Concat(model.DependencyRecords),
                f => Assert.DoesNotContain(Repository(), f.Path, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>EVERY MEMBER OF THE MODEL SECTION IS REQUIRED, refused by its name where absent; the part bound has NO DEFAULT.</summary>
    [Theory]
    [InlineData("interpreter")]
    [InlineData("environmentConfiguration")]
    [InlineData("baseInterpreterSha256")]
    [InlineData("runtimeModule")]
    [InlineData("model")]
    [InlineData("configuration")]
    [InlineData("modelCard")]
    [InlineData("runtimeRecord")]
    [InlineData("dependencyRecords")]
    [InlineData("generation")]
    [InlineData("partBoundSeconds")]
    [InlineData("generation.noiseScale")]
    [InlineData("generation.normalize")]
    public void EveryModelMemberIsRequiredByNameAndThePartBoundHasNoDefault(string member)
    {
        var path = Write("https://speech.example.invalid/", sample =>
        {
            var model = sample["inHouseModel"]!.AsObject();
            if (member.StartsWith("generation.", StringComparison.Ordinal))
            {
                model["generation"]!.AsObject().Remove(member["generation.".Length..]);
            }
            else
            {
                model.Remove(member);
            }
        });
        try
        {
            var refused = Assert.Throws<ProductionSettingRefusedException>(() => ProductionCommands.ReadSettings(path));
            Assert.Equal($"inHouseModel.{member}", refused.Setting);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>The sample settings with every placeholder set to a value of this test, and the endpoint given.</summary>
    private static string Write(string endpoint, Action<JsonObject>? change = null)
    {
        var sample = JsonNode.Parse(File.ReadAllText(Path.Combine(Repository(), "config", "production-settings.sample.json")))!.AsObject();
        change?.Invoke(sample);
        sample["outputRoot"] = Path.Combine(Path.GetTempPath(), "mediacompany-settings-test-output");
        sample["repositoryRoot"] = Repository();
        sample["rendererPath"] = "renderer-not-started-by-this-test";
        sample["probePath"] = "probe-not-started-by-this-test";
        sample["endpoints"]![0]!["endpoint"] = endpoint;
        var path = Path.Combine(Path.GetTempPath(), "mediacompany-settings-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, sample.ToJsonString());
        return path;
    }

    private static string Repository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MediaCompany.slnx")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }
}
