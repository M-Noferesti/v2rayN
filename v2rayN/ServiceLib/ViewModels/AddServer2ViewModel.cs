namespace ServiceLib.ViewModels;

public partial class AddServer2ViewModel : MyReactiveObject, ICloseable
{
    public event EventHandler? RequestClose;

    public Interaction<RxVoid, string?> BrowseConfigFileInteraction { get; } = new();

    [Reactive]
    public partial ProfileItem SelectedSource { get; set; }

    [Reactive]
    public partial string? CoreType { get; set; }

    [Reactive]
    public partial bool IsSingboxEndpoint { get; set; }

    [Reactive]
    public partial string PsiphonRegion { get; set; } = "";

    [Reactive]
    public partial bool IsPsiphon { get; set; }

    [Reactive]
    public partial int? PreSocksPort { get; set; }

    [Reactive]
    public partial bool PsiphonUseUpstream { get; set; }

    [Reactive]
    public partial string? PsiphonUpstreamProfileId { get; set; }

    [Reactive]
    public partial int PsiphonUpstreamPort { get; set; } = 1089;

    public ObservableCollection<ProfileItem> PsiphonUpstreamProfiles { get; } = [];

    public async Task LoadPsiphonProfiles()
    {
        var selected = PsiphonUpstreamProfileId;
        PsiphonUpstreamProfiles.Clear();
        foreach (var profile in (await AppManager.Instance.ProfileItems("")) ?? [])
        {
            if (PsiphonConfigService.IsSupportedUpstream(profile))
            {
                PsiphonUpstreamProfiles.Add(profile);
            }
        }
        PsiphonUpstreamProfileId = selected;
    }

    public IReadOnlyList<PsiphonConfigService.RegionOption> PsiphonRegions => PsiphonConfigService.Regions;

    public ReactiveCommand<RxVoid, RxVoid> BrowseServerCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> EditServerCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SaveServerCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> PsiphonDefaultCmd { get; }
    public bool IsModified { get; set; }

    public AddServer2ViewModel(ProfileItem profileItem)
    {
        _config = AppManager.Instance.Config;
        PsiphonDefaultCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            try
            {
                var path = Utils.GetTempPath($"psiphon-{Utils.GetGuid(false)}.json");
                await File.WriteAllTextAsync(path, EmbedUtils.GetEmbedText("ServiceLib.Sample.psiphon_default"));
                try { await BrowseServer(path); }
                finally { File.Delete(path); }
                CoreType = nameof(ECoreType.Psiphon);
                if (PreSocksPort is not > 0)
                {
                    PreSocksPort = PsiphonConfigService.FindAvailablePort();
                }
            }
            catch (Exception ex)
            {
                NoticeManager.Instance.Enqueue($"Psiphon setup: {ex.Message}");
            }
        });

        BrowseServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            var fileName = await BrowseConfigFileInteraction.HandleSafe(RxVoid.Default);
            if (fileName.IsNullOrEmpty())
            {
                return;
            }
            await BrowseServer(fileName);
        });
        EditServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await EditServer();
        });
        SaveServerCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            await SaveServerAsync();
        });

        SelectedSource = profileItem.IndexId.IsNullOrEmpty() ? profileItem : JsonUtils.DeepCopy(profileItem);
        PreSocksPort = SelectedSource.PreSocksPort;
        var coreStr = SelectedSource?.CoreType?.ToString();
        coreStr = coreStr.IsNullOrEmpty() ? Global.CoreTypes.FirstOrDefault() : coreStr;
        CoreType = coreStr;
        PsiphonRegion = SelectedSource?.GetProtocolExtra()?.PsiphonRegion ?? "";
        PsiphonUseUpstream = SelectedSource?.GetProtocolExtra()?.PsiphonUseUpstream ?? false;
        PsiphonUpstreamProfileId = SelectedSource?.GetProtocolExtra()?.PsiphonUpstreamProfileId;
        PsiphonUpstreamPort = SelectedSource?.GetProtocolExtra()?.PsiphonUpstreamPort ?? PsiphonConfigService.FindAvailablePort();
        if (SelectedSource?.GetProtocolExtra()?.PsiphonRegion == null && CoreType == nameof(ECoreType.Psiphon))
        {
            try
            {
                var path = File.Exists(SelectedSource.Address) ? SelectedSource.Address : Utils.GetConfigPath(SelectedSource.Address);
                PsiphonRegion = JsonNode.Parse(File.ReadAllText(path))?["EgressRegion"]?.GetValue<string>() ?? "";
            }
            catch { /* Import errors are shown when saving or connecting. */ }
        }
        this.WhenAnyValue(x => x.CoreType).Subscribe(value =>
        {
            IsPsiphon = value == nameof(ECoreType.Psiphon) && SelectedSource.ConfigType == EConfigType.Custom;
            if (IsPsiphon && PreSocksPort is not > 0)
            {
                PreSocksPort = PsiphonConfigService.FindAvailablePort();
            }
        });
        IsSingboxEndpoint = SelectedSource?.GetProtocolExtra()?.IsSingboxEndpoint ?? false;
    }

    private async Task SaveServerAsync()
    {
        var remarks = SelectedSource.Remarks;
        if (remarks.IsNullOrEmpty())
        {
            NoticeManager.Instance.Enqueue(ResUI.PleaseFillRemarks);
            return;
        }

        if (SelectedSource.Address.IsNullOrEmpty())
        {
            NoticeManager.Instance.Enqueue(ResUI.FillServerAddressCustom);
            return;
        }
        SelectedSource.CoreType = CoreType.IsNullOrEmpty() ? null : Enum.Parse<ECoreType>(CoreType);
        SelectedSource.PreSocksPort = PreSocksPort;
        if (IsPsiphon)
        {
            try
            {
                var path = File.Exists(SelectedSource.Address) ? SelectedSource.Address : Utils.GetConfigPath(SelectedSource.Address);
                PsiphonConfigService.CreateConfig(await File.ReadAllTextAsync(path), PsiphonRegion,
                    SelectedSource.PreSocksPort ?? 0, Utils.GetBinConfigPath("psiphon-data"),
                    PsiphonUseUpstream ? PsiphonUpstreamPort : null);
                if (PsiphonUseUpstream && string.IsNullOrWhiteSpace(PsiphonUpstreamProfileId))
                {
                    throw new ArgumentException("Choose an upstream V2Ray profile.");
                }
            }
            catch (Exception ex)
            {
                NoticeManager.Instance.Enqueue($"Psiphon: {ex.Message}");
                return;
            }
        }
        SelectedSource.SetProtocolExtra(SelectedSource?.GetProtocolExtra() with
        {
            PsiphonRegion = IsPsiphon ? PsiphonRegion : SelectedSource.GetProtocolExtra().PsiphonRegion,
            PsiphonUseUpstream = PsiphonUseUpstream,
            PsiphonUpstreamProfileId = PsiphonUpstreamProfileId,
            PsiphonUpstreamPort = PsiphonUpstreamPort,
            IsSingboxEndpoint = IsSingboxEndpoint ? true : null,
        });

        var saveResult = IsPsiphon && SelectedSource.IndexId.IsNullOrEmpty()
            ? await ConfigHandler.AddCustomServer(_config, SelectedSource, false)
            : await ConfigHandler.EditCustomServer(_config, SelectedSource);
        if (saveResult == 0)
        {
            NoticeManager.Instance.Enqueue(ResUI.OperationSuccess);
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            NoticeManager.Instance.Enqueue(ResUI.OperationFailed);
        }
    }

    public async Task BrowseServer(string fileName)
    {
        if (fileName.IsNullOrEmpty())
        {
            return;
        }

        var item = await AppManager.Instance.GetProfileItem(SelectedSource.IndexId);
        item ??= SelectedSource;
        item.Address = fileName;
        var result = item.ConfigType == EConfigType.Outbound ? await ConfigHandler.AddCustomOutboundServer(_config, item, false) : await ConfigHandler.AddCustomServer(_config, item, false);
        if (result == 0)
        {
            NoticeManager.Instance.Enqueue(ResUI.SuccessfullyImportedCustomServer);
            if (item.IndexId.IsNotEmpty())
            {
                SelectedSource = JsonUtils.DeepCopy(item);
            }
            IsModified = true;
        }
        else
        {
            NoticeManager.Instance.Enqueue(ResUI.FailedImportedCustomServer);
        }
    }

    private async Task EditServer()
    {
        var address = SelectedSource.Address;
        if (address.IsNullOrEmpty())
        {
            NoticeManager.Instance.Enqueue(ResUI.FillServerAddressCustom);
            return;
        }

        address = Utils.GetConfigPath(address);
        if (File.Exists(address))
        {
            ProcUtils.ProcessStart(address);
        }
        else
        {
            NoticeManager.Instance.Enqueue(ResUI.FailedReadConfiguration);
        }
        await Task.CompletedTask;
    }
}
