namespace ServiceLib.Models.Configs;

public class SniSpoofingItem
{
    public bool Enabled { get; set; }
    public string ConnectAddress { get; set; } = "";
    public string DecoySni { get; set; } = "hcaptcha.com";
    public string Fingerprint { get; set; } = "firefox";
    public string Injector { get; set; } = "active";
    public bool Fragment { get; set; }
    public string CloudflareAddress { get; set; } = "";
}
