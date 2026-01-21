using MediatR;
using NPPE.Application.DTOs.Questions;
using NPPE.Application.Repositories;

namespace NPPE.Application.Queries.Questions.GetQuestionById;
public record GetQuestionByIdQuery(Guid Id) : IRequest<QuestionDto?>;

public class GetQuestionByIdQueryHandler : IRequestHandler<GetQuestionByIdQuery, QuestionDto?>
{
    private readonly IQuestionRepository _questionRepository;

    public GetQuestionByIdQueryHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<QuestionDto?> Handle(GetQuestionByIdQuery request, CancellationToken ct)
    {
        var question = await _questionRepository.GetQuestionWithOptionsAsync(request.Id);
        if (question == null)
            return null;

        return new QuestionDto
        (
            question.Id,
            question.ExamId,
            question.Text,
            question.ExplanationForCorrect,
            question.ExplanationForIncorrect,
            question.Options.Select(o => new AnswerOptionDto
            (
                o.Id,
                o.Text,
                o.Label,
                o.IsCorrect
            )).OrderBy(o => o.Label).ToList()
        );
    }
}