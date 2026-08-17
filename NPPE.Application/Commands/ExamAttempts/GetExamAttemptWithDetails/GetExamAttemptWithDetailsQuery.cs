using MediatR;
using NPPE.Application.DTOs.ExamAttempts;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Application.Commands.ExamAttempts.GetExamAttemptWithDetails;
public record GetExamAttemptWithDetailsQuery(Guid Id) : IRequest<ExamAttemptDetailsDto>;

public class GetExamAttemptWithDetailsQueryHandler : IRequestHandler<GetExamAttemptWithDetailsQuery, ExamAttemptDetailsDto?>
{
    private readonly IExamAttemptRepository _examAttemptRepository;
    public GetExamAttemptWithDetailsQueryHandler(IExamAttemptRepository examAttemptRepository)
    {
        _examAttemptRepository = examAttemptRepository;
    }

    public async Task<ExamAttemptDetailsDto?> Handle(GetExamAttemptWithDetailsQuery request, CancellationToken ct)
    {
        var attempt = await _examAttemptRepository.GetAttemptWithDetailsAsync(request.Id);
        if (attempt == null)
            return null;

        var questions = new List<QuestionResultDto>();

        foreach (var answered in attempt.Answers.OrderBy(a => a.Id))
        {
            // Render history defensively: the question or options an old attempt
            // referenced may have since been removed, so fall back to placeholders
            // instead of throwing.
            var question = attempt.Exam.Questions.FirstOrDefault(q => q.Id == answered.QuestionId)
                ?? new Question { Text = "(This question is no longer available.)" };
            var selectedOption = question.Options.FirstOrDefault(o => o.Id == answered.SelectedOptionId)
                ?? new AnswerOption { Label = '?', Text = "(no longer available)" };
            var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect)
                ?? new AnswerOption { Label = '?', Text = "(no longer available)" };

            questions.Add(new QuestionResultDto
            {
                QuestionText = question.Text,
                SelectedLabel = selectedOption.Label,
                SelectedText = selectedOption.Text,
                CorrectLabel = correctOption.Label,
                CorrectText = correctOption.Text,
                IsCorrect = answered.IsCorrect,
                FeedbackMessage = answered.IsCorrect
                    ? $"That’s right, the correct answer is {correctOption.Label} “{correctOption.Text}.”\n{question.ExplanationForCorrect}"
                    : $"Wrong! You selected {selectedOption.Label} “{selectedOption.Text}” but the correct answer is {correctOption.Label} “{correctOption.Text}.”\n{question.ExplanationForIncorrect}"
            });
        }

        return new ExamAttemptDetailsDto
        {
            Id = attempt.Id,
            ExamId = attempt.ExamId,
            ExamTitle = attempt.Exam.Title,
            StudentId = attempt.UserId,
            Score = attempt.Score,
            TotalQuestions = attempt.TotalQuestions,
            Questions = questions
        };
    }
}