using MediatR;
using NPPE.Application.DTOs.ExamAttempts;
using NPPE.Application.Repositories;

namespace NPPE.Application.Commands.ExamAttempts.GetStudentExamHistory;
public record GetStudentExamHistoryQuery(string UserId) : IRequest<List<ExamAttemptSummaryDto>>;

public class GetStudentExamHistoryQueryHandler : IRequestHandler<GetStudentExamHistoryQuery, List<ExamAttemptSummaryDto>>
{
    private readonly IExamAttemptRepository _examAttemptRepository;

    public GetStudentExamHistoryQueryHandler(IExamAttemptRepository examAttemptRepository)
    {
        _examAttemptRepository = examAttemptRepository;
    }

    public async Task<List<ExamAttemptSummaryDto>> Handle(GetStudentExamHistoryQuery request, CancellationToken ct)
    {
        var attempts = await _examAttemptRepository.GetAttemptsByUserIdAsync(request.UserId);
        return attempts.Select(a => new ExamAttemptSummaryDto
        {
            Id = a.Id,
            ExamTitle = a.Exam.Title,
            Score = a.Score,
            TotalQuestions = a.TotalQuestions,
            TakenAt = a.TakenAt
        }).ToList();
    }
}