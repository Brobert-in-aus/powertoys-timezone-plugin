using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Community.PowerToys.Run.Plugin.TimeZone.UnitTests;

[TestClass]
public sealed class TimeZoneConverterTests
{
    private static readonly DateTimeOffset MayUtc = new(2026, 5, 7, 12, 0, 0, TimeSpan.Zero);

    [TestMethod]
    public void Convert_handles_source_timezone_to_utc_offset()
    {
        var results = TimeZoneConverter.Convert("10:30 AM CST to +10", MayUtc);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("2:30 AM +10:00", results[0].Title);
        StringAssert.Contains(results[0].Subtitle, "Fri 8 May 2026");
    }

    [TestMethod]
    public void Convert_handles_words_and_destination_timezone()
    {
        var results = TimeZoneConverter.Convert("ten thirty next friday to AEDT", MayUtc);

        Assert.AreEqual(2, results.Count);
        StringAssert.Contains(results[0].Title, "AEDT");
        StringAssert.Contains(results[1].Title, "AEDT");
        StringAssert.Contains(results[0].Subtitle, "assuming AM");
        StringAssert.Contains(results[1].Subtitle, "assuming PM");
    }

    [TestMethod]
    public void Convert_handles_destination_timezone_with_daylight_rules()
    {
        var results = TimeZoneConverter.Convert("10 May 10:00 AM UTC to Central European Time", MayUtc);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("12:00 PM Central European Time", results[0].Title);
    }

    [TestMethod]
    public void Convert_handles_destination_gmt_offset()
    {
        var results = TimeZoneConverter.Convert("10 May 10:00 AM UTC to GMT-0530", MayUtc);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("4:30 AM -05:30", results[0].Title);
    }

    [TestMethod]
    public void Convert_without_to_uses_final_timezone_as_source_and_local_as_destination()
    {
        var results = TimeZoneConverter.Convert("10:30 AM CET", MayUtc);
        var expected = TimeZoneInfo.ConvertTime(new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.FromHours(1)), TimeZoneInfo.Local);

        Assert.AreEqual(1, results.Count);
        StringAssert.StartsWith(results[0].Title, expected.ToString("h:mm tt"));
        StringAssert.Contains(results[0].Subtitle, "CET");
    }

    [TestMethod]
    public void Convert_with_to_and_no_source_timezone_uses_local_as_source()
    {
        var results = TimeZoneConverter.Convert("10 May 10:00 AM to CET", MayUtc);

        Assert.AreEqual(1, results.Count);
        StringAssert.Contains(results[0].Title, "CET");
    }

    [TestMethod]
    public void Convert_reports_missing_timezone_or_destination()
    {
        var results = TimeZoneConverter.Convert("10:30 AM", MayUtc);

        Assert.AreEqual(1, results.Count);
        Assert.IsFalse(results[0].Success);
        StringAssert.Contains(results[0].Subtitle, "source timezone");
    }

    [TestMethod]
    public void Convert_reports_unknown_destination_timezone()
    {
        var results = TimeZoneConverter.Convert("10:30 AM CST to Atlantis", MayUtc);

        Assert.AreEqual(1, results.Count);
        Assert.IsFalse(results[0].Success);
        StringAssert.Contains(results[0].Subtitle, "destination timezone");
    }

    [TestMethod]
    public void Convert_handles_weekday_absolute_date_before_time()
    {
        var results = TimeZoneConverter.Convert("Saturday, 16 May 2026, at 15:00 CEST to AEST", MayUtc);

        Assert.AreEqual(1, results.Count);
        Assert.AreEqual("11:00 PM AEST", results[0].Title);
        StringAssert.Contains(results[0].Subtitle, "Sat 16 May, 3:00 PM CEST");
    }
}
