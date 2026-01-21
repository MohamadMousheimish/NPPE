using MediatR;
using NPPE.Application.DTOs.Exams;
using NPPE.Application.Repositories;

namespace NPPE.Application.Queries.Exams.GetAllExams;
public record GetAllExamsQuery : IRequest<List<ExamDto>>;

public class GetAllExamsQueryHandler : IRequestHandler<GetAllExamsQuery, List<ExamDto>>
{
    private readonly IExamRepository _examRepository;

    public GetAllExamsQueryHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<List<ExamDto>> Handle(GetAllExamsQuery request, CancellationToken ct)
    {
        var exams = await _examRepository.GetAllAsync();
        return exams.Select(e => new ExamDto
        {
            Id = e.Id,
            Title = e.Title,
            Description = e.Description,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt
        }).ToList();
    }
}