using Moq;
using NPPE.Application.Commands.Exams.CreateExamFromImport;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;
using Xunit;

namespace NPPE.Tests;

public class CreateExamFromImportCommandHandlerTests
{
    private readonly Mock<IExamRepository> _exams = new();
    private readonly Mock<IQuestionRepository> _questions = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    public CreateExamFromImportCommandHandlerTests()
    {
        // Run the transactional body inline so repository calls actually execute.
        _uow.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<CancellationToken>()))
            .Returns<Func<Task>, CancellationToken>((action, _) => action());
    }

    private CreateExamFromImportCommandHandler CreateHandler() =>
        new(_exams.Object, _questions.Object, _uow.Object);

    private static List<ImportedOption> ValidOptions(int correctIndex = 1) =>
        Enumerable.Range(0, 4)
            .Select(i => new ImportedOption($"Option {(char)('A' + i)}", (char)('A' + i), i == correctIndex))
            .ToList();

    private static ImportedQuestion ValidQuestion() =>
        new("A valid stem?", "Because.", "Because not.", ValidOptions());

    [Fact]
    public async Task Creates_exam_and_questions_for_valid_input()
    {
        var cmd = new CreateExamFromImportCommand("My Exam", "Desc", true,
            new List<ImportedQuestion> { ValidQuestion(), ValidQuestion() });

        var id = await CreateHandler().Handle(cmd, default);

        Assert.NotEqual(Guid.Empty, id);
        _exams.Verify(r => r.AddAsync(It.Is<Exam>(e => e.Title == "My Exam" && e.IsActive)), Times.Once);
        _questions.Verify(r => r.AddAsync(It.IsAny<Question>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Trims_title_and_maps_options()
    {
        Exam? captured = null;
        _exams.Setup(r => r.AddAsync(It.IsAny<Exam>())).Callback<Exam>(e => captured = e);

        var cmd = new CreateExamFromImportCommand("  Spaced  ", "  d  ", false,
            new List<ImportedQuestion> { ValidQuestion() });

        await CreateHandler().Handle(cmd, default);

        Assert.NotNull(captured);
        Assert.Equal("Spaced", captured!.Title);
        Assert.False(captured.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Rejects_missing_title(string title)
    {
        var cmd = new CreateExamFromImportCommand(title, "d", true,
            new List<ImportedQuestion> { ValidQuestion() });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => CreateHandler().Handle(cmd, default));
        Assert.Contains("title", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rejects_exam_with_no_questions()
    {
        var cmd = new CreateExamFromImportCommand("T", "d", true, new List<ImportedQuestion>());

        await Assert.ThrowsAsync<ArgumentException>(() => CreateHandler().Handle(cmd, default));
    }

    [Fact]
    public async Task Rejects_question_without_exactly_four_options()
    {
        var threeOptions = ValidOptions().Take(3).ToList();
        var cmd = new CreateExamFromImportCommand("T", "d", true,
            new List<ImportedQuestion> { new("stem?", "a", "b", threeOptions) });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => CreateHandler().Handle(cmd, default));
        Assert.Contains("Question 1", ex.Message);
    }

    [Fact]
    public async Task Rejects_question_with_no_correct_option()
    {
        var noneCorrect = ValidOptions().Select(o => o with { IsCorrect = false }).ToList();
        var cmd = new CreateExamFromImportCommand("T", "d", true,
            new List<ImportedQuestion> { new("stem?", "a", "b", noneCorrect) });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => CreateHandler().Handle(cmd, default));
        Assert.Contains("exactly one option", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rejects_question_with_multiple_correct_options()
    {
        var twoCorrect = ValidOptions();
        twoCorrect[0] = twoCorrect[0] with { IsCorrect = true }; // now index 0 and 1 are correct
        var cmd = new CreateExamFromImportCommand("T", "d", true,
            new List<ImportedQuestion> { new("stem?", "a", "b", twoCorrect) });

        await Assert.ThrowsAsync<ArgumentException>(() => CreateHandler().Handle(cmd, default));
    }

    [Fact]
    public async Task Rejects_question_with_an_empty_option()
    {
        var withBlank = ValidOptions();
        withBlank[2] = withBlank[2] with { Text = "   " };
        var cmd = new CreateExamFromImportCommand("T", "d", true,
            new List<ImportedQuestion> { new("stem?", "a", "b", withBlank) });

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => CreateHandler().Handle(cmd, default));
        Assert.Contains("every option", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Does_not_create_anything_when_validation_fails()
    {
        var cmd = new CreateExamFromImportCommand("", "d", true,
            new List<ImportedQuestion> { ValidQuestion() });

        await Assert.ThrowsAsync<ArgumentException>(() => CreateHandler().Handle(cmd, default));

        _exams.Verify(r => r.AddAsync(It.IsAny<Exam>()), Times.Never);
        _questions.Verify(r => r.AddAsync(It.IsAny<Question>()), Times.Never);
    }
}
