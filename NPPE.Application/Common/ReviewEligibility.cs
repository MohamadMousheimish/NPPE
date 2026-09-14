using NPPE.Application.Repositories;

namespace NPPE.Application.Common;

/// <summary>
/// A student may leave a review only after finishing ALL of the exams they've unlocked
/// (every active exam in their pack has at least one attempt). Reviews are one-per-student
/// (upsert), never per-exam.
/// </summary>
public static class ReviewEligibility
{
    public static async Task<bool> HasFinishedAllExamsAsync(
        string userId, IExamUnlockRepository unlocks, IExamAttemptRepository attempts)
    {
        var unlocked = (await unlocks.GetForUserAsync(userId))
            .Where(u => u.Exam != null && u.Exam.IsActive)
            .ToList();
        if (unlocked.Count == 0) return false; // nothing unlocked yet

        var attemptedExamIds = (await attempts.GetAttemptsByUserIdAsync(userId))
            .Select(a => a.ExamId)
            .ToHashSet();

        return unlocked.All(u => attemptedExamIds.Contains(u.ExamId));
    }
}
