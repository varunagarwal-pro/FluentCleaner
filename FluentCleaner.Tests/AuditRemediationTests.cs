using FluentCleaner.Models;
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
}
