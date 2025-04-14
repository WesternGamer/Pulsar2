using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using FluentAvalonia.UI.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PluginLoader2;

class MessageBox
{
    internal static async Task<string> OpenFolder(Visual parent, string title)
    {
        var folders = await TopLevel.GetTopLevel(parent).StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = title,
            AllowMultiple = false,
        });
        return folders?.FirstOrDefault()?.TryGetLocalPath();
    }

    internal static async Task<string> OpenFile(Visual parent, string title, params FilePickerFileType[] filter)
    {
        var files = await TopLevel.GetTopLevel(parent).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = filter
        });
        return files?.FirstOrDefault()?.TryGetLocalPath();
    }

    internal static async Task<IEnumerable<string>> OpenFiles(Visual parent, string title, params FilePickerFileType[] filter)
    {
        var files = await TopLevel.GetTopLevel(parent).StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions()
        {
            Title = title,
            AllowMultiple = true,
            FileTypeFilter = filter
        });
        if (files == null)
            return [];
        return files.Select(x => x.TryGetLocalPath()).Where(x => x != null);
    }

    internal static async Task Show(string title, string msg)
    {
        ContentDialog dialog = new ContentDialog()
        {
            Title = title,
            PrimaryButtonText = "OK",
            Content = msg,
        };

        await dialog.ShowAsync();
    }

    internal static async Task<bool> ShowChoice(string title, string msg)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            PrimaryButtonText = "OK",
            SecondaryButtonText = "Cancel",
            Content = msg
        };

        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    internal static async Task<string> ShowInput(string title, string msg)
    {
        ContentDialog dialog = new ContentDialog
        {
            Title = title,
            PrimaryButtonText = "OK",
            SecondaryButtonText = "Cancel",
        };
        StackPanel content = new StackPanel();
        dialog.Content = content;
        content.Children.Add(new Label() { Content = msg });
        TextBox input = new TextBox();
        input.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
        content.Children.Add(input);

        ContentDialogResult result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return null;

        if (string.IsNullOrWhiteSpace(input.Text))
            return string.Empty;
        return input.Text;
    }
}