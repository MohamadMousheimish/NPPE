using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NPPE.Infrastructure.Documents;
using Xunit;

namespace NPPE.Tests;

public class ExamDocumentParserTests
{
    private readonly ExamDocumentParser _parser = new();

    /// <summary>Builds a minimal in-memory .docx from the given paragraph texts.</summary>
    private static Stream BuildDocx(params string[] paragraphs)
    {
        var ms = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document, true))
        {
            var main = doc.AddMainDocumentPart();
            main.Document = new Document();
            var body = main.Document.AppendChild(new Body());
            foreach (var p in paragraphs)
                body.AppendChild(new Paragraph(new Run(new Text(p) { Space = SpaceProcessingModeValues.Preserve })));
            main.Document.Save();
        }
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void Parses_a_well_formed_lettered_question()
    {
        using var docx = BuildDocx(
            "What is the primary duty of a Professional Engineer?",
            "A. Protect the employer's interests",
            "B. Hold public safety paramount",
            "C. Maximize the project's profit",
            "D. Follow every instruction without question",
            "That's right, the correct answer is B \"Hold public safety paramount\".",
            "The engineer's paramount duty is to public welfare and safety.",
            "Wrong! You think the answer is A, but the correct answer is B.");

        var result = _parser.Parse(docx);

        Assert.True(result.Recognized);
        Assert.Null(result.Error);
        var q = Assert.Single(result.Questions);
        Assert.Equal("What is the primary duty of a Professional Engineer?", q.Text);

        Assert.Equal(4, q.Options.Count);
        Assert.Equal(new[] { 'A', 'B', 'C', 'D' }, q.Options.Select(o => o.Label));
        Assert.Equal("Hold public safety paramount", q.Options[1].Text); // letter prefix stripped

        // The correct letter is trusted from the "correct answer is B" line.
        Assert.True(q.Options[1].IsCorrect);
        Assert.Equal(1, q.Options.Count(o => o.IsCorrect));

        Assert.Contains("public welfare", q.ExplanationForCorrect);
        Assert.Empty(q.Notes);
    }

    [Fact]
    public void Assigns_labels_by_order_when_options_are_unlettered()
    {
        using var docx = BuildDocx(
            "Which designation may a recent graduate use?",
            "First option text",
            "Second option text",
            "Third option text",
            "Fourth option text",
            "That's right, the correct answer is D \"Fourth option text\".",
            "Reasoning for the fourth option.",
            "Wrong! Not that one.");

        var result = _parser.Parse(docx);

        var q = Assert.Single(result.Questions);
        Assert.Equal(new[] { 'A', 'B', 'C', 'D' }, q.Options.Select(o => o.Label));
        Assert.Equal("Fourth option text", q.Options[3].Text); // kept verbatim, no prefix to strip
        Assert.True(q.Options[3].IsCorrect);
        Assert.Contains(q.Notes, n => n.Contains("assigned by order"));
    }

    [Fact]
    public void Auto_fills_a_placeholder_when_no_explanation_is_present()
    {
        using var docx = BuildDocx(
            "A question with no reasoning text at all?",
            "A. First",
            "B. Second",
            "C. Third",
            "D. Fourth",
            "That's right, the correct answer is C \"Third\".",
            "Wrong! Nope.");

        var result = _parser.Parse(docx);

        var q = Assert.Single(result.Questions);
        Assert.True(q.Options[2].IsCorrect);
        Assert.Equal("No explanation provided.", q.ExplanationForCorrect);
        Assert.Contains(q.Notes, n => n.Contains("placeholder"));
    }

    [Fact]
    public void Rejects_a_document_with_no_recognizable_questions()
    {
        using var docx = BuildDocx(
            "This is just some prose.",
            "It has no answer key lines whatsoever.",
            "Nothing to anchor on here.");

        var result = _parser.Parse(docx);

        Assert.False(result.Recognized);
        Assert.False(string.IsNullOrEmpty(result.Error));
        Assert.Empty(result.Questions);
    }

    [Fact]
    public void Parses_multiple_questions_in_one_document()
    {
        using var docx = BuildDocx(
            "Question one stem?",
            "A. One A", "B. One B", "C. One C", "D. One D",
            "That's right, the correct answer is A \"One A\".",
            "Reasoning one.",
            "Wrong! Not this.",
            "Question two stem?",
            "A. Two A", "B. Two B", "C. Two C", "D. Two D",
            "That's right, the correct answer is C \"Two C\".",
            "Reasoning two.",
            "Wrong! Not that.");

        var result = _parser.Parse(docx);

        Assert.True(result.Recognized);
        Assert.Equal(2, result.Questions.Count);
        Assert.True(result.Questions[0].Options[0].IsCorrect); // A
        Assert.True(result.Questions[1].Options[2].IsCorrect); // C
        Assert.Equal("Question two stem?", result.Questions[1].Text);
    }
}
