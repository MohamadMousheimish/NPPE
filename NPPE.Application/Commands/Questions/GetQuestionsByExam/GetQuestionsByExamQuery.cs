using MediatR;
using NPPE.Application.DTOs.Questions;
using NPPE.Application.Repositories;

namespace NPPE.Application.Commands.Questions.GetQuestionsByExam;
public record GetQuestionsByExamQuery(Guid ExamId) : IRequest<List<QuestionDto>>;

public class GetQuestionsByExamQueryHandler : IRequestHandler<GetQuestionsByExamQuery, List<QuestionDto>>
{
    private readonly IQuestionRepository _questionRepository;

    public GetQuestionsByExamQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<List<QuestionDto>> Handle(GetQuestionsByExamQuery request, CancellationToken ct)
    {
        var questions = await _questionRepository.GetQuestionsByExamIdAsync(request.ExamId);
        return questions.Select(q => new QuestionDto
        (
            q.Id,
            q.ExamId,
            q.Text,
            q.ExplanationForCorrect,
            q.ExplanationForIncorrect,
            q.Options.Select(o => new AnswerOptionDto
            (
                o.Id,
                o.Text,
                o.Label,
                o.IsCorrect
            )).OrderBy(o => o.Label).ToList()
        )).ToList();
    }
}