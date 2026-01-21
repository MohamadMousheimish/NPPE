using MediatR;
using NPPE.Application.DTOs.Exams;
using NPPE.Application.Repositories;

namespace NPPE.Application.Queries.Exams.GetExamById;
public record GetExamByIdQuery(Guid Id) : IRequest<ExamDto?>;

public class GetExamByIdQueryHandler : IRequestHandler<GetExamByIdQuery, ExamDto?>
{
    private readonly IExamRepository _examRepository;

    public GetExamByIdQueryHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<ExamDto?> Handle(GetExamByIdQuery request, CancellationToken ct)
    {
        var exam = await _examRepository.GetByIdAsync(request.Id);
        if (exam is null) return null;

        return new ExamDto
        {
            Id = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            IsActive = exam.IsActive,
            CreatedAt = exam.CreatedAt
        };
    }
}
