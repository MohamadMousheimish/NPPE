using MediatR;
using NPPE.Application.DTOs.Questions;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Application.Commands.Questions.UpdateQuestion;
public record UpdateQuestionCommand(Guid Id, string Text, string ExplanationForCorrect,
    string ExplanationForIncorrect, List<AnswerOptionDto> Options) : IRequest;


public class UpdateQuestionCommandHandler : IRequestHandler<UpdateQuestionCommand>
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IAnswerOptionRepository _answerOptionRepository;

    public UpdateQuestionCommandHandler(IQuestionRepository questionRepository,
        IAnswerOptionRepository answerOptionRepository)
    {
        _questionRepository = questionRepository;
        _answerOptionRepository = answerOptionRepository;
    }

    public async Task Handle(UpdateQuestionCommand request, CancellationToken ct)
    {
        var correctCount = request.Options.Count(o => o.IsCorrect);
        if (correctCount != 1)
            throw new ArgumentException("Exactly one answer must be correct.");

        if (request.Options.Count != 4)
            throw new ArgumentException("Exactly 4 answer options are required.");

        var question = await _questionRepository.GetQuestionWithOptionsAsync(request.Id);
        if (question == null)
            throw new InvalidOperationException("Question not found.");

        // Update question
        question.Text = request.Text;
        question.ExplanationForCorrect = request.ExplanationForCorrect;
        question.ExplanationForIncorrect = request.ExplanationForIncorrect;

        // Fetch current option IDs for this question
        var currentOptions = await _answerOptionRepository.GetOptionsByQuestionIdAsync(request.Id);
        var currentOptionIds = currentOptions.Select(o => o.Id).ToHashSet();

        // Validate: all provided IDs must belong to this question
        foreach (var optDto in request.Options)
        {
            if (optDto.Id.HasValue && optDto.Id != Guid.Empty)
            {
                if (!currentOptionIds.Contains(optDto.Id.Value))
                    throw new ArgumentException($"Option ID {optDto.Id} does not belong to this question.");
            }
        }

        // Update question
        question.Text = request.Text;
        question.ExplanationForCorrect = request.ExplanationForCorrect;
        question.ExplanationForIncorrect = request.ExplanationForIncorrect;
        await _questionRepository.UpdateAsync(question);

        // Determine actions
        var requestOptionIds = request.Options
            .Where(o => o.Id.HasValue && o.Id != Guid.Empty)
            .Select(o => o.Id!.Value)
            .ToHashSet();

        // 1. Delete removed options
        var optionsToDelete = currentOptions
            .Where(o => !requestOptionIds.Contains(o.Id))
            .ToList();
        foreach (var opt in optionsToDelete)
        {
            await _answerOptionRepository.DeleteAsync(opt);
        }

        // 2. Update existing options
        foreach (var optDto in request.Options)
        {
            if (optDto.Id.HasValue && optDto.Id != Guid.Empty)
            {
                var existingOpt = currentOptions.First(o => o.Id == optDto.Id.Value);
                existingOpt.Text = optDto.Text;
                existingOpt.Label = optDto.Label;
                existingOpt.IsCorrect = optDto.IsCorrect;
                await _answerOptionRepository.UpdateAsync(existingOpt);
            }
        }

        // 3. Add new options
        var newOptions = request.Options
            .Where(o => !o.Id.HasValue || o.Id == Guid.Empty)
            .ToList();
        foreach (var optDto in newOptions)
        {
            var newOpt = new AnswerOption
            {
                QuestionId = request.Id,
                Text = optDto.Text,
                Label = optDto.Label,
                IsCorrect = optDto.IsCorrect
            };
            await _answerOptionRepository.AddAsync(newOpt);
        }

        await _questionRepository.UpdateAsync(question);
    }
}