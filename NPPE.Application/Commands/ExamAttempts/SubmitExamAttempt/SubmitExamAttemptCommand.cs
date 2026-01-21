using MediatR;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Application.Commands.ExamAttempts.SubmitExamAttempt;
public record SubmitExamAttemptCommand(string StudentId, Guid ExamId, List<Guid> SelectedOptionsIds) : IRequest<Guid>;

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

        if (request.SelectedOptionsIds.Count != exam.Questions.Count)
            throw new ArgumentException("Answer count does not match question count.");

        // Calculate score
        var total = exam.Questions.Count;
        var correct = 0;
        var attemptedAnswers = new List<AttemptedAnswer>();
        var questions = exam.Questions.ToList();

        for (int i = 0; i < exam.Questions.Count; i++)
        {
            var question = questions[i];
            var selectedOptionId = request.SelectedOptionsIds[i];
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
            Answers = attemptedAnswers
        };

        await _attemptRepository.AddAsync(attempt);
        return attempt.Id;
    }
}