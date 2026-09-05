namespace NPPE.Application.Services;

/// <summary>
/// Grants the exam-pack purchased in a Stripe Checkout session: unlocks the latest N
/// active exams for the buyer, exactly once (safe to call from both the success page and
/// the webhook for the same session).
/// </summary>
public interface IExamPackGranter
{
    /// <summary>Returns true only for the call that actually performed the grant.</summary>
    Task<bool> GrantAsync(string stripeSessionId, string? customerCountry = null);
}
