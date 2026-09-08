using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Gallery;

public abstract partial class GalleryViewModelBase : ObservableObject, IDisposable
{
    private readonly DispatcherTimer _timer;

    protected GalleryViewModelBase()
    {
        Lines = [];
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
        _timer.Tick += (_, _) => RefreshLines();
        _timer.Start();
        LogDirectory = HelperLog.LogDirectory;
        RefreshLines();
    }

    public ObservableCollection<JsonLineRow> Lines { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WrittenCaption))]
    private int writtenCount;

    [ObservableProperty] private string statusText = "";
    [ObservableProperty] private string logDirectory = "";

    public string WrittenCaption => $"Written {WrittenCount}";
    public string LogDirectoryCaption =>
        string.IsNullOrWhiteSpace(LogDirectory) ? "" : $"Log directory: {LogDirectory}";

    [RelayCommand]
    private void CopyLogDirectory()
    {
        if (!string.IsNullOrWhiteSpace(LogDirectory))
            Clipboard.SetText(LogDirectory);
        StatusText = "Log directory copied.";
    }

    [RelayCommand]
    private void ClearFeed()
    {
        Lines.Clear();
        StatusText = "Feed cleared (disk file is unchanged).";
    }

    public void RefreshLines()
    {
        HelperLog.Flush();
        var fresh = HelperLog.RecentJsonLines;
        WrittenCount = fresh.Count;
        if (Lines.Count == fresh.Count)
            return;
        Lines.Clear();
        foreach (var line in fresh)
            Lines.Add(JsonLineRow.Parse(line));
    }

    public virtual void Dispose() => _timer.Stop();
}
