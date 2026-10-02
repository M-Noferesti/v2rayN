namespace ServiceLib.Services.CoreConfig;

public static class BypassModeService
{
    public static void DisableAll(Config config)
    {
        config.PsiphonMode = "off";
        config.SniSpoofing.Enabled = false;
        config.ServerlessMode = null;
        config.CloudflareFragment = false;
    }
}
