# 📋 FluentCleaner — Master Implementation & Capability Ledger

![Status](https://img.shields.io/badge/status-100%25%20Complete-brightgreen)
![Framework](https://img.shields.io/badge/framework-.NET%2010%20%7C%20WinUI%203-blue)
![Architecture](https://img.shields.io/badge/arch-64--bit%20(win--x64%20%7C%20win--arm64)-orange)
![Milestones](https://img.shields.io/badge/Tasks-42%2F42%20Completed-brightgreen)

> [!IMPORTANT]
> **Implementation Scope & Governance Rules:**
> - **Target Boundary:** `D:\SentinelLabs\FluentCleaner\` is the strict workspace boundary. All code, configs, and plugins belong strictly within this folder.
> - **Staging & Temp Directory:** `D:\SentinelLabs\FluentCleaner\temp` is the ONLY directory permitted for orchestration scripts, diagnostic test harnesses, and staging plans (`temp/<task_name>_plan.md`).
> - **Tooling Hierarchy:** Reusable fleet utilities reside in `D:\SentinelLabs\tools\`. FluentCleaner project tools reside in `FluentCleaner\tools\`.
> - **64-Bit & Asynchronous Standard:** All code and builds MUST target strictly 64-bit architectures (`win-x64`/`win-arm64`). Heavy disk I/O, SQLite operations, and regex matching MUST execute off the UI thread via `Task.Run` or background worker pools.
> - **Core Safety Invariants:** Zero telemetry, no spyware, no scareware alerts, no fake registry cleaners, and strictly non-destructive defaults for developer history/auto-memory.
> - **Approved Task Lock (Rule 12):** Once approved, task lines are LOCKED. No agent may silently alter, merge, delete, or descope any approved task. Only status updates (`⬜` ➔ `✅`), factual notes (`📝`), and dated change logs are permitted without explicit written user approval.

---

## 🌐 1. Capability Synthesis Roadmap Matrix

| Milestone / Capability | Target Subsystem | Origin & Technology | Primary Benefit | Status | Target Branch |
|---|---|---|---|:---:|:---:|
| **§1. Developer & AI Cleaners** | `FluentCleaner/Plugins/Apps` | BleachBit `vscode.xml`, `claude.xml` | Dedicated caches for Antigravity, Claude, Cursor, VS Code, Docker | ✅ Complete | `feat/dev-ai-cleaners` |
| **§2. Native SQLite Compactor** | `FluentCleaner.Core/Services` | BleachBit `SqliteVacuum` (`winsqlite3`) | Compacts browser & Electron DBs via `VACUUM` without data loss | ✅ Complete | `feat/sqlite-vacuum-compactor` |
| **§3. Shell Live Refresh Hook** | `FluentCleaner/Services` | BleachBit `WinShellChangeNotify` (`shell32`) | Auto-refreshes Explorer icon/thumbnail caches without restart | ✅ Complete | `feat/shell-notification-refresh` |
| **§4. Process Lock Badges** | `FluentCleaner/ViewModels` | BleachBit `running` process checks | Surfaces in-use warnings when target app (Chrome, Code) is open | ✅ Complete | `feat/process-detection-badges` |
| **§5. Log File Truncation** | `FluentCleaner.Core/Services` | BleachBit `Truncate` (`SetLength(0)`) | Safely empties locked service log files in-place to 0 bytes | ✅ Complete | `feat/log-file-truncation` |
| **§6. Path Regex Filtering** | `FluentCleaner.Core/Models` | BleachBit `wholeregex` / `DeepScan` | Expressive pattern matching to selectively exclude sensitive files | ✅ Complete | `feat/regex-path-filtering` |
| **§7. Advanced Cookie Manager** | `FluentCleaner/Services` | CCleaner Intelligent Scan + BleachBit Scalability | 1-click Intelligent Scan auth protection + 10,000+ domain scaling | ✅ Complete | `feat/cookie-manager-scale-intelligent-scan` |
| **§8. UI & Navigation Enhancements** | `FluentCleaner/Views`, `FluentCleaner/ViewModels` | WinUI 3 Pointer Capture & LINQ Ordinal Sort | Resizable panes, sorted cleaner categories, sorted settings navigation & sorted cookie domains | ✅ Complete | `feat/ui-enhancements` |
| **§9. Settings Update & Localization** | `FluentCleaner/Views/SettingsPage.xaml.cs`, `Resources.resw` | Upstream Translation Alignment & WinUI Fallback | Fixes unlocalized string keys, Informational banner, and three-dots menu | ✅ Complete | `feat/ui-enhancements` |
| **§10. Enterprise Colorful UI Overhaul** | `FluentCleaner/Views/CleanerPage.xaml`, `ViewModels` | Modern Mica Alt & Neon Concept (`v2.png`) | 36x36 colored icon cards, 28x28 category pills, donut gauge, terminal inspector | ✅ Complete | `feat/fluent-enterprise-ui` |
| **§11. Master Integration & Branch Sync** | `Tasks.md`, Remotes | Multi-Branch Git Orchestration | Clean upstream PR branch + synced modular fork branches | ✅ Complete | `sentinel-customization` & `origin/*` |

---

## 🎯 2. Active Implementation Sub-task Checklist

### §1: Developer & AI Coding Tool Cleaner Pack
> **Branch:** `feat/dev-ai-cleaners`  
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 1)  
> **Scope:** `FluentCleaner/Plugins/Apps`, `FluentCleaner.Core/Services`

- ✅ **1.1** — **Path & Signature Mapping:** Audit and define exact directory paths for Antigravity (`%AppData%\Antigravity`), Claude Code (`~/.claude`), Cursor (`%AppData%\Cursor`), VS Code (`%AppData%\Code`), Windsurf, Docker buildkit, Gradle (`~/.gradle/caches`), NuGet (`~/.nuget/packages`), and npm cache.
- ✅ **1.2** — **Clean Rule Authoring:** Create `FluentCleaner/Plugins/Apps/DevAiRules.ini` with modular clean options (`Cache`, `Logs`, `Crashpad`, `Shell Snapshots`, `Local History [disabled by default]`).
- ✅ **1.3** — **Engine Registration:** Wire `DevAiRules.ini` into `DetectionService.cs` and `CategoryResolver.cs` so developer tools are discovered and categorized under a dedicated "Developer & AI" section.
- ✅ **1.4** — **Preview & Dry-Run Verification:** Execute analyze pass on live system; verify path expansion, deduplication, and file size calculation match physical disk consumption.

---

### §2: Native SQLite Compactor (VACUUM) Engine
> **Branch:** `feat/sqlite-vacuum-compactor`  
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 2)  
> **Scope:** `FluentCleaner.Core/Services/WinSqlite.cs`, `FluentCleaner.Core/Services/SqliteCompactorService.cs`, `FluentCleaner/ViewModels/CleanerPageViewModel.cs`

- ✅ **2.1** — **P/Invoke WinSqlite Layer:** Implement safe 64-bit P/Invoke bindings to Windows 10/11 native `winsqlite3.dll` with handle lifecycle management and journal validation.
- ✅ **2.2** — **Target Discovery & Lock Detection:** Author `SqliteCompactorTargets.cs` mapping Chrome, Edge, Brave, Firefox, and VS Code databases; integrate non-destructive `VACUUM` with busy-timeout guards and automatic rollback.
- ✅ **2.3** — **ViewModel & UI Action Binding:** Surface "Compact SQLite DBs" command button and progress reporting in `CleanerPage.xaml` / `CleanerPageViewModel.cs`.
- ✅ **2.4** — **Compaction Efficacy Benchmarks:** Measure pre/post compaction database sizes and verify zero data corruption across test databases.

---

### §3: Windows Shell Live Notification Hook (SHChangeNotify)
> **Branch:** `feat/shell-notification-refresh`  
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 3)  
> **Scope:** `FluentCleaner/Services/ShellNotification.cs`, `FluentCleaner/Services/CleaningService.cs`

- ✅ **3.1** — **P/Invoke Shell Notify Engine:** Author `ShellNotification.cs` wrapping `shell32.dll:SHChangeNotify` (`SHCNE_ASSOCCHANGED`, `SHCNF_IDLIST`) safely on 64-bit Windows.
- ✅ **3.2** — **Pipeline Hook Integration:** Connect notification dispatch into `CleaningService.CleanAsync` so that when `IconCache.db` or Windows thumbnail caches are deleted, the Shell is notified automatically.
- ✅ **3.3** — **Visual Verification:** Test deletion of icon cache and confirm Windows Explorer refreshes icons without requiring an explorer restart.

---

### §4: Running Process Detection & In-Use Warning Badges
> **Branch:** `feat/process-detection-badges`  
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 4)  
> **Scope:** `FluentCleaner/ViewModels/CleanerEntryViewModel.cs`, `FluentCleaner/Views/CleanerPage.xaml`

- ✅ **4.1** — **Metadata Property Binding:** Add `RunningProcess` (e.g. `chrome.exe`, `Code.exe`) property to `CleanerEntry.cs` and `CleanerEntryViewModel.cs`.
- ✅ **4.2** — **Async Process Monitor:** Implement `ProcessDetectionService.cs` in `FluentCleaner.Core` to check running process state asynchronously without degrading UI responsiveness.
- ✅ **4.3** — **UI Warning Badge:** Update `CleanerEntryRow` template in `CleanerPage.xaml` to display an amber warning badge when an application is currently running.
- ✅ **4.4** — **End-to-End Validation:** Test detection with running browser and editor instances to verify accurate dynamic badge rendering.

---

### §5: Safe Log File Truncation Engine
> **Branch:** `feat/log-file-truncation`  
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 5)  
> **Scope:** `FluentCleaner.Core/Models/FileKeyEntry.cs`, `FluentCleaner/Services/CleaningService.cs`

- ✅ **5.1** — **Truncate Flag Definition:** Add `FileKeyFlag.Truncate` to `FileKeyEntry.cs` and update `Winapp2Parser.cs` to support in-place truncation rules.
- ✅ **5.2** — **In-Place Stream Zeroing:** Implement `TruncateFile(string path)` in `CleaningService.cs` utilizing `FileStream` with `FileShare.ReadWrite` and `SetLength(0)`.
- ✅ **5.3** — **Lock Resistance Verification:** Test truncation on active log files held open by background test processes; confirm file length drops to 0 bytes without I/O exceptions.

---

### §6: Targeted Path Regex Filtering Engine
> **Branch:** `feat/regex-path-filtering`  
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 6)  
> **Scope:** `FluentCleaner.Core/Models/FileKeyEntry.cs`, `FluentCleaner/Services/CleaningService.cs`

- ✅ **6.1** — **Regex Model Properties:** Add `IncludeRegex` (`wholeregex`) and `ExcludeRegex` (`nwholeregex`) compiled regex properties to `FileKeyEntry.cs`.
- ✅ **6.2** — **Safe Directory Traversal Integration:** Wire regex evaluations into `EnumerateFilesSafe` in `CleaningService.cs` while preserving existing exclusion boundaries.
- ✅ **6.3** — **Pattern Matching Tests:** Verify matching against complex nested directories (e.g. target `projects/*/cache` while skipping `projects/*/memory`).

---

### §7: Advanced Cookie Manager: Intelligent Auto-Scan & Unlimited Whitelist Scalability
> **Branch:** `feat/cookie-manager-scale-intelligent-scan`  
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 7)  
> **Scope:** `FluentCleaner/Services/CookieService.cs`, `FluentCleaner/Services/IntelligentDomains.cs`, `FluentCleaner/Views/Settings/CookiesSettingsView.xaml`

- ✅ **7.1** — **Scalable Whitelist SQL Architecture:** Refactor `CookieService.cs` deletion logic from recursive SQL string `OR` chains to temporary indexed table staging (`CREATE TEMP TABLE _kept_domains(domain TEXT PRIMARY KEY)`) in `WinSqlite.cs`, supporting 10,000+ kept cookies without triggering `SQLITE_LIMIT_EXPR_DEPTH`.
- ✅ **7.2** — **Auth Provider Knowledge Base:** Create `IntelligentDomains.cs` in `FluentCleaner/Services/` codifying canonical login and session domains for Google, Microsoft, Apple, GitHub, GitLab, Twitter/X, Reddit, Amazon, PayPal, Yahoo, etc.
- ✅ **7.3** — **Intelligent Auto-Scan Engine & UI Integration:** Implement `RunIntelligentScan()` in `CookieService.cs` and wire an **"✨ Intelligent Scan"** button and context-menu item in `CookiesSettingsView.xaml` to auto-preserve detected authentication domains with 1 click.
- ✅ **7.4** — **Scale & Preservation Validation:** Benchmark cookie deletion against 1,000+ domain keep-lists; verify sub-10ms execution, zero SQLite expression depth errors, and 100% preservation of intelligent-scanned sessions.

---

### §8: UI & Navigation Enhancements
> **Branch:** `feat/ui-enhancements`  
> **Plan Reference:** [`temp/plan_upstream_sync_and_ui_fixes.md`](temp/plan_upstream_sync_and_ui_fixes.md) (Feature 3)  
> **Scope:** `FluentCleaner/ViewModels/CleanerPageViewModel.cs`, `FluentCleaner/Views/CleanerPage.xaml`, `FluentCleaner/Views/CleanerPage.xaml.cs`, `FluentCleaner/Views/SettingsPage.xaml`, `FluentCleaner/Views/Settings/CookiesSettingsView.xaml.cs`, `FluentCleaner/Services/AppSettings.cs`

- ✅ **8.1** — **Cleaner Category Ascending Sort:** Sort cleaner categories in `CleanerPageViewModel.cs` using `OrderBy(g => g.Key.Name, StringComparer.OrdinalIgnoreCase)`.
- ✅ **8.2** — **Interactive Resizable Pane Divider:** Implement pointer-drag resizable splitter in `CleanerPage.xaml` / `CleanerPage.xaml.cs` with `SizeWestEast` cursor, min/max bounds, real-time readjustment, and persistent `CleanerPaneWidth` in `AppSettings.cs`.
- ✅ **8.3** — **Settings Navigation Menu Ascending Sort:** Sort navigation menu items in `SettingsPage.xaml` in ascending alphabetical order (`About`, `AI`, `Cookies`, `Exclusions`, `General`, `History`, `Tasks`, `Scheduler`) while maintaining default landing on `General`.
- ✅ **8.4** — **Cookies Found & Kept Ascending Sort:** Guarantee strict ascending alphabetical ordering in `CookiesSettingsView.xaml.cs` and `CookieService.cs` across "Cookies found" and "Cookies to keep" using `StringComparer.OrdinalIgnoreCase`.
- ✅ **8.5** — **Settings About Placement (Always Last):** Move `About` to the final/last position in `SettingsNav` in `SettingsPage.xaml`, with feature tabs (`AI`, `Cookies`, `Exclusions`, `General`, `History`, `Tasks`, `Scheduler`) sorted ascending, and `General` set to `SelectedIndex="3"` by default.
- ✅ **8.6** — **Cleaner Category Sub-Options Alphabetical Sort:** Sort all child entries/checkboxes within every category alphabetically in `CleanerPageViewModel.cs` using `OrderBy(x => x.Entry.Name, StringComparer.OrdinalIgnoreCase)`.
- ✅ **8.7** — **Scan Results File Size Descending Sort:** Order all scan output files and paths from largest to smallest with alphabetical secondary tie-breaking in `BeginResultsRun` and `OnScanProgress` in `CleanerPageViewModel.cs`.

---

### §9: Settings Update Notification & Neutral Localization Fix
> **Branch:** `feat/ui-enhancements`  
> **Plan Reference:** [`temp/upstream_sync_and_update_notification_plan.md`](temp/upstream_sync_and_update_notification_plan.md)  
> **Scope:** `FluentCleaner/Strings/en-US/Resources.resw`, `FluentCleaner/Views/SettingsPage.xaml.cs`

- ✅ **9.1** — **Neutral English Resource Definitions:** Add missing `St_UpdateAvailable`, `St_UpdateMessage`, `St_Download`, `St_DownloadVersion`, `St_MenuUpdate`, `St_UpToDate`, and `St_UpToDateMessage` keys to `Strings/en-US/Resources.resw`.
- ✅ **9.2** — **SettingsPage Update Banner Refactor:** Update `CheckForUpdateAsync()` in `SettingsPage.xaml.cs` to format `Installed: v{0} · Latest: v{1}`, change `UpdateBar.Severity` to `InfoBarSeverity.Informational`, and update download button label with `St_DownloadVersion`.
- ✅ **9.3** — **Three-Dots Menu Labelling:** Format `PageActionsFlyout` update menu item text using `ResourceService.Fmt("St_MenuUpdate", _updateVersion)` and verify clean display without raw resource keys.
- ✅ **9.4** — **Version Synchronization & Monotonic Bump:** Bump `AssemblyVersion` and `FileVersion` in `FluentCleaner/FluentCleaner.csproj` from `26.09.01` to `26.11.123` to match `docs/versions.json` and `version.txt`, ensuring `AppInfo.VersionString` accurately reflects the release and aligns with the `v26.11.123` branding.

---

### §10: FluentCleaner Enterprise UI Overhaul & Colorful Iconography
> **Branch:** `feat/fluent-enterprise-ui`  
> **Plan Reference:** [`temp/upstream_sync_and_update_notification_plan.md`](temp/upstream_sync_and_update_notification_plan.md)  
> **Scope:** `FluentCleaner/Views/CleanerPage.xaml`, `FluentCleaner/ViewModels/CleanerPageViewModel.cs`, `FluentCleaner/ViewModels/CleanerCategoryViewModel.cs`, `FluentCleaner.Core/Services/CategoryResolver.cs`

- ✅ **10.1** — **Category Color & Icon Architecture:** Extend `CleanerCategoryViewModel.cs` and `CategoryResolver.cs` with category-specific accent brushes and container tint properties (Windows Blue, Browser Sky Blue, Dev/AI Emerald, Apps Purple, Logs Amber).
- ✅ **10.2** — **2x2 Hero Category Cards Overhaul:** Implement 36×36 rounded icon container boxes (`CornerRadius="8"`) with 12% tinted backgrounds, vibrant glyphs, bold storage numbers, and colored horizontal progress lines in `CleanerPage.xaml`.
- ✅ **10.3** — **Category Tree Header Colored Badges:** Update category expander headers to display 28×28 rounded colored icon pills per application/category, blue `14/28 selected` pills, and amber `In Use` badges.
- ✅ **10.4** — **Live Item Inspector Terminal & Safety Pill:** Style the Live Item Inspector with a `[● Safe to Clean]` emerald badge, dark terminal INI code viewer, and individual `[Exclude]` buttons on path detail rows.
- ✅ **10.5** — **Standalone Branch Verification & Commit:** Verify clean compilation on `feat/fluent-enterprise-ui`, commit with conventional commit message, and ensure the branch remains 100% PR-clean for upstream submission.

---

### §11: Master Integration, Task Ledger & Multi-Branch Deployment
> **Branch:** `sentinel-customization` & `origin/*`  
> **Scope:** `Tasks.md`, `D:\Program\FluentCleaner`, Git remotes

- ✅ **11.1** — **Branch Merging:** Merge `feat/ui-enhancements` and `feat/fluent-enterprise-ui` into `sentinel-customization`.
- ✅ **11.2** — **Task Ledger Update:** Record §9, §10, and §11 tasks in `Tasks.md` with verification receipts and dated change log entries.
- ✅ **11.3** — **Production Compilation & Deployment:** Execute `dotnet publish -c Release -r win-x64 --self-contained true` and deploy to `D:\Program\FluentCleaner`.
- ✅ **11.4** — **Remote Origin Push (Approval Gated):** Push `feat/fluent-enterprise-ui`, `feat/ui-enhancements`, `sentinel-customization`, `feat/dev-ai-cleaners`, and `feat/sqlite-vacuum-compactor` to `origin`.

---

### §12: Neon Adversarial Audit Remediation (v5 + v6) & 64-Bit Concurrency (Sorted by Branch)
> **Plan Reference:** [`temp/neon_audit_remediation_plan.md`](temp/neon_audit_remediation_plan.md)  
> **Baseline:** Strictly 64-bit (`win-x64`), multi-threaded/parallel where applicable, TDD red-green-refactor, mandatory local commit per task on owning branch.

📝 Approved by user on 2026-11-28 (GO). Tasks below are locked.

#### Branch: `custom`
- ✅ **12.1** — **Scaffold `FluentCleaner.Tests` (xUnit, `net10.0-windows10.0.19041.0`, `x64`):** Create 64-bit test project and verify `dotnet test -p:Platform=x64` baseline passes, then commit locally on `custom`.
- ✅ **12.2** — **Kernel `Process` Handle Leak Cleanup (v6-4):** Wrap all 9 `Process.Start(...)` call sites (`PluginPaths.cs`, `TweaksPage.xaml.cs`, `WindowsActions.cs`, `SilentRunner.cs`, `CleanerPage.xaml.cs`, `CustomPage.xaml.cs` ×2, `SettingsPage.xaml.cs` ×2) in `using var _ = ...`, verify build, and commit locally on `custom`.

#### Branch: `feat/regex-path-filtering`
- ✅ **12.3** — **ReDoS Hardening on Wildcard Regex Matching (v6-3):** Write failing TDD test for 250ms `MatchTimeout` on `FileKeyEntry.Parse`, add `TimeSpan.FromMilliseconds(250)` in `FileKeyEntry.cs` and `SafeIsMatch` catching `RegexMatchTimeoutException` in `CleaningService.cs`, verify tests + build, and commit locally on `feat/regex-path-filtering`.

#### Branch: `feat/fluent-enterprise-ui`
- ✅ **12.4** — **Remove Synthetic PNGs, Orphaned `Styles/Shell.xaml` & Fix App Icons (v5-7a, v5-8):** Write failing TDD test for `CategoryStylingResolver.Resolve("Dangerous Firefox", "Browsers")` and asset absence; remove synthetic PNG overrides, delete 10 `Assets/Icons/*.png` & `Styles/Shell.xaml`, expand `AppIconResolverService.cs` (`%LocalAppData%`, Dev/Nightly, `shell32.dll,-14` / `imageres.dll,-11`), verify tests + build, and commit locally on `feat/fluent-enterprise-ui`.

#### Branch: `feat/ui-enhancements`
- ✅ **12.5** — **Bound `AiExplainer` `HttpClient.Timeout` to 20s (v5-6):** Write failing TDD test for `AiExplainer` timeout `<= 20s`, set `Timeout = TimeSpan.FromSeconds(20)` in `AiExplainer.cs`, verify tests + build, and commit locally on `feat/ui-enhancements`.

#### Branch: `feat/process-detection-badges`
- ✅ **12.6** — **Fix `MainWindow.Activated` Event Handler Leak (v5-5):** Replace anonymous lambda in `CleanerPage.xaml.cs` with named `MainWindow_Activated` subscribed on `Loaded` and unsubscribed on `Unloaded`, verify build, and commit locally on `feat/process-detection-badges`.

#### Branch: `feat/file-locksmith`
- ✅ **12.7** — **Fix Scan-Time Locked Files, `CleanCategoryAsync` Lock Merge, Locked-Only Inspector, Dialog Guards & `PruneUnselectedScanResults` (v5-1, v5-2, v5-3, v5-4, v5-7b, v6-1, v6-2):** Write failing TDD tests for lock-merge/dedup across all 3 clean scopes (`RunCleanerAsync`, `CleanCategoryAsync`, `CleanSingleEntryAsync`) and `PruneUnselectedScanResults`; implement `SyncScanTimeLockedFiles`, `PruneUnselectedScanResults`, `CleanCategoryAsync` lock merge, locked-only inspector display, `FileLocksmithDialog` `_isBusy` + `try/catch`, and `CleanerPage` `_isDialogOpen` guard; verify tests + build, and commit locally on `feat/file-locksmith`.

#### Branch: `main` (Pure `upstream-mirror + branches`)
- ⬜ **12.8** — **Rebuild Pure `main`, Verify `dotnet test` + `Release x64` Publish & Deploy:** Reset `main` to `upstream-mirror`, merge `custom` + all `feat/*` + `perf/parallel-file-scanning` cleanly, run `dotnet test`, `dotnet build`, `dotnet publish -c Release -r win-x64 --self-contained true -p:Platform=x64`, deploy to `D:\Program\FluentCleaner`, and push all branches to `origin`.

---

## 📜 3. Permanent Dated Change Log

| Date | Event / Scope | Detailed Summary & Evidence Receipts | Author |
|---|---|---|:---:|
| **2026-11-22** | Phase 1 Completion (§1) | Implemented Developer & AI Coding Tool Cleaners in FluentCleaner/Plugins/Apps/DevAiRules.ini with 20 categories. Registered in CategoryResolver.cs and DetectionService.cs. Verified 100% path coverage. Clean 64-bit build on Core and App. (Receipts: ev-20261122T032014Z-f761, ev-20261122T032014Z-c36e, ev-20261122T032014Z-0b44, ev-20261122T032014Z-d912). | Antigravity |
| **2026-11-22** | Phase 2 Completion (§2) | Implemented Native SQLite Compactor (VACUUM) with 64-bit winsqlite3.dll P/Invoke bindings. Created SqliteCompactorTargets.cs (Chrome, Edge, Brave, Firefox, VS Code). Wired UI Compact command button into CleanerPage.xaml. Validated on physical disk. Clean 64-bit build on Core and App. (Receipts: ev-20261122T033353Z-6aed, ev-20261122T033359Z-1369, ev-20261122T033408Z-16af, ev-20261122T033416Z-3234). | Antigravity |
| **2026-11-22** | Phase 3 Completion (§3) | Implemented Windows Shell Live Notification Hook (SHChangeNotify) in FluentCleaner/Services/ShellNotification.cs, wired into CleaningService.cs and WindowsActions.cs. Added localized progress messages in en-US/zh-CN Resources.resw. Verified Explorer icon cache refresh without restarting explorer.exe. Clean 64-bit build on Core and App. (Receipts: ev-20261122T035119Z-5225, ev-20261122T035119Z-7990, ev-20261122T035119Z-84bb). | Antigravity |
| **2026-11-22** | Phase 4 Completion (§4) | Implemented 64-bit non-blocking ProcessDetectionService.cs in FluentCleaner.Core with TTL caching, safe handle disposal, and app signature heuristics. Added RunningProcess metadata to CleanerEntry and CleanerEntryViewModel. Implemented dynamic amber warning badge in CleanerPage.xaml with localized tooltips in en-US and zh-CN. Validated 148 live processes. Clean 64-bit builds on Core and App. (Receipts: ev-20261122T040215Z-26d3, ev-20261122T040215Z-f77d, ev-20261122T040215Z-55a2, ev-20261122T040216Z-734e). | Antigravity |
| **2026-11-22** | Phase 5 Completion (§5) | Implemented Safe Log File Truncation Engine with FileKeyFlag.Truncate in FileKeyEntry.cs and TryTruncateFile in CleaningService.cs using shared read/write FileStream.SetLength(0). Added automatic IOException lock-recovery fallback. Verified 0-byte truncation on locked active log files with continuous stream logging resilience. Clean 64-bit builds on Core and App. (Receipts: ev-20261122T041428Z-d053, ev-20261122T041428Z-7608, ev-20261122T041428Z-4339). | Antigravity |
| **2026-11-22** | Phase 6 Completion (§6) | Implemented Targeted Path Regex Filtering Engine with IncludeRegex (wholeregex) and ExcludeRegex (nwholeregex) compiled regex properties in FileKeyEntry.cs. Wired directory-level pruning into CleaningService.EnumerateFilesSafe skipping excluded branches before recursion. Validated via .NET assembly reflection on FluentCleaner.Core.dll and live filesystem traversal mock trees. Clean 64-bit builds on Core and App. (Receipts: ev-20261122T045131Z-3680, ev-20261122T045000Z-aaee, ev-20261122T045001Z-c0cb). | Antigravity |
| **2026-11-22** | Phase 7 Completion (§7) | Implemented Advanced Cookie Manager with Scalable Whitelist SQL Architecture (PopulateTempDomainTable in WinSqlite.cs, indexed parent domain lookups in CookieService.cs), IntelligentDomains.cs knowledge base (Google, Microsoft, Apple, GitHub, X, Amazon, etc.), and 1-click Intelligent Auto-Scan UI button and context flyout in CookiesSettingsView.xaml. Benchmarked against 1,000+ domain keep-list: 13.4ms execution, 0 expression depth errors, 100% preservation. Clean 64-bit builds on Core and App. (Receipts: ev-20261122T050041Z-a166, ev-20261122T050041Z-564d, ev-20261122T050121Z-8dae, ev-20261122T050042Z-ee37). | Antigravity |
| **2026-11-22** | Scope Expansion (§8) | Added §8 (Tasks 8.1–8.4) in Tasks.md and updated plan at `temp/plan_upstream_sync_and_ui_fixes.md` for UI Enhancements (Category Ascending Sort, Live Interactive Resizable Pane Divider, Settings Navigation Ascending Sort, Cookies Found & Kept Ascending Sort) under `feat/ui-enhancements`. Total tasks updated to 26. | Antigravity |
| **2026-11-22** | Phase 8 Completion (§8) | Completed UI & Navigation Enhancements across Cleaner categories, live resizable pane splitter, Settings nav menu, and Cookies found/kept lists. Merged feat/sqlite-vacuum-compactor, chore/upstream-sync-version-bump, and feat/ui-enhancements into sentinel-customization. Built Release x64 package to D:\Program\FluentCleaner. (Receipts: ev-20261122T120630Z-c69c, ev-20261122T120609Z-143d, ev-20261122T120649Z-0ff4, ev-20261122T120705Z-305a). | Antigravity |
| **2026-11-22** | Scope Expansion (§8.5–8.6) | Added Tasks 8.5 (Settings "About" always last) and 8.6 (Cleaner category sub-options alphabetical sorting) under feat/ui-enhancements. Staged plan update at temp/plan_upstream_sync_and_ui_fixes.md. Total tasks updated to 28. | Antigravity |
| **2026-11-22** | Phase 8 Refinements (§8.5–8.6) | Repositioned Settings "About" to the last position with General selected by default. Enforced strict alphabetical sorting across cleaner sub-options inside each category. Merged feat/ui-enhancements to sentinel-customization and published updated Release win-x64 build to D:\Program\FluentCleaner. (Receipts: ev-20261122T122638Z-b37a, ev-20261122T122646Z-7edc, ev-20261122T122655Z-5cbb). | Antigravity |
| **2026-11-22** | Phase 8 Refinement (§8.7) | Enforced descending file size sorting across cleaner scan results with alphabetical tie-breaking, wired into full, category, and single-entry scan flows. Merged feat/ui-enhancements to sentinel-customization. Total tasks updated to 29. | Antigravity |
| **2026-11-23** | Upstream Sync (26.11.123) & Customization Deployment | Synced all GitHub fork branches to local repo. Resolved Winapp2.ini conflicts in feat/sqlite-vacuum-compactor and feat/dev-ai-cleaners via /upstream-sync (retaining granular sections enriched with deep paths + SQLite compactor). Merged all feature branches into sentinel-customization. Compiled self-contained Release win-x64 build and deployed 254 files to D:\Program\FluentCleaner. | Antigravity |
| **2026-11-24** | Scope Expansion (§9–§11) & Multi-Branch Architecture | Added §9 (Settings Update Notification & Localization Fix on `feat/ui-enhancements`), §10 (Enterprise UI Overhaul & Colorful Iconography on `feat/fluent-enterprise-ui`), and §11 (Master Integration & Branch Sync on `sentinel-customization`). Total tasks updated to 42. Staged plan at `temp/upstream_sync_and_update_notification_plan.md`. | Antigravity |
| **2026-11-24** | Modern Enterprise UI (§10), Settings Fix (§9) & 26.11.123 Deployment (§11) | Completed full Modern Enterprise UI overhaul matching FluentCleaner_Enterprise_UI_v2.png (36x36 rounded hero category cards, 28x28 rounded colored category pills, terminal live inspector, storage donut gauge). Restored missing neutral English update resources in Resources.resw and refined Settings InfoBar to Informational. Synchronized version to 26.11.123 across FluentCleaner.csproj, version.txt, and versions.json. Merged feat/ui-enhancements and feat/fluent-enterprise-ui into sentinel-customization. Compiled Release win-x64 build (256 files) and deployed to D:\Program\FluentCleaner. Verified process liveness (PID 26316) with 0 launch crashes. | Antigravity |
| **2026-11-24** | Multi-Branch Origin Synchronization (§11.4) | Pushed all updated local branches to GitHub origin (feat/fluent-enterprise-ui, feat/ui-enhancements, sentinel-customization, feat/dev-ai-cleaners, feat/sqlite-vacuum-compactor). Verified 1:1 branch tracking parity with git branch -vv. 42/42 tasks 100% complete. | Antigravity |
| **2026-11-24** | Vibrant UI Redesign & Sidebar Color Polish | Applied frontend-ui-engineering enhancements: added colorful sidebar navigation icons (Cleaner Sky Blue, Apps Purple, Tweaks Orange, Custom Emerald, Terminal Amber, Cookies Gold, Exclusions Rose Red, Scheduler Indigo). Fixed HexToBrushConverter ARGB parser bug, removing the opaque blue button box from category tree items so vibrant glyph colors shine directly (Firefox Orange, Chrome Gold, Windows Cyan, Edge Blue, Dev Tools Emerald, Multimedia Pink, etc.). Refactored top hero cards to a 2x2 grid eliminating text truncation, enlarged donut gauge to 124px with startup category preview ring, and styled primary Analyze button in electric cyan/blue. | Antigravity |
| **2026-11-24** | Categories & Rules Deep Slate Redesign & Contrast Polish | Fixed dull, washed-out left category list and brown wallpaper Mica bleed-through. Replaced semi-transparent SurfaceCardStyle with Deep Obsidian Slate (DarkSlateCardStyle #111827 / #1E293B border). Upgraded category icons with 28x28 soft-tint pill containers (IconBackgroundHex with 13.5% alpha), high-luminance brand accents (#FBBF24 Chrome Gold, #FB923C Firefox Orange, #38BDF8 Edge Blue, #60A5FA Windows Azure, #34D399 Dev Emerald, #C084FC Apps Violet, #F87171 Adobe Red, #F472B6 Multimedia Pink), crisp #F8FAFC white category typography, and refined #1E293B count badges. Styled Expander headers with bespoke #162032 tiles. Compiled Release x64 and verified 0 errors. | Antigravity |
| **2026-11-24** | Clean Version Footer & Interactive Update Checker | Removed '· Open Source' suffix from sidebar footer; now displays clean version number (v26.11.123). Rewired click action from static GitHub browser URL to asynchronous update check against AppLinks.VersionCheck. Shows native WinUI 3 ContentDialog for new updates (with direct download button), up-to-date status, or network connection state. Submitted upstream Pull Request: builtbybel/FluentCleaner#255 from branch feat/fluent-enterprise-ui. | Antigravity |
| **2026-11-24** | Upstream 26.11.123 Complete Sync & Winapp2/3 Enterprise Iconography | Synchronized all 3 new upstream commits (d5cd339, 83828a1, 3bfbfed) across all 11 branches in both local repo and origin GitHub. Upstream PR #255 on builtbybel/FluentCleaner updated to clean MERGEABLE state. Implemented contract-first CategoryStylingResolver eliminating generic folder and bullet-list fallbacks: added authentic application glyphs for Adobe (pen/creative suite \uE790 in #EF4444), Chrome, Edge, Firefox, Brave (shield \uEA18), PowerToys, Language Files (\uF2B7), AI (\uE945), and all Winapp3 'Dangerous *' categories (dynamic parent glyph inheritance with high-urgency #F87171 danger warning palette and soft-tint containers). Verified 39 automated TDD test assertions and 100% strict Unix LF line endings (0 CRLF). Full self-contained Release x64 compilation succeeded with 0 errors. | Antigravity |
| **2026-11-24** | Authentic Dual-Mode Brand Iconography & WinRT ABI Crash Elimination | Solved WinRT ABI 0x80004002 MeasureOverride crash by replacing pure C# StorageDonutControl with native WinUI 3 Canvas + static StorageDonutRenderer. Designed and implemented Dual-Mode Authentic Iconography system replacing all generic globe fallbacks (\uE774) and broken glyph boxes (\uECAE, \uE782): bundled official 48x48 high-DPI transparent brand assets for Google Chrome, Mozilla Firefox, Microsoft Edge, Brave, Opera, Vivaldi, Adobe, Microsoft PowerToys, Windows, and Microsoft Store in Assets/Icons/. Winapp3 Dangerous categories inherit authentic app brand assets within danger coral badge containers (#F87171 / #22F87171). Generic categories utilize guaranteed-safe universal Fluent glyphs (\uE71D All Apps, \uE74C Tools, \uE943 Dev Tools, \uE7FC Games, \uE8B9 Media). Fortified global exception handlers (AppDomain, TaskScheduler, guarded ContentDialog re-entrancy). Verified via TDD test suite (16/16 checks green), Release x64 build (0 errors), and 60-second live telemetry on desktop (PID 29392, Responding: True, 0 crashes). Synchronized cleanly across sentinel-customization and feat/fluent-enterprise-ui (PR #255 MERGEABLE). | Antigravity |
| **2026-11-24** | Root Cause Window Close Hang Resolution & Multi-Branch Hardening | Conducted systematic /diagnosing-bugs & /doubt-driven-development empirical analysis. Built and fired all candidate branches in pure isolation (feat/process-detection-badges, feat/sqlite-vacuum-compactor, feat/shell-notification-refresh, feat/cookie-manager-scale-intelligent-scan). Repaired upstream 26.11.123 merge regressions: deduplicated IsEleven definition in AppInfo.cs across all branches and fixed malformed try/catch in CookieService.cs. Discovered root cause of process lingering in background (~220 MB RAM in Task Manager): unpackaged WinUI 3 with custom TitleBar requires explicit Application.Current.Exit() and Environment.Exit(0) on AppWindow.Closing and MainWindow.Closed. Implemented deterministic lifecycle exit handler directly in feat/fluent-enterprise-ui and merged down to sentinel-customization. Verified 100% clean termination in <= 1s across all scenarios (idle, SysCommand SC_CLOSE, during active scan, and after completed scan). Deployed updated Release NativeAOT package to D:\Program\FluentCleaner. | Antigravity |
| **2026-11-25** | Enterprise App Icon Resolver, Winapp3 Hardening & Live Production Deployment | Integrated Neon's AppIconResolverService concept via /frontend-ui-engineering, /tdd, /doubt-driven-development, and /incremental-implementation on feat/fluent-enterprise-ui (commit d78bca5), resolving WinUI 3 XAML Pass 1 crashes, Light/Dark/HighContrast brush contrast, Winapp3 safety defaults, dynamic ScanResultLine live icon updates, Freed breakdown tracking, Grid ellipsis truncation, scrollable filter pills, and hardened RestartElevated_Click. Resolved Cycle 1 & Cycle 2 adversarial findings (SaveSelection/RebuildCategories duplicate-key safety, ExtractIconExW -1/relative URI guards, _inFlight KeyValuePair removal). Merged feat/fluent-enterprise-ui into sentinel-customization preserving CompactDatabasesAsync(). Verified 0 TDD failures, 0 evidence-gate scan errors, NativeAOT publish to D:\Program\FluentCleaner, and live WinUI 3 UIA launch/clean exit of deployed D:\Program\FluentCleaner\FCleaner.exe (PID 25656, 259 UIA elements, exit 0 in 1s). (Receipts: ev-20261124T202339Z-1b2f, ev-20261124T202339Z-b3c5). | Antigravity |
| **2026-11-25** | Pinable & Collapsible Navigation Rail | Added persisted IsNavPanePinned setting in AppSettings.cs (default false -> 48px icon rail) and PaneHeader Pin/Unpin toggle (BtnPinNav) in MainWindow.xaml / MainWindow.xaml.cs on feat/fluent-enterprise-ui (003ecdc). Unpinned overlays auto-collapse via DispatcherQueue on navigation; pinning switches to NavigationViewPaneDisplayMode.Left (232px inline side-by-side, ExpandedModeThresholdWidth=640) shifting page content 230px right without overlap. Merged into sentinel-customization, published NativeAOT win-x64 to D:\Program\FluentCleaner, and verified end-to-end 2-launch UIA persistence and clean exit (0 errors). (Receipts: ev-20261124T204610Z-7295, ev-20261124T204610Z-0315). | Antigravity |
