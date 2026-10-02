namespace ServiceLib.Services.CoreConfig;

public static class ServerlessConfigService
{
    public const string NodeId = "internal-serverless";
    public static bool IsEnabled(Config config) => config.ServerlessMode is "A" or "B";
    public static string BinaryPath => Utils.GetBinPath(Utils.GetExeName("xray"), "Serverless");

    public static string CreateConfig(string variant, int port, bool allowLan = false)
    {
        if (variant is not ("A" or "B") || port is < 1 or > 65535) throw new ArgumentException("Invalid Serverless variant or port.");
        var source = EmbedUtils.GetEmbedText($"ServiceLib.Sample.serverless_frag{variant}");
        var json = JsonNode.Parse(source)!.AsObject();
        var inbound = json["inbounds"]![0]!;
        inbound["port"] = port;
        inbound["listen"] = allowLan ? "0.0.0.0" : "127.0.0.1";
        return json.ToJsonString(new() { WriteIndented = true });
    }

    public static ProfileItem CreateNode(Config config, int port) => new()
    {
        IndexId = NodeId,
        ConfigType = EConfigType.Custom,
        CoreType = ECoreType.Xray,
        Remarks = $"Serverless · Fragment {config.ServerlessMode}",
        Address = "internal-serverless",
        PreSocksPort = config.TunModeItem.EnableTun ? port : null,
        DisplayLog = true,
    };
}
