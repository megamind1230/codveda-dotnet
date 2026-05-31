using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Services;
using Serilog;

namespace FileOrganizer.Gui.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ILogger _logger;
    private readonly FileScanner _scanner;
    private readonly NamedPatternResolver _patternResolver;
    private readonly FileMover _mover;

    public MainWindowViewModel()
    {
        _logger = Log.ForContext<MainWindowViewModel>();
        _scanner = new FileScanner(Log.ForContext<FileScanner>());
        _patternResolver = new NamedPatternResolver(Log.ForContext<NamedPatternResolver>());
        _mover = new FileMover(Log.ForContext<FileMover>());
    }

    [ObservableProperty]
    private string _sourceDirectory = string.Empty;

    [ObservableProperty]
    private string _currentPattern = string.Empty;

    [ObservableProperty]
    private string _containerName = string.Empty;

    public ObservableCollection<string> NamedPatterns { get; } = new();

    public ObservableCollection<FileItemViewModel> FileItems { get; } = new();

    [ObservableProperty]
    private bool _isDryRun = true;

    [ObservableProperty]
    private double _progressValue;

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isOrganizing;

    [ObservableProperty]
    private bool _isAllSelected = true;

    [ObservableProperty]
    private bool _useRegex;

    [RelayCommand]
    private void Reset()
    {
        _logger.Information("Reset triggered");
        SourceDirectory = string.Empty;
        CurrentPattern = string.Empty;
        ContainerName = string.Empty;
        NamedPatterns.Clear();
        FileItems.Clear();
        IsDryRun = true;
        ProgressValue = 0;
        StatusText = "Ready";
        IsOrganizing = false;
        IsAllSelected = true;
        UseRegex = false;
    }

partial void OnSourceDirectoryChanged(string value)
{
    _logger.Information("Source directory changed to {Directory}", value);
    RunPreview();
}

partial void OnCurrentPatternChanged(string value)
{
    UpdateFileMatching(value, UseRegex);
}

partial void OnUseRegexChanged(bool value)
{
    UpdateFileMatching(CurrentPattern, value);
}

private void UpdateFileMatching(string? pattern, bool useRegex)
{
    if (string.IsNullOrEmpty(pattern) || FileItems.Count == 0)
    {
        foreach (var item in FileItems)
            item.IsCurrentlyMatched = false;
        return;
    }

    foreach (var item in FileItems)
    {
        if (useRegex)
        {
            try
            {
                item.IsCurrentlyMatched = System.Text.RegularExpressions.Regex.IsMatch(
                    item.FileName, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                item.IsCurrentlyMatched = false;
            }
        }
        else
        {
            item.IsCurrentlyMatched = item.FileName.Contains(pattern, StringComparison.OrdinalIgnoreCase);
        }
    }
}

    [RelayCommand]
    private async Task ContainerizeAsync()
    {
        if (string.IsNullOrEmpty(SourceDirectory) || !Directory.Exists(SourceDirectory))
        {
            StatusText = "Invalid source directory";
            _logger.Warning("Containerize failed: invalid source directory {Directory}", SourceDirectory);
            return;
        }

        var containerName = ContainerName?.Trim();
        if (string.IsNullOrEmpty(containerName))
        {
            StatusText = "Enter a container folder name";
            return;
        }

        var selected = FileItems.Where(f => f.IsSelected).Select(f => f.FileName).ToHashSet();
        if (selected.Count == 0)
        {
            StatusText = "No files selected";
            return;
        }

        IsOrganizing = true;
        ProgressValue = 0;
        StatusText = "Adding to container...";

        try
        {
            var allEntries = _scanner.Scan(SourceDirectory);
            var entries = allEntries.Where(e => selected.Contains(e.FileName)).ToList();

            foreach (var entry in entries)
                entry.TargetFolder = containerName;

            _logger.Information("Containerize: moving {Count} files to {Container}", entries.Count, containerName);

            var progress = new Progress<double>(value =>
            {
                ProgressValue = value;
            });

            await Task.Run(() =>
            {
                _mover.Move(entries, SourceDirectory, IsDryRun, progress);
            });

            StatusText = IsDryRun
                ? $"Preview: {entries.Count} files → {containerName}"
                : $"Done: {entries.Count} files → {containerName}";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Containerize failed");
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsOrganizing = false;
            ContainerName = string.Empty;
            RunPreview();
        }
    }

    [RelayCommand]
    private void AddPattern()
    {
        var pattern = CurrentPattern?.Trim();
        if (!string.IsNullOrEmpty(pattern) && !NamedPatterns.Contains(pattern))
        {
            NamedPatterns.Add(pattern);
            _logger.Information("Added pattern: {Pattern}", pattern);
            CurrentPattern = string.Empty;
        }
    }

    [RelayCommand]
    private void RemovePattern(string pattern)
    {
        NamedPatterns.Remove(pattern);
        _logger.Information("Removed pattern: {Pattern}", pattern);
    }

    [RelayCommand]
    private async Task CreatePatternsAsync()
    {
        if (string.IsNullOrEmpty(SourceDirectory) || !Directory.Exists(SourceDirectory))
        {
            StatusText = "Invalid source directory";
            _logger.Warning("CreatePatterns failed: invalid source directory {Directory}", SourceDirectory);
            return;
        }

        var patterns = NamedPatterns.ToList();
        if (patterns.Count == 0)
        {
            StatusText = "Add at least one pattern tag";
            return;
        }

        var selected = FileItems.Where(f => f.IsSelected).Select(f => f.FileName).ToHashSet();
        if (selected.Count == 0)
        {
            StatusText = "No files selected";
            return;
        }

        IsOrganizing = true;
        ProgressValue = 0;
        StatusText = "Creating patterns...";

        try
        {
            var allEntries = _scanner.Scan(SourceDirectory);
            var entries = allEntries.Where(e => selected.Contains(e.FileName)).ToList();

            var matched = _patternResolver.Match(entries, patterns, false, UseRegex);
            if (matched.Count == 0)
            {
                StatusText = "No files matched the patterns";
                return;
            }

            _logger.Information("CreatePatterns: matched and moving {Count} files", matched.Count);

            var progress = new Progress<double>(value =>
            {
                ProgressValue = value;
            });

            await Task.Run(() =>
            {
                _mover.Move(matched, SourceDirectory, IsDryRun, progress);
            });

            StatusText = IsDryRun
                ? $"Preview: {matched.Count} files matched patterns"
                : $"Done: {matched.Count} files organized by patterns";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "CreatePatterns failed");
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsOrganizing = false;
            RunPreview();
        }
    }

    partial void OnIsAllSelectedChanged(bool value)
    {
        foreach (var item in FileItems)
            item.IsSelected = value;
    }

    [RelayCommand]
    private async Task OrganizeAsync()
    {
        if (string.IsNullOrEmpty(SourceDirectory) || !Directory.Exists(SourceDirectory))
        {
            StatusText = "Invalid source directory";
            _logger.Warning("Organize failed: invalid source directory {Directory}", SourceDirectory);
            return;
        }

        IsOrganizing = true;
        ProgressValue = 0;
        StatusText = "Organizing...";

        try
        {
            var entries = PrepareEntries();
            var selected = FileItems.Where(f => f.IsSelected).Select(f => f.FileName).ToHashSet();
            entries = entries.Where(e => selected.Contains(e.FileName)).ToList();

            if (entries.Count == 0)
            {
                StatusText = "No files selected";
                return;
            }

            _logger.Information("Organize: moving {Count} files (patterns first, then by category)", entries.Count);

            var progress = new Progress<double>(value =>
            {
                ProgressValue = value;
            });

            await Task.Run(() =>
            {
                _mover.Move(entries, SourceDirectory, IsDryRun, progress);
            });

            var namedCount = entries.Count(e => e.IsMatchedByPattern);
            var categorizedCount = entries.Count - namedCount;
            StatusText = IsDryRun
                ? $"Preview: {entries.Count} files ({namedCount} named, {categorizedCount} categorized)"
                : $"Done: {entries.Count} files organized";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Organize failed");
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsOrganizing = false;
            RunPreview();
        }
    }

    [RelayCommand]
    private async Task ReverseAsync()
    {
        if (string.IsNullOrEmpty(SourceDirectory) || !Directory.Exists(SourceDirectory))
        {
            StatusText = "Invalid source directory";
            _logger.Warning("Reverse failed: invalid source directory {Directory}", SourceDirectory);
            return;
        }

        IsOrganizing = true;
        ProgressValue = 0;
        StatusText = "Reversing...";

        try
        {
            _logger.Information("Reverse: moving files from subfolders back to source");

            var progress = new Progress<double>(value =>
            {
                ProgressValue = value;
            });

            (int totalFiles, long totalSize) result = (0, 0);

            await Task.Run(() =>
            {
                result = _mover.Unorganize(SourceDirectory, IsDryRun, progress);
            });

            StatusText = IsDryRun
                ? $"Preview: {result.totalFiles} files to unorganize"
                : $"Done: {result.totalFiles} files unorganized";
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Reverse failed");
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsOrganizing = false;
            RunPreview();
        }
    }

    private List<FileEntry> PrepareEntries()
    {
        var allEntries = _scanner.Scan(SourceDirectory);
        if (allEntries.Count == 0)
            return allEntries;

        var patterns = NamedPatterns.ToList();
        var matchedEntries = new List<FileEntry>();
        var remainingEntries = allEntries;

        if (patterns.Count > 0)
        {
            matchedEntries = _patternResolver.Match(allEntries, patterns, false, UseRegex);
            remainingEntries = allEntries.Except(matchedEntries).ToList();
        }

        foreach (var entry in remainingEntries)
        {
            var category = CategoryResolver.Resolve(entry.Extension);
            entry.Category = category;
            entry.TargetFolder = category;
        }

        var result = new List<FileEntry>();
        result.AddRange(matchedEntries);
        result.AddRange(remainingEntries);
        return result;
    }

    private void RunPreview()
    {
        FileItems.Clear();

        if (string.IsNullOrEmpty(SourceDirectory) || !Directory.Exists(SourceDirectory))
            return;

        var entries = PrepareEntries();
        foreach (var entry in entries)
        {
            FileItems.Add(new FileItemViewModel
            {
                FileName = entry.FileName,
                Extension = entry.Extension,
                TargetFolder = entry.TargetFolder,
                SizeBytes = entry.SizeBytes
            });
        }

        IsAllSelected = true;
        StatusText = $"{FileItems.Count(f => f.IsSelected)}/{FileItems.Count} files selected";

        UpdateFileMatching(CurrentPattern, UseRegex);
    }
}
