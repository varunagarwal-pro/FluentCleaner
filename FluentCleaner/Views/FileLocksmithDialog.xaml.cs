using FluentCleaner.Models;
using FluentCleaner.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentCleaner.Views;

public sealed partial class FileLocksmithDialog : ContentDialog
{
    private List<LockedFileItem> _remaining;
    private List<LockingProcessGroup> _processGroups = new();
    private bool _isBusy;
    private CancellationTokenSource? _scanCts;

    public int  AdditionalRemovedCount { get; private set; }
    public long AdditionalFreedBytes   { get; private set; }
    public IReadOnlyList<LockedFileItem> RemainingLockedFiles => _remaining;

    public FileLocksmithDialog(IReadOnlyList<LockedFileItem> lockedFiles)
    {
        InitializeComponent();
        _remaining = lockedFiles.DistinctBy(f => f.FilePath, StringComparer.OrdinalIgnoreCase).ToList();
        Loaded += async (_, _) =>
        {
            try { await RefreshLocksAsync(); }
            catch (OperationCanceledException) { }
            catch (Exception ex) { ShowActionInfo(InfoBarSeverity.Error, "Lock Analysis Error", ex.Message); }
        };
        Closing  += (_, _) => _scanCts?.Cancel();
        Unloaded += (_, _) =>
        {
            _scanCts?.Cancel();
            _scanCts?.Dispose();
            _scanCts = null;
        };
    }

    private void SetBusy(bool busy)
    {
        _isBusy                     = busy;
        LoadingBar.Visibility       = busy ? Visibility.Visible : Visibility.Collapsed;
        BtnRetryAll.IsEnabled       = !busy && _remaining.Count > 0;
        BtnScheduleReboot.IsEnabled = !busy && _remaining.Count > 0;
        ProcessList.IsHitTestVisible = !busy;
    }

    private async Task RefreshLocksAsync()
    {
        _scanCts?.Cancel();
        _scanCts?.Dispose();
        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;

        SetBusy(true);
        try
        {
            _remaining = await Task.Run(() => _remaining.AsParallel().Where(f => File.Exists(f.FilePath)).ToList(), token);
            UpdateSummaryText();

            if (_remaining.Count == 0)
            {
                ProcessList.ItemsSource       = Array.Empty<LockingProcessGroup>();
                EmptyProcessesText.Visibility = Visibility.Collapsed;
                ShowActionInfo(InfoBarSeverity.Success, "All Locked Files Cleared", "Every previously locked file has been removed or excluded.");
                return;
            }

            LoadingBar.IsIndeterminate = false;
            LoadingBar.Value           = 0;
            var progress = new Progress<(int Processed, int Total)>(p =>
            {
                if (p.Total <= 0) return;
                double pct = p.Processed * 100.0 / p.Total;
                LoadingBar.Value       = pct;
                DialogSummaryText.Text = $"Analyzing locks: {p.Processed:N0} / {p.Total:N0} files ({(int)pct}%)…";
            });

            var (processes, _)            = await FileLocksmithService.FindLockingProcessesAsync(_remaining, progress, token);
            _processGroups                = processes;
            ProcessList.ItemsSource       = _processGroups;
            EmptyProcessesText.Visibility = _processGroups.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateSummaryText();
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void UpdateSummaryText()
    {
        long totalBytes = _remaining.Sum(f => f.SizeBytes);
        DialogSummaryText.Text = _remaining.Count > 0
            ? $"{_remaining.Count:N0} locked files remaining · {ScanResult.FormatBytes(totalBytes)}"
            : "0 locked files remaining";
    }

    private async void CloseAndDelete_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || sender is not Button { Tag: LockingProcessGroup group }) return;

        SetBusy(true);
        try
        {
            bool closed = await FileLocksmithService.CloseProcessAsync(group.ProcessId, group.ProcessName);

            int deleted = 0;
            long freed  = 0;
            var snapshot = group.Files.ToList();
            LoadingBar.IsIndeterminate = true;
            await Task.Run(() =>
            {
                Parallel.ForEach(
                    snapshot,
                    new ParallelOptions { MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 4, 16) },
                    item =>
                    {
                        if (FileLocksmithService.TryForceDeleteOrTruncate(item.FilePath, out long b))
                        {
                            Interlocked.Increment(ref deleted);
                            Interlocked.Add(ref freed, b);
                        }
                    });
            });

            AdditionalRemovedCount += deleted;
            AdditionalFreedBytes   += freed;

            ShowActionInfo(
                deleted > 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning,
                closed ? $"Closed {group.AppDisplayName}" : $"Could not close {group.AppDisplayName}",
                deleted > 0
                    ? $"Deleted {deleted:N0} files and reclaimed {ScanResult.FormatBytes(freed)}."
                    : "Files remained locked after closing. Try running as administrator or schedule delete on reboot.");

            var deletedPaths = await Task.Run(() => snapshot
                .Where(item => !File.Exists(item.FilePath))
                .Select(item => item.FilePath)
                .ToHashSet(StringComparer.OrdinalIgnoreCase));

            if (deletedPaths.Count > 0)
            {
                PrunePathsInMemory(deletedPaths);
            }
            else
            {
                UpdateSummaryText();
            }
        }
        catch (Exception ex)
        {
            ShowActionInfo(InfoBarSeverity.Error, "Unlock Failed", ex.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void PrunePathsInMemory(HashSet<string> pathsToRemove)
    {
        _remaining.RemoveAll(r => pathsToRemove.Contains(r.FilePath));
        foreach (var pg in _processGroups)
            pg.Files.RemoveAll(f => pathsToRemove.Contains(f.FilePath));
        _processGroups.RemoveAll(pg => pg.Files.Count == 0);

        ProcessList.ItemsSource       = null;
        ProcessList.ItemsSource       = _processGroups;
        EmptyProcessesText.Visibility = _remaining.Count > 0 && _processGroups.Count == 0
            ? Visibility.Visible
            : Visibility.Collapsed;
        UpdateSummaryText();

        if (_remaining.Count == 0)
            ShowActionInfo(InfoBarSeverity.Success, "All Locked Files Cleared", "Every previously locked file has been removed or excluded.");
    }

    private void ExcludeLockedFile_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy || sender is not Button { Tag: LockedFileItem item }) return;

        try
        {
            var dir  = Path.GetDirectoryName(item.FilePath);
            var file = Path.GetFileName(item.FilePath);
            if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(file)) return;

            var rule = $"FILE|{dir}|{file}";
            var settings = AppSettings.Instance;
            if (!settings.GlobalExclusions.Contains(rule, StringComparer.OrdinalIgnoreCase))
            {
                settings.GlobalExclusions.Add(rule);
                settings.GlobalExclusionsEnabled = true;
                settings.Save();
            }

            ShowActionInfo(InfoBarSeverity.Informational, "Excluded File", $"Added {file} to Global Exclusions.");
            PrunePathsInMemory(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { item.FilePath });
        }
        catch (Exception ex)
        {
            ShowActionInfo(InfoBarSeverity.Error, "Exclusion Failed", ex.Message);
            SetBusy(false);
        }
    }

    private async void RetryAll_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;

        SetBusy(true);
        try
        {
            int deleted  = 0;
            long freed   = 0;
            var snapshot = _remaining.ToList();
            LoadingBar.IsIndeterminate = true;

            await Task.Run(() =>
            {
                Parallel.ForEach(
                    snapshot,
                    new ParallelOptions { MaxDegreeOfParallelism = Math.Clamp(Environment.ProcessorCount, 4, 16) },
                    item =>
                    {
                        if (FileLocksmithService.TryForceDeleteOrTruncate(item.FilePath, out long b))
                        {
                            Interlocked.Increment(ref deleted);
                            Interlocked.Add(ref freed, b);
                        }
                    });
            });

            AdditionalRemovedCount += deleted;
            AdditionalFreedBytes   += freed;

            ShowActionInfo(
                deleted > 0 ? InfoBarSeverity.Success : InfoBarSeverity.Informational,
                "Retry Complete",
                deleted > 0
                    ? $"Freed {ScanResult.FormatBytes(freed)} across {deleted:N0} files."
                    : "Remaining files are still held open by active processes or Windows services.");

            await RefreshLocksAsync();
        }
        catch (Exception ex)
        {
            ShowActionInfo(InfoBarSeverity.Error, "Retry Failed", ex.Message);
            SetBusy(false);
        }
    }

    private void ScheduleReboot_Click(object sender, RoutedEventArgs e)
    {
        if (_isBusy) return;
        int queued = FileLocksmithService.ScheduleDeleteOnReboot(_remaining.Select(r => r.FilePath));
        ShowActionInfo(
            queued > 0 ? InfoBarSeverity.Success : InfoBarSeverity.Warning,
            queued > 0 ? "Scheduled for Next Reboot" : "Administrator Rights Needed",
            queued > 0
                ? $"Queued {queued:N0} locked files for automatic Windows deletion on next restart."
                : "Windows requires administrator privileges to schedule PendingFileRenameOperations on reboot.");
    }

    private void ShowActionInfo(InfoBarSeverity severity, string title, string message)
    {
        ActionInfoBar.Severity = severity;
        ActionInfoBar.Title    = title;
        ActionInfoBar.Message  = message;
        ActionInfoBar.IsOpen   = true;
    }
}
