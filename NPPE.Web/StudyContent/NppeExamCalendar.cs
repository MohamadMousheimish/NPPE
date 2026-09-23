using System.Globalization;

namespace NPPE.Web.StudyContent;

/// <summary>
/// Official NPPE sitting dates (source: nppexam.ca). Drives the landing-page countdown.
/// Update roughly yearly as the regulator publishes new dates.
/// </summary>
public sealed record NppeSitting(DateOnly Start, DateOnly End, DateOnly RegistrationDeadline)
{
    /// <summary>e.g. "11–13 January 2027" (localized month name; day–day month year).</summary>
    public string DisplayRange(CultureInfo culture)
    {
        if (Start.Month == End.Month)
            return $"{Start.Day}–{End.Day} {Start.ToString("MMMM", culture)} {End.Year}";
        // Spans two months (same year in our calendar): "30 January – 2 February 2027".
        return $"{Start.Day} {Start.ToString("MMMM", culture)} – {End.Day} {End.ToString("MMMM", culture)} {End.Year}";
    }

    public string DeadlineDisplay(CultureInfo culture) =>
        RegistrationDeadline.ToString("MMMM d, yyyy", culture);

    /// <summary>UTC midnight of the sitting's first day — the countdown target (ISO 8601).</summary>
    public string StartIsoUtc => Start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)
        .ToString("yyyy-MM-ddTHH:mm:ssZ");
}

public static class NppeExamCalendar
{
    public static readonly IReadOnlyList<NppeSitting> Sittings = new[]
    {
        new NppeSitting(new(2026, 11, 2),  new(2026, 11, 4),  new(2026, 9, 18)),
        new NppeSitting(new(2027, 1, 11),  new(2027, 1, 13),  new(2026, 11, 27)),
        new NppeSitting(new(2027, 3, 22),  new(2027, 3, 24),  new(2027, 2, 12)),
        new NppeSitting(new(2027, 6, 7),   new(2027, 6, 9),   new(2027, 4, 23)),
        new NppeSitting(new(2027, 8, 23),  new(2027, 8, 25),  new(2027, 7, 2)),
        new NppeSitting(new(2027, 11, 1),  new(2027, 11, 3),  new(2027, 9, 24)),
    };

    /// <summary>The soonest upcoming exam (kept until its window ends) — the countdown target.</summary>
    public static NppeSitting? NextExam(DateOnly today) =>
        Sittings.FirstOrDefault(s => s.End >= today);

    /// <summary>The soonest sitting a visitor can still register for (deadline not yet past).</summary>
    public static NppeSitting? NextOpenRegistration(DateOnly today) =>
        Sittings.FirstOrDefault(s => s.RegistrationDeadline >= today);
}
