namespace v2rayN.Views;

public partial class AddServer2Window
{
    public AddServer2Window()
    {
        InitializeComponent();

        Loaded += Window_Loaded;

        cmbCoreType.ItemsSource = Utils.GetEnumNames<ECoreType>().Where(t => t != nameof(ECoreType.v2rayN)).ToList().AppendEmpty();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(v => v.ViewModel.SelectedSource)
                .KeepNotNull()
                .Subscribe(InitializeData)
                .DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.SelectedSource.Remarks, v => v.txtRemarks.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.Address, v => v.txtAddress.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CoreType, v => v.cmbCoreType.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedSource.DisplayLog, v => v.togDisplayLog.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.PreSocksPort, v => v.txtPreSocksPort.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.IsSingboxEndpoint, v => v.togSingBoxEndpoint.IsChecked).DisposeWith(disposables);
            cmbPsiphonRegion.ItemsSource = ViewModel.PsiphonRegions;
            cmbPsiphonUpstream.ItemsSource = ViewModel.PsiphonUpstreamProfiles;
            this.Bind(ViewModel, vm => vm.PsiphonRegion, v => v.cmbPsiphonRegion.SelectedValue).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.PsiphonUseUpstream, v => v.chkPsiphonUpstream.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.PsiphonUpstreamProfileId, v => v.cmbPsiphonUpstream.SelectedValue).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.PsiphonUpstreamPort, v => v.txtPsiphonUpstreamPort.Text).DisposeWith(disposables);
            this.WhenAnyValue(v => v.ViewModel.IsPsiphon).Subscribe(value =>
            {
                panelPsiphon.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
                panelCustomTips.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
            }).DisposeWith(disposables);
            this.WhenAnyValue(v => v.ViewModel.PsiphonUseUpstream).Subscribe(value =>
            {
                cmbPsiphonUpstream.IsEnabled = value;
                txtPsiphonUpstreamPort.IsEnabled = value;
            }).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.PsiphonDefaultCmd, v => v.btnPsiphonDefault).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.BrowseServerCmd, v => v.btnBrowse).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.EditServerCmd, v => v.btnEdit).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SaveServerCmd, v => v.btnSave).DisposeWith(disposables);

            ViewModel.BrowseConfigFileInteraction.RegisterHandler(interaction =>
            {
                if (UI.OpenFileDialog(out var fileName, "Config|*.json|YAML|*.yaml;*.yml|All|*.*") != true)
                {
                    interaction.SetOutput(null);
                    return;
                }
                interaction.SetOutput(fileName);
            }).DisposeWith(disposables);
        });
        WindowsUtils.SetDarkBorder(this, AppManager.Instance.Config.UiItem.CurrentTheme);
    }

    private void InitializeData(ProfileItem profileItem)
    {
        if (profileItem.ConfigType is EConfigType.Custom)
        {
            Title = profileItem.CoreType == ECoreType.Psiphon ? "Internal Psiphon settings" : ResUI.menuAddCustomServer;
            cmbCoreType.ItemsSource = Utils.GetEnumNames<ECoreType>().Where(t => t != nameof(ECoreType.v2rayN)).ToList();
            gridCustomServer.Visibility = Visibility.Visible;
            gridCustomOutbound.Visibility = Visibility.Collapsed;
        }
        else if (profileItem.ConfigType is EConfigType.Outbound)
        {
            Title = ResUI.menuAddCustomOutboundServer;
            cmbCoreType.ItemsSource = Global.CoreTypes;
            gridCustomServer.Visibility = Visibility.Collapsed;
            gridCustomOutbound.Visibility = Visibility.Visible;
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        txtRemarks.Focus();
        if (ViewModel != null)
        {
            await ViewModel.LoadPsiphonProfiles();
        }
    }
}
