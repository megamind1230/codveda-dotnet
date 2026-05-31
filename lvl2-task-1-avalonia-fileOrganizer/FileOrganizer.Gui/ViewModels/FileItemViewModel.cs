using CommunityToolkit.Mvvm.ComponentModel;

namespace FileOrganizer.Gui.ViewModels;

public partial class FileItemViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isCurrentlyMatched;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [ObservableProperty]
    private string _extension = string.Empty;

    [ObservableProperty]
    private string _targetFolder = string.Empty;

    [ObservableProperty]
    private long _sizeBytes;

    public string SizeFormatted => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024.0):F1} MB"
    };
}
