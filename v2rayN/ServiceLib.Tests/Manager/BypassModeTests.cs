namespace ServiceLib.Tests.Manager;

public class BypassModeTests
{
    [Test]
    [Arguments("only", "A")]
    [Arguments("after", "B")]
    public async Task DisablingAllModesPersistsNormalSelectionAndRetainsSavedSettings(string psiphon, string serverless)
    {
        var config = new Config
        {
            IndexId = "selected-server",
            PsiphonMode = psiphon,
            PsiphonProfileId = "saved-psiphon",
            ServerlessMode = serverless,
            CloudflareFragment = true,
            SniSpoofing = new() { Enabled = true, DecoySni = "saved.example", ConnectAddress = "192.0.2.1:443" },
            TunModeItem = new() { EnableTun = false },
        };
        BypassModeService.DisableAll(config);
        var restored = JsonUtils.Deserialize<Config>(JsonUtils.Serialize(config))!;
        await restored.PsiphonMode.Should().BeEqualTo("off");
        await restored.SniSpoofing.Enabled.Should().BeFalse();
        await (restored.ServerlessMode == null).Should().BeTrue();
        await restored.CloudflareFragment.Should().BeFalse();
        await restored.TunModeItem.EnableTun.Should().BeFalse();
        await restored.IndexId.Should().BeEqualTo("selected-server");
        await restored.PsiphonProfileId.Should().BeEqualTo("saved-psiphon");
        await restored.SniSpoofing.DecoySni.Should().BeEqualTo("saved.example");
        await restored.SniSpoofing.ConnectAddress.Should().BeEqualTo("192.0.2.1:443");
    }
}
