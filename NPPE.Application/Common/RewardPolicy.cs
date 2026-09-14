using Microsoft.Extensions.Configuration;
using NPPE.Application.Repositories;
using NPPE.Domain.Enums;

namespace NPPE.Application.Common;

/// <summary>Reads the completion-reward configuration and computes the refund amount.
/// Config: Rewards:Enabled (default true), Rewards:RefundPercent (default 5).</summary>
public static class RewardPolicy
{
    public static bool IsEnabled(IConfiguration config) =>
        !bool.TryParse(config["Rewards:Enabled"], out var enabled) || enabled;

    public static int RefundPercent(IConfiguration config) =>
        int.TryParse(config["Rewards:RefundPercent"], out var pct) && pct is > 0 and <= 100 ? pct : 5;

    /// <summary>5% (by default) of the student's succeeded exam-pack payments, rounded to cents.</summary>
    public static async Task<decimal> ComputeRefundAsync(
        string userId, IPaymentRepository payments, IConfiguration config)
    {
        var paid = (await payments.GetPaymentsByUserIdAsync(userId))
            .Where(p => p.PaymentType == PaymentType.ExamPack && p.Status == PaymentStatus.Succeeded)
            .Sum(p => p.Amount);
        return Math.Round(paid * RefundPercent(config) / 100m, 2);
    }
}
