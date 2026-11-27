using FluentCleaner.Models;
using FluentCleaner.Services;
using Xunit;

namespace FluentCleaner.Tests;

public class AuditRemediationTests
{
    [Fact]
    public void ScanResult_DefaultState_IsEmpty()
    {
        var result = new ScanResult { Entry = new CleanerEntry { Name = "Test" } };
        Assert.Empty(result.FilesToDelete);
        Assert.Empty(result.LockedFiles);
        Assert.Equal(0, result.TotalBytes);
    }

    [Fact]
    public void FileKeyEntry_Parse_Sets250MsReDoSTimeout()
    {
        var fk = FileKeyEntry.Parse(@"%LocalAppData%\Test|*.*|RECURSE|wholeregex=.*cache.*|nwholeregex=.*keep.*");
        Assert.NotNull(fk.IncludeRegex);
        Assert.NotNull(fk.ExcludeRegex);
        Assert.Equal(TimeSpan.FromMilliseconds(250), fk.IncludeRegex!.MatchTimeout);
        Assert.Equal(TimeSpan.FromMilliseconds(250), fk.ExcludeRegex!.MatchTimeout);
    }

    [Theory]
    [InlineData("Dangerous Firefox", "Browsers")]
    [InlineData("Firefox", "Browsers")]
    [InlineData("Google Chrome", "Browsers")]
    [InlineData("Microsoft Edge", "Browsers")]
    [InlineData("Windows", "Windows")]
    public void CategoryStylingResolver_DoesNotReturnSyntheticPng(string category, string macro)
    {
        var style = CategoryStylingResolver.Resolve(category, macro);
        Assert.Null(style.AppIconSource);
        Assert.False(style.HasAppIcon);
        Assert.False(string.IsNullOrWhiteSpace(style.Glyph));
    }

    [Fact]
    public void RepoAssets_HaveNoSyntheticPngsOrOrphanedShellXaml()
    {
        var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "FluentCleaner"));
        Assert.False(File.Exists(Path.Combine(root, "Styles", "Shell.xaml")), "Orphaned Styles/Shell.xaml should be deleted");

        var iconsDir = Path.Combine(root, "Assets", "Icons");
        if (Directory.Exists(iconsDir))
        {
            Assert.Empty(Directory.GetFiles(iconsDir, "*.png"));
        }
    }
}
