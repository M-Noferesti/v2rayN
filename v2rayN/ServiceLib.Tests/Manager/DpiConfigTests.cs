namespace ServiceLib.Tests.Manager;

public class DpiConfigTests
{
    [Test]
    [Arguments("https://8.8.8.8/dns-query", "8.8.8.8")]
    [Arguments("ech.example+https://resolver.example/dns-query", "resolver.example")]
    [Arguments("https://resolver.example/dns-query?token=a+b", "resolver.example")]
    [Arguments("AEX+DQBBwwAgACB94MZ", null)]
    [Arguments("", null)]
    public async Task EchBootstrapProtectsResolverInsteadOfQueryIdentity(string value, string? expected)
    {
        await TunBootstrapService.EchResolverHost(value).Should().BeEqualTo(expected);
    }

    [Test]
    public async Task TunProcessNameFallbackDoesNotRequireExactPathMatch()
    {
        var source = """{"route":{"rules":[{"action":"sniff"},{"outbound":"direct","process_path":["C:/app/xray.exe"],"process_name":["xray.exe"]}]},"inbounds":[{"type":"tun"}]}""";
        var result = JsonNode.Parse(PsiphonConfigService.SimplifyTunFrontend(source, [IPAddress.Parse("8.8.8.8")], true))!;
        var rules = result["route"]!["rules"]!;
        await rules[0]!["process_name"]![0]!.GetValue<string>().Should().BeEqualTo("xray.exe");
        await (rules[0]!["process_path"] == null).Should().BeTrue();
        await (rules[1]!["process_name"] == null).Should().BeTrue();
        await result["inbounds"]![0]!["route_exclude_address"]![0]!.GetValue<string>().Should().BeEqualTo("8.8.8.8/32");
    }

    private static ProfileItem Node(EConfigType type = EConfigType.VLESS) => new()
    {
        ConfigType = type, StreamSecurity = "tls", Address = "real.example.com", Port = 443,
    };

    [Test]
    [Arguments("vless", "vnext", EConfigType.VLESS)]
    [Arguments("trojan", "servers", EConfigType.Trojan)]
    public async Task SpoofingChangesDialEndpointButPreservesTlsIdentityAndCredentials(string protocol, string servers, EConfigType type)
    {
        var node = Node(type);
        var source = """
            {"outbounds":[{"tag":"proxy","protocol":"__PROTOCOL__","settings":{"__SERVERS__":[{"address":"real.example.com","port":443,"password":"test-password","users":[{"id":"test-user"}]}]},"streamSettings":{"security":"tls","tlsSettings":{"serverName":"certificate.example.com","allowInsecure":false},"wsSettings":{"headers":{"Host":"websocket.example.com"}},"sockopt":{"dialerProxy":"fragment"}}},{"tag":"direct","protocol":"freedom"}]}
            """.Replace("__PROTOCOL__", protocol).Replace("__SERVERS__", servers);
        var settings = new SniSpoofingItem { ConnectAddress = "198.51.100.42:8443", DecoySni = "hcaptcha.com" };
        var plan = SniSpoofingService.CreatePlan(node, settings, 40443);
        var result = JsonNode.Parse(SniSpoofingService.RewriteOutbound(source, node, plan))!;
        var proxy = result["outbounds"]![0]!;
        await proxy["settings"]![servers]![0]!["address"]!.GetValue<string>().Should().BeEqualTo("127.0.0.1");
        await proxy["settings"]![servers]![0]!["port"]!.GetValue<int>().Should().BeEqualTo(40443);
        await proxy["settings"]![servers]![0]!["password"]!.GetValue<string>().Should().BeEqualTo("test-password");
        await proxy["streamSettings"]!["tlsSettings"]!["serverName"]!.GetValue<string>().Should().BeEqualTo("certificate.example.com");
        await proxy["streamSettings"]!["tlsSettings"]!["allowInsecure"]!.GetValue<bool>().Should().BeFalse();
        await proxy["streamSettings"]!["wsSettings"]!["headers"]!["Host"]!.GetValue<string>().Should().BeEqualTo("websocket.example.com");
        await result["outbounds"]![1]!["protocol"]!.GetValue<string>().Should().BeEqualTo("freedom");
        await node.Address.Should().BeEqualTo("real.example.com");
        await plan.Host.Should().BeEqualTo("198.51.100.42");
    }

    [Test]
    public async Task EmptyRealSniKeepsOriginalServerIdentityAfterLoopbackRewrite()
    {
        var node = Node();
        var plan = SniSpoofingService.CreatePlan(node, new(), 40443);
        var source = """{"outbounds":[{"tag":"proxy","protocol":"vless","settings":{"vnext":[{"address":"real.example.com","port":443}]},"streamSettings":{"tlsSettings":{}}}]}""";
        var result = JsonNode.Parse(SniSpoofingService.RewriteOutbound(source, node, plan))!;
        await result["outbounds"]![0]!["streamSettings"]!["tlsSettings"]!["serverName"]!.GetValue<string>().Should().BeEqualTo(node.Address);
    }

    [Test]
    [Arguments("127.0.0.1:443")]
    [Arguments("localhost:443")]
    [Arguments("[2001:db8::1]:443")]
    [Arguments("198.51.100.1:0")]
    [Arguments("example.com:443 -test")]
    [Arguments("user@example.com:443")]
    public async Task RejectUnsupportedOrUnsafeDestinations(string address)
    {
        var rejected = false;
        try { SniSpoofingService.CreatePlan(Node(), new() { ConnectAddress = address }, 40443); }
        catch (ArgumentException) { rejected = true; }
        await rejected.Should().BeTrue();
    }

    [Test]
    [Arguments(EConfigType.SOCKS, "tls")]
    [Arguments(EConfigType.VLESS, "")]
    public async Task RejectNonTlsOrUnsupportedConfig(EConfigType type, string security)
    {
        var node = Node(type);
        node.StreamSecurity = security;
        var rejected = false;
        try { SniSpoofingService.CreatePlan(node, new(), 40443); }
        catch (ArgumentException) { rejected = true; }
        await rejected.Should().BeTrue();
    }

    [Test]
    [Arguments("A")]
    [Arguments("B")]
    public async Task ServerlessKeepsFragmentAndDnsRulesAndUsesRequestedLoopbackPort(string variant)
    {
        var json = JsonNode.Parse(ServerlessConfigService.CreateConfig(variant, 14150))!;
        await json["inbounds"]![0]!["listen"]!.GetValue<string>().Should().BeEqualTo("127.0.0.1");
        await json["inbounds"]![0]!["port"]!.GetValue<int>().Should().BeEqualTo(14150);
        await json["dns"]!["servers"]!.AsArray().Count.Should().BeEqualTo(3);
        var fragment = json["outbounds"]!.AsArray().First(o => o!["tag"]!.GetValue<string>() == "tcp-fragment-tls")!;
        await fragment["streamSettings"]!["finalmask"]!["tcp"]![0]!["settings"]!["lengths"]![0]!.GetValue<string>()
            .Should().BeEqualTo(variant == "A" ? "6" : "0");
        await json["routing"]!["rules"]!.AsArray().Count.Should().BeEqualTo(17);
    }

    [Test]
    public async Task ServerlessNeverChangesSelectedProfileAndUsesPrivateTunPort()
    {
        var config = new Config { IndexId = "selected-profile", ServerlessMode = "B", TunModeItem = new() { EnableTun = true } };
        var node = ServerlessConfigService.CreateNode(config, 14150);
        await config.IndexId.Should().BeEqualTo("selected-profile");
        await node.PreSocksPort.Should().BeEqualTo(14150);
        await node.IndexId.Should().BeEqualTo(ServerlessConfigService.NodeId);
        config.TunModeItem.EnableTun = false;
        await (ServerlessConfigService.CreateNode(config, 10808).PreSocksPort == null).Should().BeTrue();
    }

    [Test]
    public async Task SniHelperBypassesTunToAvoidRoutingLoop()
    {
        var source = """{"route":{"rules":[{"action":"sniff"}]}}""";
        var result = JsonNode.Parse(SniSpoofingService.ProtectTun(source))!;
        await result["route"]!["rules"]![0]!["outbound"]!.GetValue<string>().Should().BeEqualTo("direct");
        await result["route"]!["rules"]![0]!["process_name"]![0]!.GetValue<string>().Should().BeEqualTo(SniSpoofingService.BinaryName);
    }

    [Test]
    public async Task CloudflarePresetMatchesSuppliedSettingsWithoutEditingProfileOrTlsVerification()
    {
        var config = CoreConfig.CoreConfigTestFactory.CreateConfig(ECoreType.Xray);
        CoreConfig.CoreConfigTestFactory.BindAppManagerConfig(config);
        var node = CoreConfig.CoreConfigTestFactory.CreateVmessNode(ECoreType.Xray);
        node.ConfigType = EConfigType.VLESS;
        node.SetProtocolExtra(node.GetProtocolExtra() with { VlessEncryption = "none" });
        node.StreamSecurity = "tls";
        node.Sni = "real.example.com";
        node.Network = "ws";
        node.Fingerprint = "chrome";
        node.AllowInsecure = "false";
        var context = CoreConfig.CoreConfigTestFactory.CreateContext(config, node, ECoreType.Xray);
        var generated = new CoreConfigV2rayService(context).GenerateClientConfigContent();
        await generated.Success.Should().BeTrue();
        var patched = JsonNode.Parse(CloudflareFragmentService.Apply(generated.Data!.ToString()!, node, "188.114.97.6"))!;
        var proxy = patched["outbounds"]!.AsArray().First(o => o!["tag"]!.GetValue<string>() == "proxy")!;
        var tls = proxy["streamSettings"]!["tlsSettings"]!;
        await tls["fingerprint"]!.GetValue<string>().Should().BeEqualTo("unsafe");
        await tls["alpn"]![0]!.GetValue<string>().Should().BeEqualTo("http/1.1");
        await tls["cipherSuites"]!.GetValue<string>().Should().BeEqualTo(CloudflareFragmentService.CipherSuites);
        await tls["serverName"]!.GetValue<string>().Should().BeEqualTo("real.example.com");
        await (tls["allowInsecure"]?.GetValue<bool>() == true).Should().BeFalse();
        await proxy["settings"]!["address"]!.GetValue<string>().Should().BeEqualTo("188.114.97.6");
        await JsonNode.DeepEquals(proxy["streamSettings"]!["finalmask"], JsonNode.Parse(CloudflareFragmentService.Finalmask)).Should().BeTrue();
        await node.Address.Should().BeEqualTo("example.com");
        await node.Fingerprint.Should().BeEqualTo("chrome");
        var plan = SniSpoofingService.CreatePlan(node, new(), 40443);
        var spoofed = JsonNode.Parse(SniSpoofingService.RewriteOutbound(generated.Data!.ToString()!, node, plan))!;
        var spoofProxy = spoofed["outbounds"]!.AsArray().First(o => o!["tag"]!.GetValue<string>() == "proxy")!;
        await spoofProxy["settings"]!["address"]!.GetValue<string>().Should().BeEqualTo("127.0.0.1");
        await spoofProxy["streamSettings"]!["tlsSettings"]!["serverName"]!.GetValue<string>().Should().BeEqualTo("real.example.com");
    }
}
