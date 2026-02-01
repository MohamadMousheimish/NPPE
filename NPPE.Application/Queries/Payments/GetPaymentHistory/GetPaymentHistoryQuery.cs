using MediatR;
using NPPE.Application.DTOs.Payments;
using NPPE.Application.Repositories;

namespace NPPE.Application.Queries.Payments.GetPaymentHistory;

public record GetPaymentHistoryQuery(string UserId) : IRequest<List<PaymentHistoryDto>>;

public class GetPaymentHistoryQueryHandler
    : IRequestHandler<GetPaymentHistoryQuery, List<PaymentHistoryDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentHistoryQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<List<PaymentHistoryDto>> Handle(
        GetPaymentHistoryQuery request, CancellationToken ct)
    {
        var payments = await _paymentRepository.GetPaymentsByUserIdAsync(request.UserId);
        return payments.Select(p => new PaymentHistoryDto
        {
            Id = p.Id,
            Amount = p.Amount,
            Currency = p.Currency.ToUpper(),
            Status = p.Status.ToString(),
            PaymentType = p.PaymentType.ToString(),
            PaidAt = p.PaidAt,
            CreatedAt = p.CreatedAt,
            SubscriptionStatus = p.SubscriptionStatus?.ToString()
        }).ToList();
    }
}
