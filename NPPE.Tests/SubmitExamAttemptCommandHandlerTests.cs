using Moq;
using NPPE.Application.Commands.ExamAttempts.SubmitExamAttempt;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;
using Xunit;

namespace NPPE.Tests;

/// <summary>
/// Scoring is the core of the product, so it gets its own focused suite:
/// order-independence, partial scores, and the guard rails (missing exam,
/// unanswered questions).
/// </summary>
public class SubmitExamAttemptCommandHandlerTests
{
    private readonly Mock<IExamRepository> _exams = new();
    private readonly Mock<IGenericRepository<ExamAttempt>> _attempts = new();
    private readonly Mock<IGenericRepository<AttemptedAnswer>> _answers = new();
    private ExamAttempt? _saved;

    public SubmitExamAttemptCommandHandlerTests()
    {
        _attempts.Setup(r => r.AddAsync(It.IsAny<ExamAttempt>()))
            .Callback<ExamAttempt>(a => _saved = a)
            .Returns(Task.CompletedTask);
    }

    private SubmitExamAttemptCommandHandler CreateHandler() =>
        new(_exams.Object, _attempts.Object, _answers.Object);

    /// <summary>Builds an exam whose questions each have 4 options; option index 0 is correct.</summary>
    private static Exam BuildExam(int questions)
    {
        var exam = new Exam { Title = "Scoring", IsActive = true };
        for (var i = 0; i < questions; i++)
        {
            var q = new Question { ExamId = exam.Id, Text = $"Q{i + 1}", IsActive = true };
            for (var o = 0; o < 4; o++)
                q.Options.Add(new AnswerOption { Text = $"o{o}", Label = (char)('A' + o), IsCorrect = o == 0 });
            exam.Questions.Add(q);
        }
        return exam;
    }

    private static Guid CorrectOption(Question q) => q.Options.First(o => o.IsCorrect).Id;
    private static Guid WrongOption(Question q) => q.Options.First(o => !o.IsCorrect).Id;

    [Fact]
    public async Task All_correct_answers_score_full_marks()
    {
        var exam = BuildExam(3);
        _exams.Setup(r => r.GetExamWithQuestionsAsync(exam.Id)).ReturnsAsync(exam);
        var answers = exam.Questions.ToDictionary(q => q.Id, CorrectOption);

        await CreateHandler().Handle(new SubmitExamAttemptCommand("u1", exam.Id, answers), default);

        Assert.NotNull(_saved);
        Assert.Equal(3, _saved!.Score);
        Assert.Equal(3, _saved.TotalQuestions);
        Assert.Equal("u1", _saved.UserId);
        Assert.All(_saved.Answers, a => Assert.True(a.IsCorrect));
    }

    [Fact]
    public async Task Partial_answers_score_only_the_correct_ones()
    {
        var exam = BuildExam(4);
        _exams.Setup(r => r.GetExamWithQuestionsAsync(exam.Id)).ReturnsAsync(exam);
        var qs = exam.Questions.ToList();
        var answers = new Dictionary<Guid, Guid>
        {
            [qs[0].Id] = CorrectOption(qs[0]),
            [qs[1].Id] = WrongOption(qs[1]),
            [qs[2].Id] = CorrectOption(qs[2]),
            [qs[3].Id] = WrongOption(qs[3]),
        };

        await CreateHandler().Handle(new SubmitExamAttemptCommand("u1", exam.Id, answers), default);

        Assert.Equal(2, _saved!.Score);
        Assert.Equal(4, _saved.TotalQuestions);
    }

    [Fact]
    public async Task Scoring_is_independent_of_answer_dictionary_order()
    {
        var exam = BuildExam(3);
        _exams.Setup(r => r.GetExamWithQuestionsAsync(exam.Id)).ReturnsAsync(exam);
        // Feed the answers in reverse question order — must still score each against its own question.
        var answers = exam.Questions.Reverse().ToDictionary(q => q.Id, CorrectOption);

        await CreateHandler().Handle(new SubmitExamAttemptCommand("u1", exam.Id, answers), default);

        Assert.Equal(3, _saved!.Score);
    }

    [Fact]
    public async Task Missing_exam_throws()
    {
        _exams.Setup(r => r.GetExamWithQuestionsAsync(It.IsAny<Guid>())).ReturnsAsync((Exam?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateHandler().Handle(new SubmitExamAttemptCommand("u1", Guid.NewGuid(),
                new Dictionary<Guid, Guid>()), default));
    }

    [Fact]
    public async Task Unanswered_questions_are_rejected()
    {
        var exam = BuildExam(3);
        _exams.Setup(r => r.GetExamWithQuestionsAsync(exam.Id)).ReturnsAsync(exam);
        // Only answer the first question.
        var answers = new Dictionary<Guid, Guid> { [exam.Questions.First().Id] = CorrectOption(exam.Questions.First()) };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            CreateHandler().Handle(new SubmitExamAttemptCommand("u1", exam.Id, answers), default));

        _attempts.Verify(r => r.AddAsync(It.IsAny<ExamAttempt>()), Times.Never);
    }
}
