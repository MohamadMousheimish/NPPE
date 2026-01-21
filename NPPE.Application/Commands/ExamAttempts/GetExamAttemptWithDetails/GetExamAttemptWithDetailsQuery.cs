using MediatR;
using NPPE.Application.DTOs.ExamAttempts;
using NPPE.Application.Repositories;

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
            var question = attempt.Exam.Questions.First(q => q.Id == answered.QuestionId);
            var selectedOption = question.Options.First(o => o.Id == answered.SelectedOptionId);
            var correctOption = question.Options.First(o => o.IsCorrect);

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