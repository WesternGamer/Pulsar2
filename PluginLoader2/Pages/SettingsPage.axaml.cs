using Avalonia.Controls;
using Avalonia.Interactivity;
using PluginLoader2.Config;
using PluginLoader2.ViewModels;
using System.ComponentModel;
using System.IO;

namespace PluginLoader2.Pages;

public partial class SettingsPage : UserControl
{
    private readonly LauncherConfigModel context;

    public SettingsPage()
    {
        InitializeComponent();
    }

    public SettingsPage(LauncherConfig config)
    {
        InitializeComponent();

        context = new LauncherConfigModel(config);
        context.PropertyChanged += Context_PropertyChanged;
        DataContext = context;
    }

    private void PromptSave()
    {
        saveTip.IsOpen = true;
    }

    private void Context_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        PromptSave();
    }

    private void OnTipButtonClicked(FluentAvalonia.UI.Controls.TeachingTip sender, System.EventArgs args)
    {
        context.Save();
        saveTip.IsOpen = false;
    }

    private void OnTipClosed(FluentAvalonia.UI.Controls.TeachingTip sender, System.EventArgs args)
    {
        context.Discard();
        saveTip.IsOpen = false;
    }

    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
         deleteSelectedFolderButton.IsEnabled = pluginFoldersList.SelectedItem != null;
    }

    private void DeleteFolderClick(object sender, RoutedEventArgs e)
    {
        if(pluginFoldersList.SelectedItem is string selectedFolder)
        {
            context.LocalPluginRepositories.Remove(selectedFolder);
            PromptSave();
        }
    }

    private async void AddFolderClick(object sender, RoutedEventArgs e)
    {
        string folder = await MessageBox.OpenFolder(this, "Open Plugins Folder");
        if(folder != null && Directory.Exists(folder))
        {
            context.LocalPluginRepositories.Add(folder);
            PromptSave();
        }
    }

    private void CountdownChanged(object sender, RoutedEventArgs e)
    {
        PromptSave();
    }
}