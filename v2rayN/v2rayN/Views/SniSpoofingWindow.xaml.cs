using ServiceLib.Services.CoreConfig;

namespace v2rayN.Views;

public partial class SniSpoofingWindow : Window
{
    public SniSpoofingItem Settings { get; private set; }

    public SniSpoofingWindow(SniSpoofingItem settings)
    {
        InitializeComponent();
        Settings = settings;
        txtDestination.Text = settings.ConnectAddress;
        txtDecoy.Text = settings.DecoySni;
        cmbFingerprint.ItemsSource = SniSpoofingService.Fingerprints;
        cmbFingerprint.SelectedItem = settings.Fingerprint;
        cmbInjector.ItemsSource = new[] { "active", "passive" };
        cmbInjector.SelectedItem = settings.Injector;
        chkFragment.IsChecked = settings.Fragment;
        txtCloudflareAddress.Text = settings.CloudflareAddress;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SniSpoofingItem
        {
            Enabled = Settings.Enabled,
            ConnectAddress = txtDestination.Text.Trim(),
            DecoySni = txtDecoy.Text.Trim(),
            Fingerprint = cmbFingerprint.SelectedItem as string ?? "firefox",
            Injector = cmbInjector.SelectedItem as string ?? "active",
            Fragment = chkFragment.IsChecked == true,
            CloudflareAddress = txtCloudflareAddress.Text.Trim(),
        };
        try
        {
            _ = SniSpoofingService.CreatePlan(new ProfileItem
            {
                ConfigType = EConfigType.VLESS, StreamSecurity = "tls", Address = "example.com", Port = 443,
            }, settings, 40443);
            CloudflareFragmentService.Validate(new ProfileItem
            {
                ConfigType = EConfigType.VLESS, StreamSecurity = "tls", Address = "example.com", Port = 443,
            }, settings.CloudflareAddress);
            Settings = settings;
            DialogResult = true;
        }
        catch (Exception ex) { txtError.Text = ex.Message; }
    }
}
