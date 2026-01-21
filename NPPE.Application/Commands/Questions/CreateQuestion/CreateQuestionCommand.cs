using MediatR;
using NPPE.Application.DTOs.Questions;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Application.Commands.Questions.CreateQuestion;
public record CreateQuestionCommand(Guid ExamId, string Text, string ExplanationForCorrect,
    string ExplanationForIncorrect, List<AnswerOptionDto> Options) : IRequest<Guid>;


public class CreateQuestionCommandHandler : IRequestHandler<CreateQuestionCommand, Guid>
{
    private readonly IQuestionRepository _questionRepository;

    public CreateQuestionCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task<Guid> Handle(CreateQuestionCommand request, CancellationToken ct)
    {
        // Validate: exactly one correct answer
        var correctCount = request.Options.Count(o => o.IsCorrect);
        if (correctCount != 1)
            throw new ArgumentException("Exactly one answer must be marked as correct.");

        var question = new Question
        {
            Text = request.Text,
            ExamId = request.ExamId,
            ExplanationForCorrect = request.ExplanationForCorrect,
            ExplanationForIncorrect = request.ExplanationForIncorrect
        };

        foreach (var opt in request.Options)
        {
            question.Options.Add(new AnswerOption
            {
                Text = opt.Text,
                Label = opt.Label,
                IsCorrect = opt.IsCorrect
            });
        }

        await _questionRepository.AddAsync(question);
        return question.Id;
    }
}