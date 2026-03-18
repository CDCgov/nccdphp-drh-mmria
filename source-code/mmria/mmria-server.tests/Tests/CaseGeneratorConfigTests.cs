#nullable enable

using NUnit.Framework;
using mmria.common.Testing.CaseGeneration.Models;

namespace mmria_server.tests.Tests;

[TestFixture]
public class CaseGeneratorConfigTests
{
    [Test]
    public void GenerationConfig_ErVisitVitalSignsCountsCsv_ParsesAndCycles()
    {
        var config = new GenerationConfig
        {
            ErVisitVitalSignsCountsCsv = "0, 1, 5, 20, 30"
        };

        var counts = config.GetErVisitVitalSignsCounts();

        Assert.That(counts, Is.EqualTo(new[] { 0, 1, 5, 20, 30 }));
        Assert.That(config.GetErVisitVitalSignsCountForCase(1), Is.EqualTo(0));
        Assert.That(config.GetErVisitVitalSignsCountForCase(2), Is.EqualTo(1));
        Assert.That(config.GetErVisitVitalSignsCountForCase(3), Is.EqualTo(5));
        Assert.That(config.GetErVisitVitalSignsCountForCase(4), Is.EqualTo(20));
        Assert.That(config.GetErVisitVitalSignsCountForCase(5), Is.EqualTo(30));
        Assert.That(config.GetErVisitVitalSignsCountForCase(6), Is.EqualTo(0));
        Assert.That(config.GetErVisitVitalSignsCountForCase(7), Is.EqualTo(1));
    }

    [Test]
    public void GenerationConfig_ErVisitVitalSignsCountsCsv_IgnoresInvalidValues()
    {
        var config = new GenerationConfig
        {
            ErVisitVitalSignsCountsCsv = "5, nope, -1, 12, , 0"
        };

        var counts = config.GetErVisitVitalSignsCounts();

        Assert.That(counts, Is.EqualTo(new[] { 5, 12, 0 }));
    }
}
