namespace NPPE.Application.Services;

/// <summary>
/// Purges exams that have been soft-deleted (inactive) for longer than the retention
/// window and have never been attempted. Deleting an exam cascades to its questions,
/// options and unlocks. Exams with attempts are kept so student history is preserved.
/// </summary>
public interface IInactiveExamCleaner
{
    /// <summary>Removes eligible exams and returns how many were deleted.</summary>
    Task<int> RemoveExpiredAsync(DateTime utcNow, int retentionDays, CancellationToken ct = default);
}
