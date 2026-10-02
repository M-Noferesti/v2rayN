namespace ServiceLib.Services.CoreConfig;

public record SniSpoofingPlan(string Host, int Port, string Decoy, int LocalPort, string Arguments);

public static class SniSpoofingService
{
    public static readonly string[] Fingerprints = ["firefox", "chrome", "safari", "edge", "randomized", "none"];
    public static string BinaryName => RuntimeInformation.ProcessArchitecture == Architecture.Arm64
        ? "sni-spoofing-windows-arm64.exe" : "sni-spoofing-windows-amd64.exe";
    public static string BinaryPath => Utils.GetBinPath(BinaryName, "SniSpoofing");

    public static SniSpoofingPlan CreatePlan(ProfileItem node, SniSpoofingItem settings, int localPort)
    {
        if (node.ConfigType is not (EConfigType.VLESS or EConfigType.Trojan)
            || node.StreamSecurity is not ("tls" or "reality")
            || node.GetNetwork() is "kcp" or "quic")
            throw new ArgumentException("SNI spoofing requires an active VLESS or Trojan config with TLS/REALITY over TCP.");
        if (localPort is < 1 or > 65535) throw new ArgumentException("Invalid SNI local port.");
        var host = node.Address;
        var port = node.Port;
        if (!string.IsNullOrWhiteSpace(settings.ConnectAddress))
        {
            if (!Uri.TryCreate("tcp://" + settings.ConnectAddress.Trim(), UriKind.Absolute, out var uri)
                || uri.Port is < 1 or > 65535 || uri.UserInfo.Length > 0 || uri.AbsolutePath != "/"
                || uri.Query.Length > 0 || uri.Fragment.Length > 0)
                throw new ArgumentException("Spoof destination must be a host or IPv4 address followed by :port.");
            host = uri.Host;
            port = uri.Port;
        }
        if (port is < 1 or > 65535 || host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || Uri.CheckHostName(host) == UriHostNameType.Unknown || IPAddress.TryParse(host, out var ip)
            && (ip.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip)))
            throw new ArgumentException("The SNI helper supports a hostname or a non-loopback IPv4 destination.");
        var decoy = (settings.DecoySni ?? "").Trim();
        if (decoy.Length == 0 && !IPAddress.TryParse(host, out _)) decoy = host;
        if (Uri.CheckHostName(decoy) != UriHostNameType.Dns)
            throw new ArgumentException("Set a valid decoy SNI hostname, for example hcaptcha.com.");
        if (!Fingerprints.Contains(settings.Fingerprint) || settings.Injector is not ("active" or "passive"))
            throw new ArgumentException("Choose a supported fingerprint and injector mode.");
        // All values are validated hostnames, integers or fixed presets; no shell is used.
        var arguments = $"-listen 127.0.0.1:{localPort} -connect {host}:{port} -fake-sni {decoy} -utls {settings.Fingerprint} -injector {settings.Injector}"
            + (settings.Fragment ? " -enable-fragment" : "");
        return new(host, port, decoy, localPort, arguments);
    }

    public static string RewriteOutbound(string source, ProfileItem node, SniSpoofingPlan plan)
    {
        var json = JsonNode.Parse(source)!.AsObject();
        var outbound = (json["outbounds"] as JsonArray)?.FirstOrDefault(o => o?["tag"]?.GetValue<string>() == Global.ProxyTag);
        if (outbound?["protocol"]?.GetValue<string>() is not ("vless" or "trojan"))
            throw new ArgumentException("SNI spoofing does not support custom templates or proxy chains. Select a single VLESS/Trojan config.");
        var server = FindServer(outbound, node.ConfigType);
        var stream = outbound["streamSettings"];
        var tls = stream?[node.StreamSecurity == "reality" ? "realitySettings" : "tlsSettings"];
        if (server == null || tls == null || server["address"]?.GetValue<string>() != node.Address
            || server["port"]?.GetValue<int>() != node.Port)
            throw new ArgumentException("SNI spoofing could not identify the active TLS outbound.");
        // Preserve the real TLS identity and WebSocket/HTTP headers. Only the dial endpoint changes.
        if (string.IsNullOrEmpty(tls["serverName"]?.GetValue<string>())) tls["serverName"] = node.Address;
        server["address"] = "127.0.0.1";
        server["port"] = plan.LocalPort;
        if (stream?["sockopt"] is JsonObject sockopt)
        {
            sockopt.Remove("dialerProxy");
            sockopt.Remove("interface");
            sockopt.Remove("sendThrough");
        }
        return json.ToJsonString(new() { WriteIndented = true });
    }

    internal static JsonNode? FindServer(JsonNode? outbound, EConfigType type)
    {
        var settings = outbound?["settings"];
        // Current Xray uses flat outbound settings; older templates use vnext/servers.
        return settings?["address"] != null ? settings
            : (settings?[type == EConfigType.VLESS ? "vnext" : "servers"] as JsonArray)?.FirstOrDefault();
    }

    public static string ProtectTun(string source)
    {
        var json = JsonNode.Parse(source)!.AsObject();
        if (json["route"]?["rules"] is JsonArray rules)
            rules.Insert(0, new JsonObject { ["process_name"] = new JsonArray(BinaryName), ["outbound"] = "direct" });
        return json.ToJsonString(new() { WriteIndented = true });
    }
}
