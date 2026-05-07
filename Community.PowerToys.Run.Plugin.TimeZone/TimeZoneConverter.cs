using System.Globalization;
using System.Text.RegularExpressions;

namespace Community.PowerToys.Run.Plugin.TimeZone;

internal static class TimeZoneConverter
{
    private static readonly TimeZoneSpec[] TimeZones =
    [
        new("ET", "Eastern Standard Time", null),
        new("Eastern Time", "Eastern Standard Time", null),
        new("EST", null, TimeSpan.FromHours(-5)),
        new("EDT", null, TimeSpan.FromHours(-4)),
        new("CT", "Central Standard Time", null),
        new("Central Time", "Central Standard Time", null),
        new("CST", null, TimeSpan.FromHours(-6)),
        new("CDT", null, TimeSpan.FromHours(-5)),
        new("MT", "Mountain Standard Time", null),
        new("Mountain Time", "Mountain Standard Time", null),
        new("MST", null, TimeSpan.FromHours(-7)),
        new("MDT", null, TimeSpan.FromHours(-6)),
        new("PT", "Pacific Standard Time", null),
        new("Pacific Time", "Pacific Standard Time", null),
        new("PST", null, TimeSpan.FromHours(-8)),
        new("PDT", null, TimeSpan.FromHours(-7)),
        new("AT", "Atlantic Standard Time", null),
        new("Atlantic Time", "Atlantic Standard Time", null),
        new("AST", null, TimeSpan.FromHours(-4)),
        new("ADT", null, TimeSpan.FromHours(-3)),
        new("Alaska Time", "Alaskan Standard Time", null),
        new("AKST", null, TimeSpan.FromHours(-9)),
        new("AKDT", null, TimeSpan.FromHours(-8)),
        new("Hawaii Time", "Hawaiian Standard Time", null),
        new("HST", null, TimeSpan.FromHours(-10)),
        new("Newfoundland Time", "Newfoundland Standard Time", null),
        new("NST", null, TimeSpan.FromHours(-3.5)),
        new("NDT", null, TimeSpan.FromHours(-2.5)),
        new("UTC", null, TimeSpan.Zero),
        new("GMT", null, TimeSpan.Zero),
        new("Z", null, TimeSpan.Zero),
        new("UK Time", "GMT Standard Time", null),
        new("British Time", "GMT Standard Time", null),
        new("BST", null, TimeSpan.FromHours(1)),
        new("WET", null, TimeSpan.Zero),
        new("WEST", null, TimeSpan.FromHours(1)),
        new("CET", null, TimeSpan.FromHours(1)),
        new("CEST", null, TimeSpan.FromHours(2)),
        new("Central European Time", "Central Europe Standard Time", null),
        new("EET", null, TimeSpan.FromHours(2)),
        new("EEST", null, TimeSpan.FromHours(3)),
        new("Eastern European Time", "E. Europe Standard Time", null),
        new("MSK", null, TimeSpan.FromHours(3)),
        new("TRT", null, TimeSpan.FromHours(3)),
        new("ART", null, TimeSpan.FromHours(-3)),
        new("BRT", null, TimeSpan.FromHours(-3)),
        new("WAT", null, TimeSpan.FromHours(1)),
        new("CAT", null, TimeSpan.FromHours(2)),
        new("SAST", null, TimeSpan.FromHours(2)),
        new("EAT", null, TimeSpan.FromHours(3)),
        new("GST", null, TimeSpan.FromHours(4)),
        new("IRST", null, TimeSpan.FromHours(3.5)),
        new("IRDT", null, TimeSpan.FromHours(4.5)),
        new("PKT", null, TimeSpan.FromHours(5)),
        new("IST", null, TimeSpan.FromHours(5.5)),
        new("India Time", "India Standard Time", null),
        new("NPT", null, TimeSpan.FromHours(5.75)),
        new("Bangladesh Time", "Bangladesh Standard Time", null),
        new("BST Bangladesh", null, TimeSpan.FromHours(6)),
        new("ICT", null, TimeSpan.FromHours(7)),
        new("WIB", null, TimeSpan.FromHours(7)),
        new("China Time", "China Standard Time", null),
        new("HKT", null, TimeSpan.FromHours(8)),
        new("SGT", null, TimeSpan.FromHours(8)),
        new("MYT", null, TimeSpan.FromHours(8)),
        new("PHT", null, TimeSpan.FromHours(8)),
        new("AWST", null, TimeSpan.FromHours(8)),
        new("WITA", null, TimeSpan.FromHours(8)),
        new("JST", null, TimeSpan.FromHours(9)),
        new("KST", null, TimeSpan.FromHours(9)),
        new("WIT", null, TimeSpan.FromHours(9)),
        new("ACST", null, TimeSpan.FromHours(9.5)),
        new("ACDT", null, TimeSpan.FromHours(10.5)),
        new("AEST", null, TimeSpan.FromHours(10)),
        new("AEDT", null, TimeSpan.FromHours(11)),
        new("ChST", null, TimeSpan.FromHours(10)),
        new("NZST", null, TimeSpan.FromHours(12)),
        new("NZDT", null, TimeSpan.FromHours(13)),
        new("New Zealand Time", "New Zealand Standard Time", null),
    ];

    private static readonly Dictionary<string, int> NumberWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["zero"] = 0, ["oh"] = 0, ["o"] = 0, ["one"] = 1, ["two"] = 2, ["three"] = 3,
        ["four"] = 4, ["five"] = 5, ["six"] = 6, ["seven"] = 7, ["eight"] = 8, ["nine"] = 9,
        ["ten"] = 10, ["eleven"] = 11, ["twelve"] = 12, ["thirteen"] = 13, ["fourteen"] = 14,
        ["fifteen"] = 15, ["sixteen"] = 16, ["seventeen"] = 17, ["eighteen"] = 18,
        ["nineteen"] = 19, ["twenty"] = 20, ["thirty"] = 30, ["forty"] = 40, ["fifty"] = 50,
    };

    public static IReadOnlyList<ConversionResult> Convert(string input, DateTimeOffset? nowUtc = null)
    {
        if (!TryParse(input, nowUtc ?? DateTimeOffset.UtcNow, out var request, out var error))
        {
            return [ConversionResult.Error(error)];
        }

        var results = new List<ConversionResult>();
        foreach (var candidate in BuildSourceCandidates(request))
        {
            var source = CreateDateTime(candidate.LocalTime, request.SourceTimeZone);
            var converted = ConvertToDestination(source, request.DestinationTimeZone);
            var destinationLabel = request.DestinationTimeZone.Name;

            results.Add(new ConversionResult(
                Success: true,
                Title: $"{converted:h:mm tt} {destinationLabel}",
                Subtitle: $"{FormatSource(candidate.LocalTime, request.SourceTimeZone, candidate.Assumption)} -> {converted:ddd d MMM yyyy, h:mm tt} {destinationLabel}",
                ClipboardText: $"{converted:yyyy-MM-dd HH:mm} {destinationLabel}",
                SourceText: input.Trim()));
        }

        return results;
    }

    private static bool TryParse(string input, DateTimeOffset nowUtc, out ConversionRequest request, out string error)
    {
        request = default!;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
        {
            error = "Try 10:30 AM CST to +10, ten thirty next Friday to AEDT, or 22:30 CET to UTC.";
            return false;
        }

        var normalized = Normalize(input);
        var explicitMatch = Regex.Match(normalized, @"^(?<source>.+?)\s+to\s+(?<destination>.+)$", RegexOptions.IgnoreCase);
        string left;
        TimeZoneSpec sourceTimeZone;
        TimeZoneSpec destinationTimeZone;

        if (explicitMatch.Success)
        {
            left = explicitMatch.Groups["source"].Value.Trim();
            var destinationText = explicitMatch.Groups["destination"].Value.Trim();
            if (!TryParseTimeZone(destinationText, requireFullMatch: true, out destinationTimeZone, out _))
            {
                error = "I could not read the destination timezone. Try +10, AEDT, UTC, CET, or Central European Time.";
                return false;
            }

            sourceTimeZone = TimeZoneSpec.Local();
            if (TryParseTimeZone(left, requireFullMatch: false, out var parsedSourceTimeZone, out var withoutSourceTimeZone))
            {
                sourceTimeZone = parsedSourceTimeZone;
                left = withoutSourceTimeZone;
            }
        }
        else if (TryParseTimeZone(normalized, requireFullMatch: false, out sourceTimeZone, out left) && !string.IsNullOrWhiteSpace(left))
        {
            destinationTimeZone = TimeZoneSpec.Local();
        }
        else
        {
            error = "Add a source timezone, or use 'to' for a destination, for example 10:30 AM CET or 10:30 AM to CET.";
            return false;
        }

        var sourceToday = GetTodayInZone(nowUtc, sourceTimeZone);
        var date = ExtractDate(ref left, sourceToday);
        if (!TryExtractTime(left, out var clock, out var remaining))
        {
            error = "I could not read the source time. Try 10:30 AM, 22:30, ten thirty, noon, or quarter past ten.";
            return false;
        }

        date = ParseDate(remaining, date);
        request = new ConversionRequest(date, clock, sourceTimeZone, destinationTimeZone);
        return true;
    }

    private static IEnumerable<SourceCandidate> BuildSourceCandidates(ConversionRequest request)
    {
        if (request.Clock.IsAmbiguousTwelveHour)
        {
            yield return new SourceCandidate(request.Date.ToDateTime(new TimeOnly(request.Clock.Hour, request.Clock.Minute)), "assuming AM");
            yield return new SourceCandidate(request.Date.ToDateTime(new TimeOnly(request.Clock.Hour + 12, request.Clock.Minute)), "assuming PM");
            yield break;
        }

        yield return new SourceCandidate(request.Date.ToDateTime(new TimeOnly(request.Clock.Hour, request.Clock.Minute)), null);
    }

    private static DateTimeOffset CreateDateTime(DateTime localTime, TimeZoneSpec timeZone)
    {
        if (timeZone.FixedOffset is not null)
        {
            return new DateTimeOffset(localTime, timeZone.FixedOffset.Value);
        }

        var zone = timeZone.WindowsId is null ? TimeZoneInfo.Local : FindTimeZone(timeZone.WindowsId, TimeSpan.Zero);
        return new DateTimeOffset(localTime, zone.GetUtcOffset(localTime));
    }

    private static DateTimeOffset ConvertToDestination(DateTimeOffset source, TimeZoneSpec destination)
    {
        if (destination.FixedOffset is not null)
        {
            return source.ToOffset(destination.FixedOffset.Value);
        }

        var zone = destination.WindowsId is null ? TimeZoneInfo.Local : FindTimeZone(destination.WindowsId, TimeSpan.Zero);
        return TimeZoneInfo.ConvertTime(source, zone);
    }

    private static TimeZoneInfo FindTimeZone(string windowsId, TimeSpan fallbackOffset)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(windowsId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(windowsId, fallbackOffset, windowsId, windowsId);
        }
    }

    private static DateOnly GetTodayInZone(DateTimeOffset nowUtc, TimeZoneSpec timeZone)
    {
        DateTimeOffset localNow;
        if (timeZone.FixedOffset is not null)
        {
            localNow = nowUtc.ToOffset(timeZone.FixedOffset.Value);
        }
        else
        {
            var zone = timeZone.WindowsId is null ? TimeZoneInfo.Local : FindTimeZone(timeZone.WindowsId, TimeSpan.Zero);
            localNow = TimeZoneInfo.ConvertTime(nowUtc, zone);
        }

        return DateOnly.FromDateTime(localNow.DateTime);
    }

    private static bool TryParseTimeZone(string input, bool requireFullMatch, out TimeZoneSpec timeZone, out string remaining)
    {
        if (TryParseOffsetTimeZone(input, requireFullMatch, out timeZone, out remaining))
        {
            return true;
        }

        foreach (var zone in TimeZones.OrderByDescending(zone => zone.Name.Length))
        {
            var pattern = requireFullMatch ? $@"^{Regex.Escape(zone.Name)}$" : $@"\b{Regex.Escape(zone.Name)}\b$";
            if (Regex.IsMatch(input, pattern, RegexOptions.IgnoreCase))
            {
                timeZone = zone;
                remaining = requireFullMatch ? string.Empty : Regex.Replace(input, pattern, string.Empty, RegexOptions.IgnoreCase).Trim();
                return true;
            }
        }

        timeZone = default!;
        remaining = input;
        return false;
    }

    private static bool TryParseOffsetTimeZone(string input, bool requireFullMatch, out TimeZoneSpec timeZone, out string remaining)
    {
        var boundary = requireFullMatch ? "^" : @"(?:^|\s)";
        var suffix = requireFullMatch ? "$" : "$";
        var match = Regex.Match(input, $@"{boundary}(?:(?<prefix>UTC|GMT)\s*)?(?<sign>[+-])\s*(?<hours>\d{{1,2}})(?::?(?<minutes>\d{{2}}))?{suffix}", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            timeZone = default!;
            remaining = input;
            return false;
        }

        var hours = int.Parse(match.Groups["hours"].Value, CultureInfo.InvariantCulture);
        var minutes = match.Groups["minutes"].Success ? int.Parse(match.Groups["minutes"].Value, CultureInfo.InvariantCulture) : 0;
        if (hours > 14 || minutes > 59)
        {
            timeZone = default!;
            remaining = input;
            return false;
        }

        var offset = new TimeSpan(hours, minutes, 0);
        if (match.Groups["sign"].Value == "-")
        {
            offset = -offset;
        }

        var name = $"{match.Groups["sign"].Value}{hours:00}:{minutes:00}";
        timeZone = new TimeZoneSpec(name, null, offset);
        remaining = requireFullMatch ? string.Empty : RemoveMatch(input, match);
        return true;
    }

    private static string Normalize(string input)
    {
        return Regex.Replace(input.Trim(), @"\s+", " ")
            .Replace(".", string.Empty, StringComparison.Ordinal)
            .Trim();
    }

    private static DateOnly ExtractDate(ref string input, DateOnly fallback)
    {
        if (TryExtractRelativeDay(ref input, fallback, out var relativeDate))
        {
            return relativeDate;
        }

        if (TryExtractWeekday(ref input, fallback, out var weekdayDate))
        {
            return weekdayDate;
        }

        if (TryExtractAbsoluteDate(ref input, fallback, out var absoluteDate))
        {
            return absoluteDate;
        }

        return fallback;
    }

    private static bool TryExtractRelativeDay(ref string input, DateOnly fallback, out DateOnly date)
    {
        foreach (var (word, days) in new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["today"] = 0, ["tomorrow"] = 1, ["tmr"] = 1, ["yesterday"] = -1 })
        {
            if (!Regex.IsMatch(input, $@"\b{word}\b", RegexOptions.IgnoreCase))
            {
                continue;
            }

            date = fallback.AddDays(days);
            input = Regex.Replace(input, $@"\b{word}\b", string.Empty, RegexOptions.IgnoreCase).Trim();
            return true;
        }

        date = fallback;
        return false;
    }

    private static bool TryExtractWeekday(ref string input, DateOnly fallback, out DateOnly date)
    {
        var match = Regex.Match(input, @"\b(?<modifier>next|last)?\s*(?<weekday>monday|mon|tuesday|tue|tues|wednesday|wed|thursday|thu|thur|thurs|friday|fri|saturday|sat|sunday|sun)\b", RegexOptions.IgnoreCase);
        if (!match.Success || !TryParseDayOfWeek(match.Groups["weekday"].Value, out var targetDay))
        {
            date = fallback;
            return false;
        }

        var delta = ((int)targetDay - (int)fallback.DayOfWeek + 7) % 7;
        var modifier = match.Groups["modifier"].Value;
        if (modifier.Equals("next", StringComparison.OrdinalIgnoreCase) && delta == 0)
        {
            delta = 7;
        }
        else if (modifier.Equals("last", StringComparison.OrdinalIgnoreCase))
        {
            delta = delta == 0 ? -7 : delta - 7;
        }

        date = fallback.AddDays(delta);
        input = RemoveMatch(input, match);
        return true;
    }

    private static bool TryParseDayOfWeek(string input, out DayOfWeek dayOfWeek)
    {
        var normalized = input.ToLowerInvariant();
        dayOfWeek = normalized switch
        {
            "sunday" or "sun" => DayOfWeek.Sunday,
            "monday" or "mon" => DayOfWeek.Monday,
            "tuesday" or "tue" or "tues" => DayOfWeek.Tuesday,
            "wednesday" or "wed" => DayOfWeek.Wednesday,
            "thursday" or "thu" or "thur" or "thurs" => DayOfWeek.Thursday,
            "friday" or "fri" => DayOfWeek.Friday,
            "saturday" or "sat" => DayOfWeek.Saturday,
            _ => default,
        };

        return normalized is "sunday" or "sun"
            or "monday" or "mon"
            or "tuesday" or "tue" or "tues"
            or "wednesday" or "wed"
            or "thursday" or "thu" or "thur" or "thurs"
            or "friday" or "fri"
            or "saturday" or "sat";
    }

    private static bool TryExtractAbsoluteDate(ref string input, DateOnly fallback, out DateOnly date)
    {
        var datePatterns = new[]
        {
            @"\b\d{4}[-/]\d{1,2}[-/]\d{1,2}\b",
            @"\b\d{1,2}[-/]\d{1,2}[-/]\d{2,4}\b",
            @"\b\d{1,2}[-/]\d{1,2}\b",
            @"\b\d{1,2}\s+(?:jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)(?:,?\s+\d{4})?\b",
            @"\b(?:jan(?:uary)?|feb(?:ruary)?|mar(?:ch)?|apr(?:il)?|may|jun(?:e)?|jul(?:y)?|aug(?:ust)?|sep(?:t(?:ember)?)?|oct(?:ober)?|nov(?:ember)?|dec(?:ember)?)\s+\d{1,2}(?:,?\s+\d{4})?\b",
        };

        foreach (var pattern in datePatterns)
        {
            var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
            if (!match.Success || !TryParseDateText(match.Value, fallback, out date))
            {
                continue;
            }

            input = RemoveMatch(input, match);
            return true;
        }

        date = fallback;
        return false;
    }

    private static bool TryParseDateText(string text, DateOnly fallback, out DateOnly date)
    {
        var hasYear = Regex.IsMatch(text, @"\b\d{4}\b|\b\d{1,2}[-/]\d{1,2}[-/]\d{2,4}\b");
        var candidate = hasYear ? text : $"{text} {fallback.Year}";
        if (!DateTime.TryParse(candidate, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDate)
            && !DateTime.TryParse(candidate, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out parsedDate))
        {
            date = fallback;
            return false;
        }

        date = DateOnly.FromDateTime(parsedDate);
        return true;
    }

    private static bool TryExtractTime(string input, out ClockTime clock, out string remaining)
    {
        if (TryExtractNumericTime(input, out clock, out remaining) || TryExtractWordTime(input, out clock, out remaining))
        {
            return true;
        }

        return TryExtractCompactTime(input, out clock, out remaining);
    }

    private static bool TryExtractNumericTime(string input, out ClockTime clock, out string remaining)
    {
        foreach (Match match in Regex.Matches(input, @"\b(?<hour>\d{1,2})(?::(?<minute>\d{2}))?\s*(?<meridiem>am|pm)?\b", RegexOptions.IgnoreCase))
        {
            var hasMinute = match.Groups["minute"].Success;
            var hasMeridiem = match.Groups["meridiem"].Success;
            if (!hasMinute && !hasMeridiem && Regex.IsMatch(input, @"\d{1,2}[-/]\d{1,2}"))
            {
                continue;
            }

            var hour = int.Parse(match.Groups["hour"].Value, CultureInfo.InvariantCulture);
            var minute = hasMinute ? int.Parse(match.Groups["minute"].Value, CultureInfo.InvariantCulture) : 0;
            if (!TryBuildClock(hour, minute, match.Groups["meridiem"].Value, out clock))
            {
                continue;
            }

            remaining = RemoveMatch(input, match);
            return true;
        }

        clock = default;
        remaining = input;
        return false;
    }

    private static bool TryExtractCompactTime(string input, out ClockTime clock, out string remaining)
    {
        foreach (Match match in Regex.Matches(input, @"\b(?<digits>\d{3,4})\s*(?<meridiem>am|pm)?\b", RegexOptions.IgnoreCase))
        {
            var digits = match.Groups["digits"].Value;
            if (digits.Length == 4 && digits.StartsWith("20", StringComparison.Ordinal) && !match.Groups["meridiem"].Success)
            {
                continue;
            }

            var hour = int.Parse(digits[..^2], CultureInfo.InvariantCulture);
            var minute = int.Parse(digits[^2..], CultureInfo.InvariantCulture);
            if (!TryBuildClock(hour, minute, match.Groups["meridiem"].Value, out clock))
            {
                continue;
            }

            remaining = RemoveMatch(input, match);
            return true;
        }

        clock = default;
        remaining = input;
        return false;
    }

    private static bool TryExtractWordTime(string input, out ClockTime clock, out string remaining)
    {
        var words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var meridiem = ExtractMeridiem(words);

        if (words.Contains("noon", StringComparer.OrdinalIgnoreCase))
        {
            clock = new ClockTime(12, 0, false);
            remaining = RemoveWords(words, "noon");
            return true;
        }

        if (words.Contains("midnight", StringComparer.OrdinalIgnoreCase))
        {
            clock = new ClockTime(0, 0, false);
            remaining = RemoveWords(words, "midnight");
            return true;
        }

        if (TryParseRelativeWordTime(words, meridiem, out clock, out remaining))
        {
            return true;
        }

        var numberValues = words
            .Select((word, index) => new { Index = index, Value = NumberWords.TryGetValue(word, out var value) ? value : (int?)null })
            .Where(item => item.Value is not null)
            .ToList();
        if (numberValues.Count == 0)
        {
            clock = default;
            remaining = input;
            return false;
        }

        var hour = numberValues[0].Value!.Value;
        var minute = 0;
        var minuteStart = 1;
        if (numberValues.Count >= 3 && hour == 20 && numberValues[1].Value is >= 1 and <= 3)
        {
            hour += numberValues[1].Value!.Value;
            minuteStart = 2;
        }

        if (numberValues.Count > minuteStart)
        {
            minute = numberValues.Skip(minuteStart).Sum(item => item.Value!.Value);
        }

        if (!TryBuildClock(hour, minute, meridiem, out clock))
        {
            remaining = input;
            return false;
        }

        var usedIndexes = numberValues.Select(item => item.Index).ToHashSet();
        remaining = string.Join(' ', words.Where((_, index) => !usedIndexes.Contains(index)));
        return true;
    }

    private static bool TryParseRelativeWordTime(List<string> words, string meridiem, out ClockTime clock, out string remaining)
    {
        var pastIndex = words.FindIndex(word => word.Equals("past", StringComparison.OrdinalIgnoreCase));
        var toIndex = words.FindIndex(word => word.Equals("to", StringComparison.OrdinalIgnoreCase));

        if (pastIndex > 0 && pastIndex + 1 < words.Count && TryParseMinutePhrase(words.Take(pastIndex), out var pastMinutes) && NumberWords.TryGetValue(words[pastIndex + 1], out var pastHour))
        {
            return FinishRelative(pastHour, pastMinutes, meridiem, words, [.. Enumerable.Range(0, pastIndex + 2)], out clock, out remaining);
        }

        if (toIndex > 0 && toIndex + 1 < words.Count && TryParseMinutePhrase(words.Take(toIndex), out var toMinutes) && NumberWords.TryGetValue(words[toIndex + 1], out var toHour))
        {
            var hour = toHour == 1 ? 12 : toHour - 1;
            return FinishRelative(hour, 60 - toMinutes, meridiem, words, [.. Enumerable.Range(0, toIndex + 2)], out clock, out remaining);
        }

        clock = default;
        remaining = string.Join(' ', words);
        return false;
    }

    private static bool FinishRelative(int hour, int minute, string meridiem, List<string> words, int[] usedIndexes, out ClockTime clock, out string remaining)
    {
        if (!TryBuildClock(hour, minute, meridiem, out clock))
        {
            remaining = string.Join(' ', words);
            return false;
        }

        var used = usedIndexes.ToHashSet();
        remaining = string.Join(' ', words.Where((_, index) => !used.Contains(index)));
        return true;
    }

    private static bool TryParseMinutePhrase(IEnumerable<string> words, out int minutes)
    {
        var list = words.ToList();
        if (list.Count == 1 && list[0].Equals("quarter", StringComparison.OrdinalIgnoreCase))
        {
            minutes = 15;
            return true;
        }

        if (list.Count == 1 && list[0].Equals("half", StringComparison.OrdinalIgnoreCase))
        {
            minutes = 30;
            return true;
        }

        minutes = list.Sum(word => NumberWords.TryGetValue(word, out var value) ? value : 0);
        return minutes is > 0 and < 60;
    }

    private static string ExtractMeridiem(List<string> words)
    {
        var meridiem = string.Empty;
        for (var index = words.Count - 1; index >= 0; index--)
        {
            if (words[index].Equals("am", StringComparison.OrdinalIgnoreCase) || words[index].Equals("pm", StringComparison.OrdinalIgnoreCase))
            {
                meridiem = words[index].ToLowerInvariant();
                words.RemoveAt(index);
            }
        }

        return meridiem;
    }

    private static bool TryBuildClock(int hour, int minute, string meridiem, out ClockTime clock)
    {
        clock = default;
        if (minute is < 0 or > 59)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(meridiem))
        {
            if (hour is < 1 or > 12)
            {
                return false;
            }

            var normalizedHour = hour % 12;
            if (meridiem.Equals("pm", StringComparison.OrdinalIgnoreCase))
            {
                normalizedHour += 12;
            }

            clock = new ClockTime(normalizedHour, minute, false);
            return true;
        }

        if (hour is < 0 or > 23)
        {
            return false;
        }

        clock = new ClockTime(hour, minute, hour is >= 1 and <= 11);
        return true;
    }

    private static DateOnly ParseDate(string input, DateOnly fallback)
    {
        var cleaned = Regex.Replace(input, @"\b(at|on|in)\b", string.Empty, RegexOptions.IgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(cleaned))
        {
            return fallback;
        }

        return DateTime.TryParse(cleaned, CultureInfo.CurrentCulture, DateTimeStyles.AllowWhiteSpaces, out var parsedDate)
            ? DateOnly.FromDateTime(parsedDate)
            : fallback;
    }

    private static string FormatSource(DateTime source, TimeZoneSpec sourceTimeZone, string? assumption)
    {
        var suffix = assumption is null ? string.Empty : $" ({assumption})";
        return $"{source:ddd d MMM, h:mm tt} {sourceTimeZone.Name}{suffix}";
    }

    private static string RemoveMatch(string input, Match match)
    {
        return (input[..match.Index] + input[(match.Index + match.Length)..]).Trim();
    }

    private static string RemoveWords(IEnumerable<string> words, params string[] wordsToRemove)
    {
        return string.Join(' ', words.Where(word => !wordsToRemove.Contains(word, StringComparer.OrdinalIgnoreCase)));
    }

    private sealed record TimeZoneSpec(string Name, string? WindowsId, TimeSpan? FixedOffset)
    {
        public static TimeZoneSpec Local()
        {
            return new TimeZoneSpec(TimeZoneInfo.Local.IsDaylightSavingTime(DateTime.Now) ? TimeZoneInfo.Local.DaylightName : TimeZoneInfo.Local.StandardName, null, null);
        }
    }

    private sealed record ConversionRequest(DateOnly Date, ClockTime Clock, TimeZoneSpec SourceTimeZone, TimeZoneSpec DestinationTimeZone);

    private readonly record struct ClockTime(int Hour, int Minute, bool IsAmbiguousTwelveHour);

    private sealed record SourceCandidate(DateTime LocalTime, string? Assumption);
}

internal sealed record ConversionResult(
    bool Success,
    string Title,
    string Subtitle,
    string ClipboardText,
    string SourceText)
{
    public static ConversionResult Error(string message)
    {
        return new ConversionResult(false, "Could not convert time", message, string.Empty, string.Empty);
    }
}
