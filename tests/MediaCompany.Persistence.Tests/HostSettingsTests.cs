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

    /// <summary>The sample settings with every placeholder set to a value of this test, and the endpoint given.</summary>
    private static string Write(string endpoint)
    {
        var sample = JsonNode.Parse(File.ReadAllText(Path.Combine(Repository(), "config", "production-settings.sample.json")))!.AsObject();
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
