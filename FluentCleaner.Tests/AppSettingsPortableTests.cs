using FluentCleaner.Services;
using Xunit;

namespace FluentCleaner.Tests;

public sealed class AppSettingsPortableTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "fc_portable_test_" + Guid.NewGuid().ToString("N"));
    private readonly string _portablePath;
    private readonly string _roamingPath;

    public AppSettingsPortableTests()
    {
        _portablePath = Path.Combine(_root, "app", "settings.json");
        _roamingPath = Path.Combine(_root, "appdata", "FluentCleaner", "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(_portablePath)!);
        Directory.CreateDirectory(Path.GetDirectoryName(_roamingPath)!);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true); } catch { }
    }

    [Fact]
    public void ResolveSettingsPath_ReturnsPortablePath_WhenPortableFileExists()
    {
        File.WriteAllText(_portablePath, "{}");
        File.WriteAllText(_roamingPath, "{}");

        var resolved = AppSettings.ResolveSettingsPath(_portablePath, _roamingPath);

        Assert.Equal(_portablePath, resolved);
    }

    [Fact]
    public void ResolveSettingsPath_ReturnsRoamingPath_WhenPortableFileMissing()
    {
        File.WriteAllText(_roamingPath, "{}");

        var resolved = AppSettings.ResolveSettingsPath(_portablePath, _roamingPath);

        Assert.Equal(_roamingPath, resolved);
    }

    [Fact]
    public void SwitchStorageLocation_EnablePortable_WritesPortableJsonAndUpdatesActiveFile()
    {
        var settings = new AppSettings
        {
            Theme = "Dark",
            CookieDomainsToKeep = ["github.com", "accounts.google.com"]
        };
        string activeFile = _roamingPath;

        bool ok = AppSettings.SwitchStorageLocation(
            settings, enablePortable: true, _portablePath, _roamingPath, ref activeFile, out string? error);

        Assert.True(ok, error);
        Assert.Null(error);
        Assert.Equal(_portablePath, activeFile);
        Assert.True(File.Exists(_portablePath));

        var reloaded = AppSettings.LoadFrom(_portablePath);
        Assert.Equal("Dark", reloaded.Theme);
        Assert.Equal(["github.com", "accounts.google.com"], reloaded.CookieDomainsToKeep);
    }

    [Fact]
    public void SwitchStorageLocation_DisablePortable_WritesRoamingAndRemovesPortableFile()
    {
        File.WriteAllText(_portablePath, "{}");
        var settings = new AppSettings
        {
            Language = "de-DE",
            CookieDomainsToKeep = ["mail.google.com"]
        };
        string activeFile = _portablePath;

        bool ok = AppSettings.SwitchStorageLocation(
            settings, enablePortable: false, _portablePath, _roamingPath, ref activeFile, out string? error);

        Assert.True(ok, error);
        Assert.Null(error);
        Assert.Equal(_roamingPath, activeFile);
        Assert.False(File.Exists(_portablePath));
        Assert.True(File.Exists(_roamingPath));

        var reloaded = AppSettings.LoadFrom(_roamingPath);
        Assert.Equal("de-DE", reloaded.Language);
        Assert.Equal(["mail.google.com"], reloaded.CookieDomainsToKeep);
    }
}
