namespace NPPE.Application.Repositories;

/// <summary>
/// Runs a set of persistence operations inside a single database transaction so
/// they either all commit or all roll back together.
/// </summary>
public interface IUnitOfWork
{
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
}
