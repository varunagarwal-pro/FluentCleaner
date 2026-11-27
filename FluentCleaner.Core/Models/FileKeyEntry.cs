using System;
using System.Text.RegularExpressions;

namespace FluentCleaner.Models;

// Controls how a FileKey directory is scanned and cleaned.
[Flags]
public enum FileKeyFlag
{
    None = 0,
    Recurse = 1 << 0,       // Scan all subdirectories recursively.
    RemoveSelf = 1 << 1,    // Like Recurse, but also prune empty directories afterwards.
    Truncate = 1 << 2       // Zero locked or active log files in-place instead of deleting directory entry.
}

/* Parsed representation of one FileKeyN= line from Winapp2.ini.
   Format:  FileKey1=<path>|<pattern(s)>[|RECURSE|REMOVESELF|TRUNCATE][|wholeregex=<regex>][|nwholeregex=<regex>]
   Example: FileKey1=%LocalAppData%\Temp|*.tmp;*.log|RECURSE */
public class FileKeyEntry
{
    // Directory path to scan. May contain %EnvVar% tokens and * wildcards in path segments.
    public string Path { get; set; } = "";

    /* Semicolon-separated file filter(s), e.g. "*.tmp" or "*.log;*.bak".
       Defaults to "*.*" when no pattern is specified in the ini. */
    public string Pattern { get; set; } = "*.*";

    // Whether to recurse into subdirectories, remove empty dirs afterwards, or truncate in-place.
    public FileKeyFlag Flag { get; set; } = FileKeyFlag.None;

    // Compiled regex for full path inclusion (BleachBit 'wholeregex')
    public Regex? IncludeRegex { get; set; }
    public string? IncludeRegexPattern { get; set; }

    // Compiled regex for full path exclusion (BleachBit 'nwholeregex')
    public Regex? ExcludeRegex { get; set; }
    public string? ExcludeRegexPattern { get; set; }

    public static FileKeyEntry Parse(string value)
    {
        var parts = value.Split('|');
        var entry = new FileKeyEntry { Path = parts[0].Trim() };

        for (int i = 1; i < parts.Length; i++)
        {
            var p = parts[i].Trim();
            if (string.IsNullOrEmpty(p)) continue;

            if (p.StartsWith("wholeregex=", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("include_regex=", StringComparison.OrdinalIgnoreCase) ||
                p.StartsWith("regex=", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = p.Substring(p.IndexOf('=') + 1).Trim();
                entry.IncludeRegexPattern = pattern;
                try { entry.IncludeRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)); }
                catch { }
            }
            else if (p.StartsWith("nwholeregex=", StringComparison.OrdinalIgnoreCase) ||
                     p.StartsWith("exclude_regex=", StringComparison.OrdinalIgnoreCase) ||
                     p.StartsWith("nregex=", StringComparison.OrdinalIgnoreCase))
            {
                var pattern = p.Substring(p.IndexOf('=') + 1).Trim();
                entry.ExcludeRegexPattern = pattern;
                try { entry.ExcludeRegex = new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)); }
                catch { }
            }
            else if (TryParseFlag(p, out var flag))
            {
                entry.Flag |= flag;
            }
            else if (i == 1)
            {
                // First segment after path is the file pattern if not a flag or regex
                entry.Pattern = p;
            }
        }

        return entry;
    }

    private static bool TryParseFlag(string raw, out FileKeyFlag result)
    {
        result = FileKeyFlag.None;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var tokens = raw.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
        bool matched = false;

        foreach (var token in tokens)
        {
            var t = token.Trim().ToUpperInvariant();
            switch (t)
            {
                case "RECURSE":
                    result |= FileKeyFlag.Recurse;
                    matched = true;
                    break;
                case "REMOVESELF":
                    result |= FileKeyFlag.RemoveSelf | FileKeyFlag.Recurse;
                    matched = true;
                    break;
                case "TRUNCATE":
                    result |= FileKeyFlag.Truncate;
                    matched = true;
                    break;
            }
        }

        return matched;
    }
}
