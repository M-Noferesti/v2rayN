namespace ServiceLib.Services.CoreConfig;

public static class CloudflareFragmentService
{
    public const string CipherSuites = "TLS_AES_256_GCM_SHA384:TLS_CHACHA20_POLY1305_SHA256:TLS_AES_128_GCM_SHA256:TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384:TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384:TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256:TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256:TLS_ECDHE_ECDSA_WITH_CHACHA20_POLY1305_SHA256:TLS_ECDHE_RSA_WITH_CHACHA20_POLY1305_SHA256:TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA:TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA:TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256:TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256";
    public const string Finalmask = """
        {"tcp":[{"type":"fragment","settings":{"packets":"tlshello","lengths":["0","104","1"],"delays":["0"],"maxSplit":"0"}},{"type":"fragment","settings":{"packets":"1-1","lengths":["114","1"],"delays":["1"],"maxSplit":"11"}}]}
        """;

    public static void Validate(ProfileItem node, string? cleanIp)
    {
        if (node.ConfigType is not (EConfigType.VLESS or EConfigType.Trojan) || node.StreamSecurity != "tls"
            || node.GetNetwork() is not ("raw" or "tcp" or "ws" or "httpupgrade" or "xhttp"))
            throw new ArgumentException("Cloudflare Fragment requires a VLESS/Trojan TLS config using WebSocket, HTTPUpgrade, XHTTP or TCP.");
        if (!string.IsNullOrWhiteSpace(cleanIp)
            && (!IPAddress.TryParse(cleanIp, out var ip) || ip.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip)))
            throw new ArgumentException("Cloudflare clean IP must be an IPv4 address, or leave it blank to keep the server address.");
    }

    public static string Apply(string source, ProfileItem node, string? cleanIp)
    {
        Validate(node, cleanIp);
        var json = JsonNode.Parse(source)!.AsObject();
        var outbound = (json["outbounds"] as JsonArray)?.FirstOrDefault(o => o?["tag"]?.GetValue<string>() == Global.ProxyTag);
        if (!ApplyToOutbound(outbound, node, cleanIp))
            throw new ArgumentException("Cloudflare Fragment supports a single active config, without custom templates or proxy chains.");
        return json.ToJsonString(new() { WriteIndented = true });
    }

    public static bool IsEligible(ProfileItem node, string? cleanIp)
    {
        try
        {
            Validate(node, cleanIp);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    // The preset needs Xray's finalmask support, which only the Serverless runtime bundle has.
    public static bool CanApplyToSpeedtest(Config config) =>
        config.CloudflareFragment && File.Exists(ServerlessConfigService.BinaryPath);

    // Delay tests put every profile in one config, each behind an outbound tagged proxy{port}.
    // Apply the same preset as the live connection to each eligible profile and leave the rest
    // unchanged, so the test measures what the running connection would actually do.
    public static string ApplyToSpeedtest(string source, IEnumerable<ServerTestItem> items, string? cleanIp)
    {
        var json = JsonNode.Parse(source)!.AsObject();
        if (json["outbounds"] is not JsonArray outbounds) return source;
        foreach (var item in items)
        {
            var node = item.Profile;
            if (node == null || item.Port <= 0 || !IsEligible(node, cleanIp)) continue;
            var tag = Global.ProxyTag + item.Port;
            var matches = outbounds.Where(o => o?["tag"]?.GetValue<string>() == tag).ToList();
            if (matches.Count == 1) ApplyToOutbound(matches[0], node, cleanIp);
        }
        return json.ToJsonString(new() { WriteIndented = true });
    }

    private static bool ApplyToOutbound(JsonNode? outbound, ProfileItem node, string? cleanIp)
    {
        var stream = outbound?["streamSettings"];
        var tls = stream?["tlsSettings"];
        var server = SniSpoofingService.FindServer(outbound, node.ConfigType);
        if (outbound?["protocol"]?.GetValue<string>() is not ("vless" or "trojan") || tls == null
            || server?["address"]?.GetValue<string>() != node.Address)
            return false;
        if (string.IsNullOrEmpty(tls["serverName"]?.GetValue<string>())) tls["serverName"] = node.Address;
        tls["fingerprint"] = "unsafe";
        tls["alpn"] = new JsonArray("http/1.1");
        tls["cipherSuites"] = CipherSuites;
        stream!["finalmask"] = JsonNode.Parse(Finalmask);
        if (stream["sockopt"] is JsonObject sockopt) sockopt.Remove("dialerProxy");
        if (!string.IsNullOrWhiteSpace(cleanIp)) server!["address"] = cleanIp;
        return true;
    }
}
