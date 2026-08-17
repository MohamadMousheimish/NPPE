using MediatR;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Application.Commands.ExamAttempts.SubmitExamAttempt;

/// <summary>
/// Submits an exam attempt. <paramref name="Answers"/> maps each QuestionId to the
/// selected AnswerOptionId, so scoring does not depend on question ordering.
/// </summary>
public record SubmitExamAttemptCommand(string StudentId, Guid ExamId, IReadOnlyDictionary<Guid, Guid> Answers) : IRequest<Guid>;

public class SubmitExamAttemptCommandHandler : IRequestHandler<SubmitExamAttemptCommand, Guid>
{
    private readonly IExamRepository _examRepository;
    private readonly IGenericRepository<ExamAttempt> _attemptRepository;
    private readonly IGenericRepository<AttemptedAnswer> _answerRepository;

    public SubmitExamAttemptCommandHandler(
        IExamRepository examRepository,
        IGenericRepository<ExamAttempt> attemptRepository,
        IGenericRepository<AttemptedAnswer> answerRepository)
    {
        _examRepository = examRepository;
        _attemptRepository = attemptRepository;
        _answerRepository = answerRepository;
    }

    public async Task<Guid> Handle(SubmitExamAttemptCommand request, CancellationToken ct)
    {
        // Load exam with questions and options
        var exam = await _examRepository.GetExamWithQuestionsAsync(request.ExamId);
        if (exam == null)
            throw new InvalidOperationException("Exam not found.");

        // Every question must be answered exactly once.
        var unanswered = exam.Questions.Where(q => !request.Answers.ContainsKey(q.Id)).ToList();
        if (unanswered.Count > 0)
            throw new ArgumentException("All questions must be answered.");

        // Calculate score by matching each answer to its own question (order-independent).
        var total = exam.Questions.Count;
        var correct = 0;
        var attemptedAnswers = new List<AttemptedAnswer>();

        foreach (var question in exam.Questions)
        {
            var selectedOptionId = request.Answers[question.Id];
            var isCorrect = question.Options.Any(o => o.Id == selectedOptionId && o.IsCorrect);

            if (isCorrect) correct++;

            attemptedAnswers.Add(new AttemptedAnswer
            {
                QuestionId = question.Id,
                SelectedOptionId = selectedOptionId,
                IsCorrect = isCorrect
            });
        }

        // Save attempt
        var attempt = new ExamAttempt
        {
            UserId = request.StudentId,
            ExamId = request.ExamId,
            Score = correct,
            TotalQuestions = total,
            TakenAt = DateTime.UtcNow,
            Answers = attemptedAnswers
        };

        await _attemptRepository.AddAsync(attempt);
        return attempt.Id;
    }
}