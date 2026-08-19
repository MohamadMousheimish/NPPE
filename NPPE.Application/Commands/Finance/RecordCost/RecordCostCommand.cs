using MediatR;
using NPPE.Application.Repositories;
using NPPE.Domain.Constants;
using NPPE.Domain.Entities;
using NPPE.Domain.Enums;

namespace NPPE.Application.Commands.Finance.RecordCost;

public record RecordCostCommand(
    string Provider,
    CostCategory Category,
    decimal Amount,
    DateTime IncurredOn,
    bool IsRecurring,
    string? Note) : IRequest<Guid>;

public class RecordCostCommandHandler : IRequestHandler<RecordCostCommand, Guid>
{
    private readonly ICostRepository _costs;

    public RecordCostCommandHandler(ICostRepository costs) => _costs = costs;

    public async Task<Guid> Handle(RecordCostCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Provider))
            throw new ArgumentException("Provider is required.");
        if (request.Amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        var cost = new Cost
        {
            Provider = request.Provider.Trim(),
            Category = request.Category,
            Amount = request.Amount,
            Currency = Currencies.Canadian,
            IncurredOn = request.IncurredOn,
            IsRecurring = request.IsRecurring,
            Note = request.Note?.Trim(),
            Source = CostSource.Manual
        };

        await _costs.AddAsync(cost);
        return cost.Id;
    }
}
