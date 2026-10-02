using ServiceLib.Tests.CoreConfig;

namespace ServiceLib.Tests.Services;

[NotInParallel]
public class ConnectionHandlerCancellationTests
{
    [Test]
    public async Task CancelledAvailabilityCheckDoesNotPublishFailedDelay()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var cancelled = false;
        try { await ConnectionHandler.RunAvailabilityCheck(cts.Token); }
        catch (OperationCanceledException) { cancelled = true; }
        await cancelled.Should().BeTrue();
    }

    [Test]
    public async Task CancellingDuringProxyHandshakeAbortsAvailabilityCheckWithoutRetrying()
    {
        var field = typeof(AppManager).GetField("_config", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var previousConfig = field.GetValue(AppManager.Instance);
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var cts = new CancellationTokenSource();
        try
        {
            var config = CoreConfigTestFactory.CreateConfig();
            config.Inbound[0].LocalPort = ((IPEndPoint)listener.LocalEndpoint).Port;
            config.SpeedTestItem.SpeedPingTestUrl = "http://test.invalid/";
            CoreConfigTestFactory.BindAppManagerConfig(config);
            var check = ConnectionHandler.RunAvailabilityCheck(cts.Token);
            using var client = await listener.AcceptTcpClientAsync(cts.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            var greeting = new byte[1];
            await client.GetStream().ReadExactlyAsync(greeting).AsTask().WaitAsync(TimeSpan.FromSeconds(5));
            // The proxy has accepted the test but deliberately never answers its SOCKS greeting.
            cts.Cancel();
            var cancelled = false;
            try { await check.WaitAsync(TimeSpan.FromSeconds(2)); }
            catch (OperationCanceledException) { cancelled = true; }
            await cancelled.Should().BeTrue();
            await listener.Pending().Should().BeFalse();
        }
        finally { field.SetValue(AppManager.Instance, previousConfig); }
    }
}
