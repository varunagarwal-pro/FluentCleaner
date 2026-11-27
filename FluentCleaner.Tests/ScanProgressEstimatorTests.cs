using FluentCleaner.Services;
using Xunit;

namespace FluentCleaner.Tests;

/// <summary>
/// Unit tests for <see cref="ScanProgressEstimator"/> following the TDD contract spec.
/// Covers all five scenarios from §13.5 of Tasks.md.
/// </summary>
public sealed class ScanProgressEstimatorTests
{
    // ------------------------------------------------------------------ //
    //  1. Cold-start (done=0) → warm-up guard fires, no ETA              //
    // ------------------------------------------------------------------ //
    [Fact]
    public void Estimate_ZeroDone_ReturnsZeroPercentAndNoEta()
    {
        var result = ScanProgressEstimator.Estimate(done: 0, total: 192, elapsed: TimeSpan.Zero);

        Assert.Equal(0, result.Percent);
        Assert.Null(result.Eta);
        // Could be "0%" or "Calculating ETA…" — either is fine; must NOT say "Done"
        Assert.DoesNotContain("Done", result.DisplayText);
    }

    // ------------------------------------------------------------------ //
    //  2. 100% done → Percent=100, Eta=Zero, text contains "Done"        //
    // ------------------------------------------------------------------ //
    [Fact]
    public void Estimate_AllDone_Returns100PercentAndEtaZero()
    {
        var result = ScanProgressEstimator.Estimate(done: 192, total: 192, elapsed: TimeSpan.FromSeconds(30));

        Assert.Equal(100, result.Percent);
        Assert.Equal(TimeSpan.Zero, result.Eta);
        Assert.Contains("Done", result.DisplayText);
    }

    // ------------------------------------------------------------------ //
    //  3. 50% with 10 s elapsed → Percent≈50, ETA≈10 s                   //
    // ------------------------------------------------------------------ //
    [Fact]
    public void Estimate_HalfwayWith10sElapsed_ReturnsApprox50PctAnd10sEta()
    {
        // 50 done out of 100 in 10 s → rate 5/s → 50 remaining → ~10 s ETA
        var result = ScanProgressEstimator.Estimate(done: 50, total: 100, elapsed: TimeSpan.FromSeconds(10));

        Assert.Equal(50.0, result.Percent, precision: 1);
        Assert.NotNull(result.Eta);
        // ETA should be within ±1 s of 10 s
        Assert.InRange(result.Eta!.Value.TotalSeconds, 9.0, 11.0);
        // FormatEta for ~10 s → "~10s left"
        Assert.Contains("10s left", result.DisplayText);
    }

    // ------------------------------------------------------------------ //
    //  4. FormatEta(0.5 s) → "< 1s left"                                 //
    // ------------------------------------------------------------------ //
    [Theory]
    [InlineData(0.0)]
    [InlineData(0.4)]
    [InlineData(0.99)]
    public void FormatEta_UnderOneSecond_ReturnsLessThan1sLeft(double seconds)
    {
        var formatted = ScanProgressEstimator.FormatEta(TimeSpan.FromSeconds(seconds));
        Assert.Equal("< 1s left", formatted);
    }

    // ------------------------------------------------------------------ //
    //  5. FormatEta(90 s) → "~1m 30s left"                               //
    // ------------------------------------------------------------------ //
    [Fact]
    public void FormatEta_NinetySeconds_Returns1m30sLeft()
    {
        var formatted = ScanProgressEstimator.FormatEta(TimeSpan.FromSeconds(90));
        Assert.Equal("~1m 30s left", formatted);
    }

    // ------------------------------------------------------------------ //
    //  Bonus: FormatEta for exact minute (no seconds suffix)              //
    // ------------------------------------------------------------------ //
    [Fact]
    public void FormatEta_ExactMinute_ReturnsMinutesOnlyLeft()
    {
        var formatted = ScanProgressEstimator.FormatEta(TimeSpan.FromMinutes(2));
        Assert.Equal("~2m left", formatted);
    }

    // ------------------------------------------------------------------ //
    //  Warm-up guard: done<3 within warm-up → Calculating ETA            //
    // ------------------------------------------------------------------ //
    [Fact]
    public void Estimate_InWarmup_ReturnsCalculatingEtaText()
    {
        // 2 done in 500 ms (past the 400 ms window, but done<3 → still warm-up)
        var result = ScanProgressEstimator.Estimate(done: 2, total: 100, elapsed: TimeSpan.FromMilliseconds(600));

        Assert.Null(result.Eta);
        Assert.Contains("Calculating", result.DisplayText);
    }

    // ------------------------------------------------------------------ //
    //  Edge: total=0 → guard against division by zero                     //
    // ------------------------------------------------------------------ //
    [Fact]
    public void Estimate_TotalZero_ReturnsZeroPercentSafely()
    {
        var result = ScanProgressEstimator.Estimate(done: 0, total: 0, elapsed: TimeSpan.FromSeconds(1));

        Assert.Equal(0, result.Percent);
        Assert.Null(result.Eta);
    }
}
