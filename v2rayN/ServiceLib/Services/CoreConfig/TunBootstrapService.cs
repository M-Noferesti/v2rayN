namespace ServiceLib.Services.CoreConfig;

public static class TunBootstrapService
{
    public static string? EchResolverHost(string? ech)
    {
        if (string.IsNullOrWhiteSpace(ech)) return null;
        var url = ech.Trim();
        var prefix = url.IndexOf('+');
        var scheme = url.IndexOf("://", StringComparison.Ordinal);
        if (prefix >= 0 && prefix < scheme) url = url[(prefix + 1)..];
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && uri.Scheme is "https" or "http" && !string.IsNullOrEmpty(uri.Host)
            ? uri.Host : null;
    }
}
