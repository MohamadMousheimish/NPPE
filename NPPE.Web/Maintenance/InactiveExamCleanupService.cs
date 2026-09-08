using NPPE.Application.Services;

namespace NPPE.Web.Maintenance;

/// <summary>
/// Runs once shortly after startup and then daily, purging exams that have been
/// inactive beyond the retention window (default 14 days) and never attempted.
/// Retention is configurable via <c>Maintenance:InactiveExamRetentionDays</c>.
/// </summary>
public class InactiveExamCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<InactiveExamCleanupService> _logger;

    public InactiveExamCleanupService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<InactiveExamCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var retentionDays = _config.GetValue<int?>("Maintenance:InactiveExamRetentionDays") ?? 14;

        // Let startup (including DB migration) settle before the first run.
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); }
        catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var cleaner = scope.ServiceProvider.GetRequiredService<IInactiveExamCleaner>();
                await cleaner.RemoveExpiredAsync(DateTime.UtcNow, retentionDays, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inactive-exam cleanup run failed; will retry on the next cycle.");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}
