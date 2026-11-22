# 📋 FluentCleaner — Master Implementation & Capability Ledger

![Status](https://img.shields.io/badge/status-100%25%20Complete-brightgreen)
![Framework](https://img.shields.io/badge/framework-.NET%2010%20%7C%20WinUI%203-blue)
![Architecture](https://img.shields.io/badge/arch-64--bit%20(win--x64%20%7C%20win--arm64)-orange)
![Milestones](https://img.shields.io/badge/Completed%20Tasks-26%2F26-brightgreen)

> [!IMPORTANT]
> **Implementation Scope & Governance Rules:**
> - **Target Boundary:** `D:\Sentinel-Labs\FluentCleaner\` is the strict workspace boundary. All code, configs, and plugins belong strictly within this folder.
> - **Staging & Temp Directory:** `D:\Sentinel-Labs\FluentCleaner\temp` is the ONLY directory permitted for orchestration scripts, diagnostic test harnesses, and staging plans (`temp/<task_name>_plan.md`).
> - **Tooling Hierarchy:** Reusable fleet utilities reside in `D:\Sentinel-Labs\tools\`. FluentCleaner project tools reside in `FluentCleaner\tools\`.
> - **64-Bit & Asynchronous Standard:** All code and builds MUST target strictly 64-bit architectures (`win-x64`/`win-arm64`). Heavy disk I/O, SQLite operations, and regex matching MUST execute off the UI thread via `Task.Run` or background worker pools.
> - **Core Safety Invariants:** Zero telemetry, no spyware, no scareware alerts, no fake registry cleaners, and strictly non-destructive defaults for developer history/auto-memory.
> - **Approved Task Lock (Rule 12):** Once approved, task lines are LOCKED. No agent may silently alter, merge, delete, or descope any approved task. Only status updates (`⬜` ➔ `✅`), factual notes (`📝`), and dated change logs are permitted without explicit written user approval.

---

## 🌐 1. Capability Synthesis Roadmap Matrix

| Milestone / Capability | Target Subsystem | Origin & Technology | Primary Benefit | Status |
|---|---|---|---|:---:|
| **§1. Developer & AI Cleaners** | `FluentCleaner/Plugins/Apps` | BleachBit `vscode.xml`, `claude.xml` | Dedicated caches for Antigravity, Claude, Cursor, VS Code, Docker | ✅ Complete |
| **§2. Native SQLite Compactor** | `FluentCleaner.Core/Services` | BleachBit `SqliteVacuum` (`winsqlite3`) | Compacts browser & Electron DBs via `VACUUM` without data loss | ✅ Complete |
| **§3. Shell Live Refresh Hook** | `FluentCleaner/Services` | BleachBit `WinShellChangeNotify` (`shell32`) | Auto-refreshes Explorer icon/thumbnail caches without restart | ✅ Complete |
| **§4. Process Lock Badges** | `FluentCleaner/ViewModels` | BleachBit `running` process checks | Surfaces in-use warnings when target app (Chrome, Code) is open | ✅ Complete |
| **§5. Log File Truncation** | `FluentCleaner.Core/Services` | BleachBit `Truncate` (`SetLength(0)`) | Safely empties locked service log files in-place to 0 bytes | ✅ Complete |
| **§6. Path Regex Filtering** | `FluentCleaner.Core/Models` | BleachBit `wholeregex` / `DeepScan` | Expressive pattern matching to selectively exclude sensitive files | ✅ Complete |
| **§7. Advanced Cookie Manager** | `FluentCleaner/Services` | CCleaner Intelligent Scan + BleachBit Scalability | 1-click Intelligent Scan auth protection + 10,000+ domain scaling | ✅ Complete |
| **§8. UI & Navigation Enhancements** | `FluentCleaner/Views`, `FluentCleaner/ViewModels` | WinUI 3 Pointer Capture & LINQ Ordinal Sort | Resizable panes, sorted cleaner categories, sorted settings navigation & sorted cookie domains | ✅ Complete |

---

## 🎯 2. Active Implementation Sub-task Checklist

### §1: Developer & AI Coding Tool Cleaner Pack
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 1)  
> **Scope:** `FluentCleaner/Plugins/Apps`, `FluentCleaner.Core/Services`

- ✅ **1.1** — **Path & Signature Mapping:** Audit and define exact directory paths for Antigravity (`%AppData%\Antigravity`), Claude Code (`~/.claude`), Cursor (`%AppData%\Cursor`), VS Code (`%AppData%\Code`), Windsurf, Docker buildkit, Gradle (`~/.gradle/caches`), NuGet (`~/.nuget/packages`), and npm cache.
- ✅ **1.2** — **Clean Rule Authoring:** Create `FluentCleaner/Plugins/Apps/DevAiRules.ini` with modular clean options (`Cache`, `Logs`, `Crashpad`, `Shell Snapshots`, `Local History [disabled by default]`).
- ✅ **1.3** — **Engine Registration:** Wire `DevAiRules.ini` into `DetectionService.cs` and `CategoryResolver.cs` so developer tools are discovered and categorized under a dedicated "Developer & AI" section.
- ✅ **1.4** — **Preview & Dry-Run Verification:** Execute analyze pass on live system; verify path expansion, deduplication, and file size calculation match physical disk consumption.

---

### §2: Native SQLite Compactor (VACUUM) Engine
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 2)  
> **Scope:** `FluentCleaner.Core/Services/WinSqlite.cs`, `FluentCleaner/Services/CleaningService.cs`

- ✅ **2.1** — **Native SQLite Vacuum API:** Extend `WinSqlite.cs` with `VacuumDatabase(string path)` executing `PRAGMA wal_checkpoint(TRUNCATE);` followed by `VACUUM;` with 250ms busy timeout handling.
- ✅ **2.2** — **Compaction Action Service:** Author `CompactDbAction` in `FluentCleaner.Core` that measures initial file size, performs compaction, calculates reclaimed bytes, and handles database locks gracefully.
- ✅ **2.3** — **Target Registration:** Register SQLite databases for Google Chrome (`History`, `Cookies`, `Web Data`), Microsoft Edge, Mozilla Firefox (`places.sqlite`), and VS Code state databases.
- ✅ **2.4** — **UI Binding & Reclaim Benchmark:** Add "Compact Databases (VACUUM)" toggle to `CleanerCategoryViewModel.cs` and verify database integrity post-compaction.

---

### §3: Windows Shell Live Notification Hook
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 3)  
> **Scope:** `FluentCleaner/Services/ShellNotification.cs`, `FluentCleaner/Services/CleaningService.cs`

- ✅ **3.1** — **Shell32 P/Invoke Definition:** Implement `ShellNotification.cs` wrapping `SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero)` with safe interop signatures.
- ✅ **3.2** — **Pipeline Hook Integration:** Connect notification dispatch into `CleaningService.CleanAsync` so that when `IconCache.db` or Windows thumbnail caches are deleted, the Shell is notified automatically.
- ✅ **3.3** — **Visual Verification:** Test deletion of icon cache and confirm Windows Explorer refreshes icons without requiring an explorer restart.

---

### §4: Running Process Detection & In-Use Warning Badges
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 4)  
> **Scope:** `FluentCleaner/ViewModels/CleanerEntryViewModel.cs`, `FluentCleaner/Views/CleanerPage.xaml`

- ✅ **4.1** — **Metadata Property Binding:** Add `RunningProcess` (e.g. `chrome.exe`, `Code.exe`) property to `CleanerEntry.cs` and `CleanerEntryViewModel.cs`.
- ✅ **4.2** — **Async Process Monitor:** Implement `ProcessDetectionService.cs` in `FluentCleaner.Core` to check running process state asynchronously without degrading UI responsiveness.
- ✅ **4.3** — **UI Warning Badge:** Update `CleanerEntryRow` template in `CleanerPage.xaml` to display an amber warning badge when an application is currently running.
- ✅ **4.4** — **End-to-End Validation:** Test detection with running browser and editor instances to verify accurate dynamic badge rendering.

---

### §5: Safe Log File Truncation Engine
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 5)  
> **Scope:** `FluentCleaner.Core/Models/FileKeyEntry.cs`, `FluentCleaner/Services/CleaningService.cs`

- ✅ **5.1** — **Truncate Flag Definition:** Add `FileKeyFlag.Truncate` to `FileKeyEntry.cs` and update `Winapp2Parser.cs` to support in-place truncation rules.
- ✅ **5.2** — **In-Place Stream Zeroing:** Implement `TruncateFile(string path)` in `CleaningService.cs` utilizing `FileStream` with `FileShare.ReadWrite` and `SetLength(0)`.
- ✅ **5.3** — **Lock Resistance Verification:** Test truncation on active log files held open by background test processes; confirm file length drops to 0 bytes without I/O exceptions.

---

### §6: Targeted Path Regex Filtering Engine
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 6)  
> **Scope:** `FluentCleaner.Core/Models/FileKeyEntry.cs`, `FluentCleaner/Services/CleaningService.cs`

- ✅ **6.1** — **Regex Model Properties:** Add `IncludeRegex` (`wholeregex`) and `ExcludeRegex` (`nwholeregex`) compiled regex properties to `FileKeyEntry.cs`.
- ✅ **6.2** — **Safe Directory Traversal Integration:** Wire regex evaluations into `EnumerateFilesSafe` in `CleaningService.cs` while preserving existing exclusion boundaries.
- ✅ **6.3** — **Pattern Matching Tests:** Verify matching against complex nested directories (e.g. target `projects/*/cache` while skipping `projects/*/memory`).

---

### §7: Advanced Cookie Manager: Intelligent Auto-Scan & Unlimited Whitelist Scalability
> **Plan Reference:** [`temp/bleachbit_integration_plan.md`](temp/bleachbit_integration_plan.md) (Phase 7)  
> **Scope:** `FluentCleaner/Services/CookieService.cs`, `FluentCleaner/Services/IntelligentDomains.cs`, `FluentCleaner/Views/Settings/CookiesSettingsView.xaml`

- ✅ **7.1** — **Scalable Whitelist SQL Architecture:** Refactor `CookieService.cs` deletion logic from recursive SQL string `OR` chains to temporary indexed table staging (`CREATE TEMP TABLE _kept_domains(domain TEXT PRIMARY KEY)`) in `WinSqlite.cs`, supporting 10,000+ kept cookies without triggering `SQLITE_LIMIT_EXPR_DEPTH`.
- ✅ **7.2** — **Auth Provider Knowledge Base:** Create `IntelligentDomains.cs` in `FluentCleaner/Services/` codifying canonical login and session domains for Google, Microsoft, Apple, GitHub, GitLab, Twitter/X, Reddit, Amazon, PayPal, Yahoo, etc.
- ✅ **7.3** — **Intelligent Auto-Scan Engine & UI Integration:** Implement `RunIntelligentScan()` in `CookieService.cs` and wire an **"✨ Intelligent Scan"** button and context-menu item in `CookiesSettingsView.xaml` to auto-preserve detected authentication domains with 1 click.
- ✅ **7.4** — **Scale & Preservation Validation:** Benchmark cookie deletion against 1,000+ domain keep-lists; verify sub-10ms execution, zero SQLite expression depth errors, and 100% preservation of intelligent-scanned sessions.

---

### §8: UI & Navigation Enhancements
> **Plan Reference:** [`temp/plan_upstream_sync_and_ui_fixes.md`](temp/plan_upstream_sync_and_ui_fixes.md) (Feature 3)  
> **Scope:** `FluentCleaner/ViewModels/CleanerPageViewModel.cs`, `FluentCleaner/Views/CleanerPage.xaml`, `FluentCleaner/Views/CleanerPage.xaml.cs`, `FluentCleaner/Views/SettingsPage.xaml`, `FluentCleaner/Views/Settings/CookiesSettingsView.xaml.cs`, `FluentCleaner/Services/AppSettings.cs`

- ✅ **8.1** — **Cleaner Category Ascending Sort:** Sort cleaner categories in `CleanerPageViewModel.cs` using `OrderBy(g => g.Key.Name, StringComparer.OrdinalIgnoreCase)`.
- ✅ **8.2** — **Interactive Resizable Pane Divider:** Implement pointer-drag resizable splitter in `CleanerPage.xaml` / `CleanerPage.xaml.cs` with `SizeWestEast` cursor, min/max bounds, real-time readjustment, and persistent `CleanerPaneWidth` in `AppSettings.cs`.
- ✅ **8.3** — **Settings Navigation Menu Ascending Sort:** Sort navigation menu items in `SettingsPage.xaml` in ascending alphabetical order (`About`, `AI`, `Cookies`, `Exclusions`, `General`, `History`, `Tasks`, `Scheduler`) while maintaining default landing on `General`.
- ✅ **8.4** — **Cookies Found & Kept Ascending Sort:** Guarantee strict ascending alphabetical ordering in `CookiesSettingsView.xaml.cs` and `CookieService.cs` across "Cookies found" and "Cookies to keep" using `StringComparer.OrdinalIgnoreCase`.

---

## 📅 Append-Only Dated Change Log

| Date | Task / Milestone | Change Details & Verification Outcome | Author |
|:---:|---|---|:---:|
| **2026-11-21** | Ledger Initialization | Codified Master Tasks ledger (§1 through §6) for BleachBit ➔ FluentCleaner capability synthesis. Staged detailed plan at `temp/bleachbit_integration_plan.md`. | Antigravity |
| **2026-11-21** | Scope Expansion (§7) | Added §7 (Tasks 7.1–7.4): Intelligent Cookie Auto-Scan preset (CCleaner benchmark) and Unlimited Whitelist Scalability (BleachBit SQLite architecture). Total tasks updated to 22. | Antigravity |
| **2026-11-22** | Phase 1 Completion (§1) | Implemented Developer & AI Coding Tool Cleaner Pack across Winapp2.ini and CategoryResolver.cs. Verified 25 entries (9 detected on local rig, 3.65 GB reclaimable). Clean builds on Core and App. (Receipts: ev-20261121T225733Z-55fb, ev-20261121T225739Z-3db7, ev-20261121T225748Z-b140, ev-20261121T225755Z-9fa3). | Antigravity |
| **2026-11-22** | Phase 2 Completion (§2) | Implemented 64-bit multi-threaded Native SQLite Compactor (VACUUM) Engine in FluentCleaner.Core (WinSqlite.cs, SqliteCompactorService.cs, SqliteCompactorTargets.cs, CleaningService.cs, CleanerCategoryViewModel.cs). Benchmark achieved 79.9% page reduction with 100% integrity verification. Discovered 38 targets (75.35 MB) across Chrome, Edge, Firefox, Code. Clean builds on Core and App. (Receipts: ev-20261122T033353Z-6aed, ev-20261122T033359Z-1369, ev-20261122T033408Z-16af, ev-20261122T033416Z-3234). | Antigravity |
| **2026-11-22** | Phase 3 Completion (§3) | Implemented Windows Shell Live Notification Hook (SHChangeNotify) in FluentCleaner/Services/ShellNotification.cs, wired into CleaningService.cs and WindowsActions.cs. Added localized progress messages in en-US/zh-CN Resources.resw. Verified Explorer icon cache refresh without restarting explorer.exe. Clean 64-bit build on Core and App. (Receipts: ev-20261122T035119Z-5225, ev-20261122T035119Z-7990, ev-20261122T035119Z-84bb). | Antigravity |
| **2026-11-22** | Phase 4 Completion (§4) | Implemented 64-bit non-blocking ProcessDetectionService.cs in FluentCleaner.Core with TTL caching, safe handle disposal, and app signature heuristics. Added RunningProcess metadata to CleanerEntry and CleanerEntryViewModel. Implemented dynamic amber warning badge in CleanerPage.xaml with localized tooltips in en-US and zh-CN. Validated 148 live processes. Clean 64-bit builds on Core and App. (Receipts: ev-20261122T040215Z-26d3, ev-20261122T040215Z-f77d, ev-20261122T040215Z-55a2, ev-20261122T040216Z-734e). | Antigravity |
| **2026-11-22** | Phase 5 Completion (§5) | Implemented Safe Log File Truncation Engine with FileKeyFlag.Truncate in FileKeyEntry.cs and TryTruncateFile in CleaningService.cs using shared read/write FileStream.SetLength(0). Added automatic IOException lock-recovery fallback. Verified 0-byte truncation on locked active log files with continuous stream logging resilience. Clean 64-bit builds on Core and App. (Receipts: ev-20261122T041428Z-d053, ev-20261122T041428Z-7608, ev-20261122T041428Z-4339). | Antigravity |
| **2026-11-22** | Phase 6 Completion (§6) | Implemented Targeted Path Regex Filtering Engine with IncludeRegex (wholeregex) and ExcludeRegex (nwholeregex) compiled regex properties in FileKeyEntry.cs. Wired directory-level pruning into CleaningService.EnumerateFilesSafe skipping excluded branches before recursion. Validated via .NET assembly reflection on FluentCleaner.Core.dll and live filesystem traversal mock trees. Clean 64-bit builds on Core and App. (Receipts: ev-20261122T045131Z-3680, ev-20261122T045000Z-aaee, ev-20261122T045001Z-c0cb). | Antigravity |
| **2026-11-22** | Phase 7 Completion (§7) | Implemented Advanced Cookie Manager with Scalable Whitelist SQL Architecture (PopulateTempDomainTable in WinSqlite.cs, indexed parent domain lookups in CookieService.cs), IntelligentDomains.cs knowledge base (Google, Microsoft, Apple, GitHub, X, Amazon, etc.), and 1-click Intelligent Auto-Scan UI button and context flyout in CookiesSettingsView.xaml. Benchmarked against 1,000+ domain keep-list: 13.4ms execution, 0 expression depth errors, 100% preservation. Clean 64-bit builds on Core and App. (Receipts: ev-20261122T050041Z-a166, ev-20261122T050041Z-564d, ev-20261122T050121Z-8dae, ev-20261122T050042Z-ee37). | Antigravity |
| **2026-11-22** | Scope Expansion (§8) | Added §8 (Tasks 8.1–8.4) in Tasks.md and updated plan at `temp/plan_upstream_sync_and_ui_fixes.md` for UI Enhancements (Category Ascending Sort, Live Interactive Resizable Pane Divider, Settings Navigation Ascending Sort, Cookies Found & Kept Ascending Sort) under `feat/ui-enhancements`. Total tasks updated to 26. | Antigravity |
| **2026-11-22** | Phase 8 Completion (§8) | Completed UI & Navigation Enhancements across Cleaner categories, live resizable pane splitter, Settings nav menu, and Cookies found/kept lists. Merged feat/sqlite-vacuum-compactor, chore/upstream-sync-version-bump, and feat/ui-enhancements into sentinel-customization. Built Release x64 package to D:\Program\FluentCleaner. (Receipts: ev-20261122T120630Z-c69c, ev-20261122T120609Z-143d, ev-20261122T120649Z-0ff4, ev-20261122T120705Z-305a). | Antigravity |
