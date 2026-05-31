using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Linq;
using System.Threading.Tasks;

namespace FileOrganizer.Gui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        BrowseButton.Click += async (_, _) => await BrowseFolderAsync();
    }

    private async Task BrowseFolderAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Source Directory",
            AllowMultiple = false
        });

        var folder = folders?.FirstOrDefault();
        if (folder != null)
        {
            var path = folder.TryGetLocalPath();
            if (path != null && DataContext is ViewModels.MainWindowViewModel vm)
                vm.SourceDirectory = path;
        }
    }
}
