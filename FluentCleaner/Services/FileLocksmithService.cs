using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace FluentCleaner.Services;

public sealed record LockedFileItem(string FilePath, long SizeBytes, string EntryName, string Reason)
{
    public string FormattedSize => FluentCleaner.Models.ScanResult.FormatBytes(SizeBytes);
}

public sealed class LockingProcessGroup
{
    public int ProcessId { get; }
    public string ProcessName { get; }
    public string AppDisplayName { get; }
    public bool IsSystemProtected { get; }
    public List<LockedFileItem> Files { get; }

    public LockingProcessGroup(
        int processId,
        string processName,
        string appDisplayName,
        bool isSystemProtected,
        List<LockedFileItem> files)
    {
        ProcessId         = processId;
        ProcessName       = processName;
        AppDisplayName    = appDisplayName;
        IsSystemProtected = isSystemProtected;
        Files             = files;
    }

    public int    FileCount   => Files.Count;
    public long   TotalBytes  => Files.Sum(f => f.SizeBytes);
    public string SummaryText => $"{FileCount:N0} locked {(FileCount == 1 ? "file" : "files")} · {FluentCleaner.Models.ScanResult.FormatBytes(TotalBytes)} · PID {ProcessId}";
    public bool   CanClose    => !IsSystemProtected;
}

public sealed record CleanOutcome(int RemovedCount, long FreedBytes, List<LockedFileItem> LockedFiles);

/// <summary>
/// PowerToys File Locksmith-style locked-file analyzer and unlocker.
/// Uses native ntdll.dll (NtQuerySystemInformation + NtQueryObject) with ObjectTypeIndex pre-filtering,
/// QueryDosDeviceW kernel-path mapping, stall-safe name queries, and parallel per-PID handle + module workers.
/// 100% 64-bit, NativeAOT-safe, symlink-safe, PID-reuse-safe, and self-termination-safe.
/// </summary>
public static class FileLocksmithService
{
    // Critical Windows OS processes that must never be terminated by a cleaner
    private static readonly HashSet<string> ProtectedSystemProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Idle", "Registry", "Memory Compression",
        "smss", "csrss", "wininit", "winlogon", "services", "lsass", "lsm",
        "dwm", "fontdrvhost", "sihost", "svchost", "explorer", "ctfmon",
        "RuntimeBroker", "ShellExperienceHost", "StartMenuExperienceHost",
        "SearchHost", "SearchIndexer", "MsMpEng", "NisSrv", "SecurityHealthService"
    };

    /// <summary>
    /// Identifies all processes currently holding locks on the supplied file paths using native
    /// NtQuerySystemInformation handle enumeration and process module inspection across parallel workers.
    /// </summary>
    public static Task<(List<LockingProcessGroup> Processes, List<LockedFileItem> UnattributedFiles)> FindLockingProcessesAsync(
        IReadOnlyList<LockedFileItem> lockedFiles,
        CancellationToken token = default)
        => FindLockingProcessesAsync(lockedFiles, progress: null, token);

    public static Task<(List<LockingProcessGroup> Processes, List<LockedFileItem> UnattributedFiles)> FindLockingProcessesAsync(
        IReadOnlyList<LockedFileItem> lockedFiles,
        IProgress<(int ProcessedFiles, int TotalFiles)>? progress,
        CancellationToken token = default)
        => Task.Run(() => FindLockingProcessesInternal(lockedFiles, progress, token), token);

    private static (List<LockingProcessGroup> Processes, List<LockedFileItem> UnattributedFiles) FindLockingProcessesInternal(
        IReadOnlyList<LockedFileItem> lockedFiles,
        IProgress<(int ProcessedFiles, int TotalFiles)>? progress,
        CancellationToken token)
    {
        var deduped = lockedFiles
            .AsParallel()
            .WithCancellation(token)
            .Where(f => !string.IsNullOrWhiteSpace(f.FilePath) && File.Exists(f.FilePath))
            .DistinctBy(f => f.FilePath, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int total = deduped.Count;
        if (total == 0)
        {
            progress?.Report((0, 0));
            return (new List<LockingProcessGroup>(), new List<LockedFileItem>());
        }

        progress?.Report((0, total));

        // 1. Build O(1) kernel-path -> LockedFileItem lookup via cached QueryDosDeviceW drive mappings.
        var driveMap = BuildDriveToDeviceMap();
        var kernelLookup = BuildKernelPathLookup(deduped, driveMap);
        if (kernelLookup.Count == 0)
        {
            progress?.Report((total, total));
            return (new List<LockingProcessGroup>(), deduped);
        }

        // 2. Open one temporary disk handle so we can identify the exact kernel File ObjectTypeIndex
        //    in O(1) from the handle snapshot, skipping 95%+ of non-File handles without DuplicateHandle.
        ushort? fileTypeIndex = null;
        SafeFileHandle? probeHandle = null;
        try
        {
            probeHandle = File.OpenHandle(deduped[0].FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        }
        catch { }

        byte[]? handleBuffer = null;
        try
        {
            handleBuffer = QuerySystemInformationLoop(SystemExtendedHandleInformation);
        }
        finally
        {
            // Keep probeHandle open until after we read its ObjectTypeIndex below, or dispose if query failed.
            if (handleBuffer is null)
                probeHandle?.Dispose();
        }

        var byPid = new ConcurrentDictionary<int, (string ProcName, string DisplayName, bool IsProtected, ConcurrentBag<LockedFileItem> Files)>();
        var attributedPaths = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        if (handleBuffer is not null && handleBuffer.Length >= 16)
        {
            ulong rawCount = MemoryMarshal.Read<ulong>(handleBuffer.AsSpan(0, 8));
            int entrySize = Marshal.SizeOf<SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>();
            int maxEntries = Math.Min((int)Math.Min(rawCount, int.MaxValue), (handleBuffer.Length - 16) / entrySize);
            var entries = MemoryMarshal.Cast<byte, SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX>(
                handleBuffer.AsSpan(16, maxEntries * entrySize));

            if (probeHandle is not null)
            {
                nuint selfPid = (nuint)Environment.ProcessId;
                nuint probeVal = (nuint)probeHandle.DangerousGetHandle();
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].UniqueProcessId == selfPid && entries[i].HandleValue == probeVal)
                    {
                        fileTypeIndex = entries[i].ObjectTypeIndex;
                        break;
                    }
                }
                probeHandle.Dispose();
                probeHandle = null;
            }

            // Group candidate handles by PID (skipping PID <= 4 and self, and pre-filtering by File ObjectTypeIndex)
            var handlesByPid = new Dictionary<int, List<nuint>>(256);
            int selfProcessId = Environment.ProcessId;
            for (int i = 0; i < entries.Length; i++)
            {
                ref readonly var e = ref entries[i];
                if (fileTypeIndex.HasValue && e.ObjectTypeIndex != fileTypeIndex.Value)
                    continue;

                ulong pid64 = e.UniqueProcessId;
                if (pid64 <= 4 || pid64 > int.MaxValue) continue;
                int pid = (int)pid64;
                if (pid == selfProcessId) continue;

                if (!handlesByPid.TryGetValue(pid, out var list))
                {
                    list = new List<nuint>(32);
                    handlesByPid[pid] = list;
                }
                list.Add(e.HandleValue);
            }

            // 3. Scan processes in parallel across CPU cores with stall-safe NtQueryObject workers.
            var pidGroups = handlesByPid.ToList();
            int totalPids = Math.Max(1, pidGroups.Count);
            int donePids = 0;
            long lastReportTick = 0;
            int maxParallel = Math.Clamp(Environment.ProcessorCount, 4, 16);

            Parallel.ForEach(
                pidGroups,
                new ParallelOptions { MaxDegreeOfParallelism = maxParallel, CancellationToken = token },
                () => new PidScanScratch(),
                (pidGroup, _, scratch) =>
                {
                    ScanPidHandlesAndModules(
                        pidGroup.Key,
                        pidGroup.Value,
                        fileTypeIndex.HasValue,
                        kernelLookup,
                        driveMap,
                        byPid,
                        attributedPaths,
                        scratch,
                        token);

                    int done = Interlocked.Increment(ref donePids);
                    if (progress is not null)
                    {
                        long now = Environment.TickCount64;
                        long prev = Interlocked.Read(ref lastReportTick);
                        if (done >= totalPids || (now - prev >= 60 && Interlocked.CompareExchange(ref lastReportTick, now, prev) == prev))
                        {
                            int approxFiles = Math.Min(total, (int)((long)done * total / totalPids));
                            progress.Report((approxFiles, total));
                        }
                    }

                    return scratch;
                },
                scratch => scratch.Dispose());
        }
        else
        {
            probeHandle?.Dispose();
        }

        progress?.Report((total, total));

        var unattributed = deduped
            .Where(f => !attributedPaths.ContainsKey(f.FilePath))
            .ToList();

        var groups = byPid
            .Select(kvp => new LockingProcessGroup(
                kvp.Key,
                kvp.Value.ProcName,
                kvp.Value.DisplayName,
                kvp.Value.IsProtected,
                kvp.Value.Files.DistinctBy(f => f.FilePath, StringComparer.OrdinalIgnoreCase)
                               .OrderByDescending(f => f.SizeBytes)
                               .ToList()))
            .OrderBy(g => g.IsSystemProtected)
            .ThenByDescending(g => g.TotalBytes)
            .ToList();

        return (groups, unattributed);
    }

    internal static Dictionary<string, string> BuildDriveToDeviceMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sb = new char[512];
        foreach (var drive in Environment.GetLogicalDrives())
        {
            if (drive.Length < 2) continue;
            string dosDrive = drive[..2]; // e.g. "C:"
            uint len = QueryDosDeviceW(dosDrive, sb, (uint)sb.Length);
            if (len > 0)
            {
                int nullIdx = Array.IndexOf(sb, '\0');
                string device = nullIdx > 0 ? new string(sb, 0, nullIdx) : new string(sb, 0, (int)len).TrimEnd('\0');
                if (!string.IsNullOrEmpty(device))
                    map[dosDrive] = device;
            }
        }
        return map;
    }

    internal static string? TryConvertToKernelPath(string dosPath, IReadOnlyDictionary<string, string> driveMap)
    {
        if (string.IsNullOrWhiteSpace(dosPath)) return null;
        if (dosPath.Length >= 3 && dosPath[1] == ':' && dosPath[2] == '\\')
        {
            string dosDrive = dosPath[..2];
            if (driveMap.TryGetValue(dosDrive, out var device))
                return string.Concat(device, dosPath.AsSpan(2));
        }
        return null;
    }

    internal static Dictionary<string, LockedFileItem> BuildKernelPathLookup(
        IEnumerable<LockedFileItem> files,
        IReadOnlyDictionary<string, string> driveMap)
    {
        var lookup = new Dictionary<string, LockedFileItem>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in files)
        {
            string? kpath = TryConvertToKernelPath(item.FilePath, driveMap);
            if (string.IsNullOrEmpty(kpath))
                kpath = QueryKernelNameForPath(item.FilePath);

            if (!string.IsNullOrEmpty(kpath))
                lookup.TryAdd(kpath, item);
        }
        return lookup;
    }

    private static void ScanPidHandlesAndModules(
        int pid,
        List<nuint> handleValues,
        bool typePreFiltered,
        Dictionary<string, LockedFileItem> kernelLookup,
        Dictionary<string, string> driveMap,
        ConcurrentDictionary<int, (string ProcName, string DisplayName, bool IsProtected, ConcurrentBag<LockedFileItem> Files)> byPid,
        ConcurrentDictionary<string, byte> attributedPaths,
        PidScanScratch scratch,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        IntPtr hProc = OpenProcess(PROCESS_DUP_HANDLE | PROCESS_QUERY_LIMITED_INFORMATION, false, (uint)pid);
        if (hProc == IntPtr.Zero)
        {
            hProc = OpenProcess(PROCESS_DUP_HANDLE, false, (uint)pid);
            if (hProc == IntPtr.Zero) return;
        }

        string? procName = null;
        string? displayName = null;
        IntPtr currentProc = GetCurrentProcess();

        try
        {
            foreach (nuint hVal in handleValues)
            {
                if (token.IsCancellationRequested) break;

                if (!DuplicateHandle(hProc, (IntPtr)hVal, currentProc, out IntPtr dup, 0, false, DUPLICATE_SAME_ACCESS))
                    continue;

                if (!typePreFiltered)
                {
                    if (!IsFileObjectType(dup, scratch.TypeBuffer))
                    {
                        CloseHandle(dup);
                        continue;
                    }
                }

                // Ownership of `dup` is transferred to StallSafeFileNameWorker (always closed by worker or on abandon).
                string? kernelName = scratch.NameWorker.QueryKernelFileName(dup);
                if (!string.IsNullOrEmpty(kernelName) && kernelLookup.TryGetValue(kernelName, out var item))
                {
                    EnsureProcessNames(pid, ref procName, ref displayName);
                    AddLockedItem(byPid, pid, procName ?? $"PID {pid}", displayName ?? procName ?? $"PID {pid}", item);
                    attributedPaths.TryAdd(item.FilePath, 0);
                }
            }

            // Also inspect loaded modules (DLL/EXE) for memory-mapped locks (matches PowerToys FileLocksmith.cpp).
            ScanProcessModules(pid, kernelLookup, driveMap, byPid, attributedPaths, ref procName, ref displayName);
        }
        finally
        {
            CloseHandle(hProc);
        }
    }

    private static void ScanProcessModules(
        int pid,
        Dictionary<string, LockedFileItem> kernelLookup,
        Dictionary<string, string> driveMap,
        ConcurrentDictionary<int, (string ProcName, string DisplayName, bool IsProtected, ConcurrentBag<LockedFileItem> Files)> byPid,
        ConcurrentDictionary<string, byte> attributedPaths,
        ref string? procName,
        ref string? displayName)
    {
        IntPtr hProcMod = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, (uint)pid);
        if (hProcMod == IntPtr.Zero) return;

        try
        {
            var modules = new IntPtr[512];
            uint cb = (uint)(modules.Length * IntPtr.Size);
            if (!EnumProcessModulesEx(hProcMod, modules, cb, out uint needed, LIST_MODULES_ALL))
                return;

            if (needed > cb)
            {
                modules = new IntPtr[needed / (uint)IntPtr.Size];
                if (!EnumProcessModulesEx(hProcMod, modules, needed, out needed, LIST_MODULES_ALL))
                    return;
            }

            int count = (int)(needed / (uint)IntPtr.Size);
            var pathBuf = new char[1024];
            for (int i = 0; i < count; i++)
            {
                uint len = GetModuleFileNameExW(hProcMod, modules[i], pathBuf, (uint)pathBuf.Length);
                if (len == 0) continue;

                string modPath = new string(pathBuf, 0, (int)len);
                string? kernelName = TryConvertToKernelPath(modPath, driveMap);
                if (!string.IsNullOrEmpty(kernelName) && kernelLookup.TryGetValue(kernelName, out var item))
                {
                    EnsureProcessNames(pid, ref procName, ref displayName);
                    AddLockedItem(byPid, pid, procName ?? $"PID {pid}", displayName ?? procName ?? $"PID {pid}", item);
                    attributedPaths.TryAdd(item.FilePath, 0);
                }
            }
        }
        catch { }
        finally
        {
            CloseHandle(hProcMod);
        }
    }

    private static void EnsureProcessNames(int pid, ref string? procName, ref string? displayName)
    {
        if (procName is not null && displayName is not null) return;
        try
        {
            using var p = Process.GetProcessById(pid);
            procName = p.ProcessName;
            string? friendly = null;
            try { friendly = p.MainModule?.FileVersionInfo.FileDescription; } catch { }
            displayName = string.IsNullOrWhiteSpace(friendly) ? procName : friendly.Trim();
        }
        catch
        {
            procName ??= $"PID {pid}";
            displayName ??= procName;
        }
    }

    private static void AddLockedItem(
        ConcurrentDictionary<int, (string ProcName, string DisplayName, bool IsProtected, ConcurrentBag<LockedFileItem> Files)> map,
        int pid,
        string procName,
        string displayName,
        LockedFileItem item)
    {
        var entry = map.GetOrAdd(pid, id =>
        {
            // Protect critical Windows OS processes AND FluentCleaner itself (self-termination guard)
            bool isProtected = id <= 4
                            || id == Environment.ProcessId
                            || ProtectedSystemProcesses.Contains(procName);
            return (procName, string.IsNullOrWhiteSpace(displayName) ? procName : displayName, isProtected, new ConcurrentBag<LockedFileItem>());
        });
        entry.Files.Add(item);
    }

    /// <summary>
    /// Gracefully closes a user application process (CloseMainWindow -> 2.5s wait -> single-PID Kill).
    /// Verifies expectedProcessName to prevent PID-reuse races and refuses to terminate OS processes or self.
    /// </summary>
    public static async Task<bool> CloseProcessAsync(int pid, string expectedProcessName, CancellationToken token = default)
    {
        if (pid <= 4 || pid == Environment.ProcessId)
            return false;

        try
        {
            using var proc = Process.GetProcessById(pid);
            if (ProtectedSystemProcesses.Contains(proc.ProcessName))
                return false;

            // PID reuse safety check
            if (!string.IsNullOrWhiteSpace(expectedProcessName) &&
                !string.Equals(proc.ProcessName, expectedProcessName, StringComparison.OrdinalIgnoreCase))
                return false;

            try
            {
                if (proc.MainWindowHandle != IntPtr.Zero)
                {
                    proc.CloseMainWindow();
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    cts.CancelAfter(TimeSpan.FromMilliseconds(2500));
                    try { await proc.WaitForExitAsync(cts.Token); } catch { }
                }
            }
            catch { }

            if (!proc.HasExited)
            {
                // Terminate only this exact locking PID (never kill unrelated child terminals/servers)
                proc.Kill(entireProcessTree: false);
                using var cts2 = CancellationTokenSource.CreateLinkedTokenSource(token);
                cts2.CancelAfter(TimeSpan.FromMilliseconds(3000));
                try { await proc.WaitForExitAsync(cts2.Token); } catch { }
            }

            await Task.Delay(250, token);
            return proc.HasExited;
        }
        catch (ArgumentException)
        {
            return true; // already exited
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Attempts attribute-cleared deletion and shared-handle truncation on a locked file.
    /// </summary>
    public static bool TryForceDeleteOrTruncate(string path, out long freedBytes)
    {
        freedBytes = 0;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return true;

        if (IsReparsePoint(path) || CleaningService.IsProtectedPath(path))
            return false;

        long size = 0;
        try { size = new FileInfo(path).Length; } catch { }

        try
        {
            ClearRestrictiveAttributes(path);
            File.Delete(path);
            freedBytes = size;
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (size > 0 && CleaningService.TryTruncateFile(path))
            {
                freedBytes = size;
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Clears ReadOnly, Hidden, and System file attributes so File.Delete does not fail with UnauthorizedAccessException.
    /// Skips reparse points/symlinks.
    /// </summary>
    public static void ClearRestrictiveAttributes(string path)
    {
        try
        {
            var attrs = File.GetAttributes(path);
            const FileAttributes restrictive = FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System;
            if ((attrs & restrictive) != 0 && (attrs & FileAttributes.ReparsePoint) == 0)
                File.SetAttributes(path, attrs & ~restrictive);
        }
        catch { }
    }

    private static bool IsReparsePoint(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch { return true; }
    }

    /// <summary>
    /// Schedules stubborn OS-locked files for deletion on the next Windows reboot via MoveFileExW.
    /// Rejects symlinks/reparse points and protected paths for security when running elevated.
    /// </summary>
    public static int ScheduleDeleteOnReboot(IEnumerable<string> paths)
    {
        int scheduled = 0;
        foreach (var path in paths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) continue;
            if (IsReparsePoint(path) || CleaningService.IsProtectedPath(path)) continue;

            ClearRestrictiveAttributes(path);
            if (MoveFileExW(path, null, MOVEFILE_DELAY_UNTIL_REBOOT))
                scheduled++;
        }
        return scheduled;
    }

    // --- Native Handle & Module Enumeration P/Invoke (ntdll, kernel32, psapi) ---

    private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x00000004;
    private const int SystemExtendedHandleInformation = 64;
    private const int ObjectNameInformation = 1;
    private const int ObjectTypeInformation = 2;
    private const int STATUS_INFO_LENGTH_MISMATCH = unchecked((int)0xC0000004);
    private const uint PROCESS_DUP_HANDLE = 0x0040;
    private const uint PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
    private const uint PROCESS_VM_READ = 0x0010;
    private const uint DUPLICATE_SAME_ACCESS = 0x00000002;
    private const uint FILE_TYPE_DISK = 0x0001;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint FILE_SHARE_DELETE = 0x00000004;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint LIST_MODULES_ALL = 0x03;

    [StructLayout(LayoutKind.Sequential)]
    private struct SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX
    {
        public IntPtr Object;
        public nuint UniqueProcessId;
        public nuint HandleValue;
        public uint GrantedAccess;
        public ushort CreatorBackTraceIndex;
        public ushort ObjectTypeIndex;
        public uint HandleAttributes;
        public uint Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct UNICODE_STRING
    {
        public ushort Length;
        public ushort MaximumLength;
        public IntPtr Buffer;
    }

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(
        int SystemInformationClass,
        byte[] SystemInformation,
        int SystemInformationLength,
        out int ReturnLength);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryObject(
        IntPtr Handle,
        int ObjectInformationClass,
        byte[] ObjectInformation,
        int ObjectInformationLength,
        out int ReturnLength);

    [DllImport("kernel32.dll")]
    private static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DuplicateHandle(
        IntPtr hSourceProcessHandle,
        IntPtr hSourceHandle,
        IntPtr hTargetProcessHandle,
        out IntPtr lpTargetHandle,
        uint dwDesiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        uint dwOptions);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll")]
    private static extern uint GetFileType(IntPtr hFile);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint QueryDosDeviceW(string lpDeviceName, [Out] char[] lpTargetPath, uint ucchMax);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("psapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumProcessModulesEx(
        IntPtr hProcess,
        [Out] IntPtr[] lphModule,
        uint cb,
        out uint lpcbNeeded,
        uint dwFilterFlag);

    [DllImport("psapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetModuleFileNameExW(
        IntPtr hProcess,
        IntPtr hModule,
        [Out] char[] lpFilename,
        uint nSize);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool MoveFileExW(string lpExistingFileName, string? lpNewFileName, uint dwFlags);

    private static byte[]? QuerySystemInformationLoop(int infoClass)
    {
        int size = 4 * 1024 * 1024;
        const int maxSize = 256 * 1024 * 1024;

        while (size <= maxSize)
        {
            var buf = new byte[size];
            int status = NtQuerySystemInformation(infoClass, buf, size, out int needed);
            if (status == STATUS_INFO_LENGTH_MISMATCH)
            {
                size = needed > size ? needed + (1024 * 1024) : size * 2;
                continue;
            }
            return status >= 0 ? buf : null;
        }
        return null;
    }

    private static string? QueryKernelNameForPath(string path)
    {
        IntPtr h = CreateFileW(path, 0, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, IntPtr.Zero, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS, IntPtr.Zero);
        if (h == IntPtr.Zero || h == new IntPtr(-1))
            return null;

        try
        {
            if (GetFileType(h) != FILE_TYPE_DISK) return null;
            var buf = new byte[64 * 1024];
            if (NtQueryObject(h, ObjectNameInformation, buf, buf.Length, out _) >= 0)
            {
                var ustr = MemoryMarshal.Read<UNICODE_STRING>(buf);
                if (ustr.Length > 0 && ustr.Buffer != IntPtr.Zero)
                    return Marshal.PtrToStringUni(ustr.Buffer, ustr.Length / 2);
            }
            return null;
        }
        finally
        {
            CloseHandle(h);
        }
    }

    private static bool IsFileObjectType(IntPtr handle, byte[] typeBuffer)
    {
        if (NtQueryObject(handle, ObjectTypeInformation, typeBuffer, typeBuffer.Length, out _) < 0)
            return false;

        var ustr = MemoryMarshal.Read<UNICODE_STRING>(typeBuffer);
        if (ustr.Length != 8 || ustr.Buffer == IntPtr.Zero) // "File" is 4 UTF-16 chars = 8 bytes
            return false;

        string? name = Marshal.PtrToStringUni(ustr.Buffer, 4);
        return string.Equals(name, "File", StringComparison.Ordinal);
    }

    private sealed class PidScanScratch : IDisposable
    {
        public readonly byte[] TypeBuffer = new byte[4096];
        public readonly StallSafeFileNameWorker NameWorker = new(stallTimeoutMs: 40);

        public void Dispose() => NameWorker.Dispose();
    }

    /// <summary>
    /// Dedicated worker thread per parallel scanner that executes GetFileType + NtQueryObject(ObjectNameInformation)
    /// with a stall timeout, abandoning hung pipe/driver queries without blocking the scan (PowerToys FileNameQueryWorker).
    /// </summary>
    private sealed class StallSafeFileNameWorker : IDisposable
    {
        private sealed class WorkerState
        {
            public readonly AutoResetEvent RequestEvent = new(false);
            public readonly AutoResetEvent DoneEvent = new(false);
            public readonly byte[] Buffer = new byte[64 * 1024];
            public IntPtr FileHandle;
            public int Abandoned; // 0 = active, 1 = abandoned
            public string? Result;
        }

        private readonly int _timeoutMs;
        private WorkerState? _state;

        public StallSafeFileNameWorker(int stallTimeoutMs = 40)
        {
            _timeoutMs = stallTimeoutMs;
        }

        public string? QueryKernelFileName(IntPtr handle)
        {
            if (_state is null)
            {
                _state = new WorkerState();
                var stateRef = _state;
                var thread = new Thread(() => RunWorker(stateRef))
                {
                    IsBackground = true,
                    Name = "LocksmithNameQueryWorker"
                };
                thread.Start();
            }

            var st = _state;
            st.Result = null;
            st.FileHandle = handle;
            st.RequestEvent.Set();

            if (!st.DoneEvent.WaitOne(_timeoutMs))
            {
                // Query stalled on a synchronous pipe/driver handle: abandon this worker and spin a fresh one next time.
                Interlocked.Exchange(ref st.Abandoned, 1);
                _state = null;
                return null;
            }

            CloseHandle(handle);
            return st.Result;
        }

        private static void RunWorker(WorkerState st)
        {
            while (true)
            {
                st.RequestEvent.WaitOne();
                IntPtr h = st.FileHandle;
                if (h == IntPtr.Zero)
                    break;

                string? res = null;
                try
                {
                    if (GetFileType(h) == FILE_TYPE_DISK &&
                        NtQueryObject(h, ObjectNameInformation, st.Buffer, st.Buffer.Length, out _) >= 0)
                    {
                        var ustr = MemoryMarshal.Read<UNICODE_STRING>(st.Buffer);
                        if (ustr.Length > 0 && ustr.Buffer != IntPtr.Zero)
                            res = Marshal.PtrToStringUni(ustr.Buffer, ustr.Length / 2);
                    }
                }
                catch { }

                if (Volatile.Read(ref st.Abandoned) != 0)
                {
                    CloseHandle(h);
                    break;
                }

                st.Result = res;
                st.DoneEvent.Set();
            }

            st.RequestEvent.Dispose();
            st.DoneEvent.Dispose();
        }

        public void Dispose()
        {
            if (_state is not null)
            {
                _state.FileHandle = IntPtr.Zero;
                _state.RequestEvent.Set();
                _state = null;
            }
        }
    }
}
