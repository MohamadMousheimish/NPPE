using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPPE.Application.Services;
using NPPE.Infrastructure.Persistence;

namespace NPPE.Infrastructure.Maintenance;

public class InactiveExamCleaner : IInactiveExamCleaner
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<InactiveExamCleaner> _logger;

    public InactiveExamCleaner(ApplicationDbContext db, ILogger<InactiveExamCleaner> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<int> RemoveExpiredAsync(DateTime utcNow, int retentionDays, CancellationToken ct = default)
    {
        var cutoff = utcNow.AddDays(-Math.Max(1, retentionDays));

        // Inactive, deactivated before the cutoff (falling back to CreatedAt for rows
        // deactivated before this timestamp existed), and never attempted.
        var stale = await _db.Exams
            .Where(e => !e.IsActive
                        && (e.DeactivatedAt ?? e.CreatedAt) < cutoff
                        && !_db.ExamAttempts.Any(a => a.ExamId == e.Id))
            .ToListAsync(ct);

        if (stale.Count == 0) return 0;

        // FK cascade removes the questions, answer options and any unlocks.
        _db.Exams.RemoveRange(stale);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Inactive-exam cleanup removed {Count} exam(s) inactive for more than {Days} day(s).",
            stale.Count, retentionDays);
        return stale.Count;
    }
}
