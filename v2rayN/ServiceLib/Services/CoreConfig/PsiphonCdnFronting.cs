namespace ServiceLib.Services.CoreConfig;

/// <summary>Chosen CDN-fronting behaviour for a Psiphon profile ("" / "normal", "auto", "cdn").</summary>
public record PsiphonCdnOptions(string? Mode, string? CustomIps, string? CustomSni)
{
    public bool Enabled => Mode is "auto" or "cdn";
}

/// <summary>
/// CDN fronting for Psiphon, following the Shir o Khorshid design: FrontedMeekDialOverrides pointing
/// at Akamai/Fastly edges, an optional scan spec, and (in "cdn" mode) only the CDN tunnel protocols.
/// These settings and protocol names exist only in the modified engine (psiphon-tunnel-core-cdn.exe).
/// </summary>
public static class PsiphonCdnFronting
{
    public const string EngineName = "psiphon-tunnel-core-cdn";

    public static readonly string[] CdnTunnelProtocols =
        ["FRONTED-MEEK-CDN-OSSH", "FRONTED-MEEK-CDN-HTTP-OSSH", "FRONTED-MEEK-CDN-QUIC-OSSH"];

    private static readonly (string Id, string Ip)[] EdgeAddresses =
    [
        ("edge-a-1", "23.215.0.206"), ("edge-a-2", "23.215.0.203"),
        ("edge-b-1", "23.212.250.91"), ("edge-b-2", "23.212.250.78"),
        ("edge-c-1", "23.12.147.13"), ("edge-c-2", "23.12.147.29"),
        ("edge-d-1", "23.73.207.8"), ("edge-d-2", "23.73.207.15"),
        ("edge-original", "92.123.102.43"),
    ];

    private static readonly string[] FastlyVerifyNames =
    [
        "www.python.org", "pypi.org", "fastly.com", "www.fastly.com", "developer.fastly.com",
        "githubassets.com", "github.com", "github.io", "githubusercontent.com",
    ];

    public static bool EngineAvailable() =>
        File.Exists(Utils.GetBinPath(Utils.GetExeName(EngineName), nameof(ECoreType.Psiphon)));

    public static List<string> ParseIps(string? text) => Split(text)
        .Where(item => IsIpv4(item) || IsIpv4Cidr(item)).Distinct().ToList();

    public static List<string> ParseSni(string? text) => Split(text)
        .Select(NormalizeHost).Where(item => item.Length > 0).Distinct().ToList();

    private static IEnumerable<string> Split(string? text) =>
        (text ?? "").Split([' ', '\t', '\r', '\n', ',', ';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(item => item.Trim());

    private static bool IsIpv4(string value) =>
        value.Count(c => c == '.') == 3 && IPAddress.TryParse(value, out var ip)
        && ip.AddressFamily == AddressFamily.InterNetwork;

    private static bool IsIpv4Cidr(string value)
    {
        var parts = value.Split('/');
        return parts.Length == 2 && IsIpv4(parts[0]) && int.TryParse(parts[1], out var prefix) && prefix is >= 0 and <= 32;
    }

    private static string NormalizeHost(string host)
    {
        var name = host.Trim().ToLowerInvariant().TrimEnd('.');
        if (name.Length is 0 or > 253 || IsIpv4(name)) return "";
        foreach (var label in name.Split('.'))
        {
            if (label.Length is 0 or > 63 || label.StartsWith('-') || label.EndsWith('-')
                || !label.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')) return "";
        }
        return name;
    }

    private static JsonObject Override(string id, string[]? providerRegexes, string[]? addressRegexes,
        string dialAddress, string sni, IEnumerable<string> verify, string[] alpn) => new()
    {
        ["OverrideID"] = id,
        ["MatchFrontingProviderIDRegexes"] = providerRegexes == null ? null : ToArray(providerRegexes),
        ["MatchDialAddressRegexes"] = addressRegexes == null ? null : ToArray(addressRegexes),
        ["DialAddresses"] = ToArray([dialAddress]),
        ["SNIServerName"] = sni,
        ["VerifyServerNames"] = ToArray(verify),
        ["ALPNProtocols"] = ToArray(alpn),
        ["TLSProfile"] = "Chrome-83",
    };

    private static JsonArray ToArray(IEnumerable<string> values) =>
        new(values.Select(value => (JsonNode?)JsonValue.Create(value)).ToArray());

    public static JsonArray BuildDialOverrides(string? customSni)
    {
        var edgeSni = ParseSni(customSni).FirstOrDefault() ?? "";
        var overrides = new JsonArray
        {
            Override("fastly-provider", ["(?i)fastly"], null, "pypi.org", "pypi.org", FastlyVerifyNames, ["h2", "http/1.1"]),
            Override("fastly-address", null, ["(?i)(fastly|pypi|python|github)"], "pypi.org", "pypi.org", FastlyVerifyNames, ["h2", "http/1.1"]),
        };
        foreach (var (id, ip) in EdgeAddresses)
        {
            var sni = edgeSni.Length > 0 ? edgeSni : ip;
            var verify = new[] { sni, ip, "a248.e.akamai.net", "a.akamaized.net", "a.akamaized-staging.net",
                "a.akamaihd.net", "a.akamaihd-staging.net", "www.akamai.com" }.Distinct();
            overrides.Add(Override(id, null, [".*"], ip, sni, verify, ["http/1.1"]));
        }
        return overrides;
    }

    public static void Apply(JsonObject config, PsiphonCdnOptions? options)
    {
        if (options is not { Enabled: true }) return;
        config["FrontedMeekDialOverrides"] = BuildDialOverrides(options.CustomSni);
        config["FrontedMeekDialOverridesProbability"] = 1.0;
        config["FrontedMeekCDNScanUseBuiltInSpec"] = true;
        var ips = ParseIps(options.CustomIps);
        if (ips.Count > 0)
        {
            var spec = new JsonObject { ["IPCandidates"] = ToArray(ips) };
            var snis = ParseSni(options.CustomSni);
            if (snis.Count > 0) spec["SNIServerNames"] = ToArray(snis);
            config["FrontedMeekCDNScanSpec"] = spec;
        }
        if (options.Mode == "cdn")
        {
            config["LimitTunnelProtocols"] = ToArray(CdnTunnelProtocols);
            config["DisableTactics"] = true;
        }
    }
}
