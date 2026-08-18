using MediatR;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Application.Commands.Exams.CreateExamFromImport;

public record CreateExamFromImportCommand(
    string Title,
    string Description,
    bool IsActive,
    List<ImportedQuestion> Questions) : IRequest<Guid>;

public record ImportedQuestion(
    string Text,
    string ExplanationForCorrect,
    string ExplanationForIncorrect,
    List<ImportedOption> Options);

public record ImportedOption(string Text, char Label, bool IsCorrect);

public class CreateExamFromImportCommandHandler : IRequestHandler<CreateExamFromImportCommand, Guid>
{
    private readonly IExamRepository _examRepository;
    private readonly IQuestionRepository _questionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateExamFromImportCommandHandler(
        IExamRepository examRepository,
        IQuestionRepository questionRepository,
        IUnitOfWork unitOfWork)
    {
        _examRepository = examRepository;
        _questionRepository = questionRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateExamFromImportCommand request, CancellationToken ct)
    {
        // Authoritative server-side validation (never trust the client's "no red flags").
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Exam title is required.");
        if (request.Questions.Count == 0)
            throw new ArgumentException("An imported exam must have at least one question.");

        for (int i = 0; i < request.Questions.Count; i++)
        {
            var q = request.Questions[i];
            var n = i + 1;
            if (string.IsNullOrWhiteSpace(q.Text))
                throw new ArgumentException($"Question {n}: the question text is empty.");
            if (q.Options.Count != 4)
                throw new ArgumentException($"Question {n}: exactly 4 options are required (found {q.Options.Count}).");
            if (q.Options.Any(o => string.IsNullOrWhiteSpace(o.Text)))
                throw new ArgumentException($"Question {n}: every option must have text.");
            if (q.Options.Count(o => o.IsCorrect) != 1)
                throw new ArgumentException($"Question {n}: exactly one option must be marked correct.");
        }

        var exam = new Exam
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            IsActive = request.IsActive
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _examRepository.AddAsync(exam);

            foreach (var q in request.Questions)
            {
                var question = new Question
                {
                    ExamId = exam.Id,
                    Text = q.Text.Trim(),
                    ExplanationForCorrect = q.ExplanationForCorrect ?? string.Empty,
                    ExplanationForIncorrect = q.ExplanationForIncorrect ?? string.Empty,
                    Options = q.Options.Select(o => new AnswerOption
                    {
                        Text = o.Text.Trim(),
                        Label = o.Label,
                        IsCorrect = o.IsCorrect
                    }).ToList()
                };
                await _questionRepository.AddAsync(question);
            }
        }, ct);

        return exam.Id;
    }
}
