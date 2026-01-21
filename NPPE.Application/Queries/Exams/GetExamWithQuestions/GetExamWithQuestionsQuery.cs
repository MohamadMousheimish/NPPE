using MediatR;
using NPPE.Application.DTOs.Exams;
using NPPE.Application.DTOs.Questions;
using NPPE.Application.Repositories;

namespace NPPE.Application.Queries.Exams.GetExamWithQuestions;
public record GetExamWithQuestionsQuery(Guid Id) : IRequest<ExamWithQuestionsDto?>;

public class GetExamWithQuestionsQueryHandler : IRequestHandler<GetExamWithQuestionsQuery, ExamWithQuestionsDto?>
{
    private readonly IExamRepository _examRepository;

    public GetExamWithQuestionsQueryHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<ExamWithQuestionsDto?> Handle(GetExamWithQuestionsQuery request, CancellationToken ct)
    {
        var exam = await _examRepository.GetExamWithQuestionsAsync(request.Id);
        if (exam == null || !exam.IsActive)
            return null;

        return new ExamWithQuestionsDto
        {
            Id = exam.Id,
            Title = exam.Title,
            Description = exam.Description,
            Questions = exam.Questions.Select(q => new QuestionDto
            (
                q.Id,
                q.ExamId,
                q.Text, q.ExplanationForCorrect, q.ExplanationForIncorrect,
                q.Options.OrderBy(o => o.Label).Select(o => new AnswerOptionDto
                (
                    o.Id,
                    o.Text,
                    o.Label,
                    o.IsCorrect
                )).ToList()
            )).ToList()
        };
    }
}