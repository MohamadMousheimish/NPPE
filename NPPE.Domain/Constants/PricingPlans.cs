namespace NPPE.Domain.Constants;

/// <summary>
/// A one-time exam package. Buying a pack unlocks the latest <see cref="ExamCount"/>
/// active exams for the student, each attemptable up to <see cref="PricingPlans.AttemptsPerExam"/> times.
/// </summary>
public sealed record ExamPack(string Id, int ExamCount, long PriceCents, string Name, string Blurb);

public static class PricingPlans
{
    public const string Currency = "cad";

    /// <summary>Attempts allowed per unlocked exam.</summary>
    public const int AttemptsPerExam = 2;

    /// <summary>The exam packs offered on the pricing page (order = display order).</summary>
    public static readonly IReadOnlyList<ExamPack> Packs = new[]
    {
        new ExamPack("pack2", 2, 8000,  "2-Exam Pack",
            "Unlock 2 full practice exams — 2 attempts each — plus all study materials."),
        new ExamPack("pack3", 3, 10000, "3-Exam Pack",
            "Unlock 3 full practice exams — 2 attempts each — plus all study materials. Best value."),
    };

    public static ExamPack? GetPack(string id) => Packs.FirstOrDefault(p => p.Id == id);

    public static string PackProductName(ExamPack pack) => $"NPPE Prep — {pack.Name}";
    public static string PackProductDescription(ExamPack pack) =>
        $"Unlocks {pack.ExamCount} practice exams ({AttemptsPerExam} attempts each) and full study materials.";

    // ── Legacy (retired subscription / one-time-lifetime). Kept only so the old,
    //    now-unused checkout commands still compile; removed when those are deleted. ──
    public const decimal OneTimePrice = 29.00m;
    public const long OneTimePriceCents = 2900;
    public const decimal MonthlyPrice = 9.99m;
    public const long MonthlyPriceCents = 999;
    public const string OneTimeProductName = "NPPE Exam Prep Full Access";
    public const string OneTimeProductDescription = "One-time payment for unlimited exam attempts";
    public const string MonthlyProductName = "NPPE Exam Prep Monthly";
    public const string MonthlyProductDescription = "Monthly subscription for unlimited exam attempts";
}
