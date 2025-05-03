using Avalonia.Controls;
using PluginLoader2.Config;
using PluginLoader2.Plugins;
using PluginLoader2.ViewModels;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using PluginLoader2.Plugins.List;
using Avalonia.Threading;
using Avalonia.Interactivity;

namespace PluginLoader2.Pages;

public partial class PluginsPage : UserControl
{
    private LoaderConfigModel context = new LoaderConfigModel();
    private LocalPluginList localPlugins = new LocalPluginList();
    private readonly LoaderConfig loaderConfig;
    private readonly LauncherConfig launcherConfig;
    private CancellationTokenSource pageCts = new CancellationTokenSource();
    private SemaphoreSlim contextRefreshLock = new SemaphoreSlim(1, 1);

    public PluginsPage()
    {
        InitializeComponent();
    }
    public PluginsPage(LoaderConfig loaderConfig, LauncherConfig launcherConfig)
    {
        DataContext = context;
     
        InitializeComponent();

        this.loaderConfig = loaderConfig;
        this.launcherConfig = launcherConfig;
        saveTip.IsOpen = false;

        Unloaded += OnPageUnloaded;
        Loaded += OnPageLoaded;
    }

    private void OnPageUnloaded(object sender, RoutedEventArgs e)
    {
        pageCts.Cancel();
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await RefreshModel(true);
    }

    private async Task RefreshModel(bool init = false)
    {
        if (!contextRefreshLock.Wait(0))
            return;

        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                progressRing.IsVisible = true;
                localPluginsGrid.IsHitTestVisible = false;
                githubPluginsGrid.IsHitTestVisible = false;
            });

            CancellationToken cancelToken = pageCts.Token;
            Task<PluginHubData> hubTask = DownloadPluginList(cancelToken);
            Task<bool> localTask = RefreshLocalPlugins(init, cancelToken);
            await Task.WhenAll(hubTask, localTask);

            PluginHubData hubData = await hubTask;
            bool localSuccess = await localTask;
            if (!localSuccess || hubData == null)
                return; // Canceled

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                localPluginsGrid.IsHitTestVisible = true;
                githubPluginsGrid.IsHitTestVisible = true;
                if(init)
                    context = new LoaderConfigModel(loaderConfig, localPlugins, hubData);
                else
                    context = new LoaderConfigModel(loaderConfig, localPlugins, hubData, context);
                DataContext = context;
                progressRing.IsVisible = false;
                PromptSave();
            });

        }
        finally
        {
            contextRefreshLock.Release();
        }
    }

    private void PluginsGrid_CellPointerPressed(object sender, DataGridCellPointerPressedEventArgs e)
    {
        if (Design.IsDesignMode)
            return;
        if (e.Column != null && e.Cell?.DataContext is IPluginModel plugin && "Enabled".Equals(e.Column.Header))
        {
            plugin.Enabled = !plugin.Enabled;
            PromptSave();
        }
    }

    private void PromptSave()
    {
        saveTip.IsOpen = context.HasChanged;
    }

    private void OnTipButtonClicked(FluentAvalonia.UI.Controls.TeachingTip sender, System.EventArgs args)
    {
        if (Design.IsDesignMode)
            return;
        context.Save();
        saveTip.IsOpen = false;
    }

    private void OnTipClosed(FluentAvalonia.UI.Controls.TeachingTip sender, System.EventArgs args)
    {
        if (Design.IsDesignMode)
            return;
        context.Discard();
        saveTip.IsOpen = false;
    }

}