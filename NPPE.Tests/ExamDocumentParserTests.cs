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
    public void Parses_lettered_options_with_a_correct_answer_line()
    {
        using var docx = BuildDocx(
            "What is the primary duty of a Professional Engineer?",
            "A. protect the employer's interests",           // lowercase → should be capitalized
            "B. Hold public safety paramount",
            "C. Maximize the project's profit",
            "D. Follow every instruction without question",
            "Correct Answer: B — Hold public safety paramount",
            "Explanation",
            "The engineer's paramount duty is to public welfare and safety.");

        var result = _parser.Parse(docx);

        Assert.True(result.Recognized);
        Assert.Null(result.Error);
        var q = Assert.Single(result.Questions);
        Assert.Equal("What is the primary duty of a Professional Engineer?", q.Text);

        Assert.Equal(4, q.Options.Count);
        Assert.Equal(new[] { 'A', 'B', 'C', 'D' }, q.Options.Select(o => o.Label));
        Assert.Equal("Protect the employer's interests", q.Options[0].Text); // prefix stripped + capitalized
        Assert.Equal("Hold public safety paramount", q.Options[1].Text);

        Assert.True(q.Options[1].IsCorrect);
        Assert.Equal(1, q.Options.Count(o => o.IsCorrect));

        Assert.StartsWith("The engineer's paramount duty", q.ExplanationForCorrect); // "Explanation" header dropped
        Assert.Contains("public welfare", q.ExplanationForCorrect);
        Assert.Equal(q.ExplanationForCorrect, q.ExplanationForIncorrect);
        Assert.Empty(q.Notes);
    }

    [Fact]
    public void Splits_four_options_glued_into_one_paragraph()
    {
        using var docx = BuildDocx(
            "A contractor cites an earthquake to excuse performance. Which clause applies?",
            "A. Copyright clauseB. Fixed-price clauseC. Force majeure clauseD. Contract obligation clause",
            "Correct Answer: C — Force majeure clause",
            "A force majeure clause may excuse performance for events beyond a party's control.");

        var result = _parser.Parse(docx);

        var q = Assert.Single(result.Questions);
        Assert.Equal(4, q.Options.Count);
        Assert.Equal("Copyright clause", q.Options[0].Text);
        Assert.Equal("Fixed-price clause", q.Options[1].Text);
        Assert.Equal("Force majeure clause", q.Options[2].Text);
        Assert.Equal("Contract obligation clause", q.Options[3].Text);
        Assert.True(q.Options[2].IsCorrect);
        Assert.Empty(q.Notes);
    }

    [Fact]
    public void Parses_multiple_questions_and_bounds_each_explanation()
    {
        using var docx = BuildDocx(
            "Question one stem?",
            "A. one aB. one bC. one cD. one d",
            "Correct Answer: A",
            "Reasoning for question one.",
            "Question two stem?",
            "A. Two A", "B. Two B", "C. Two C", "D. Two D",
            "Correct Answer: C",
            "Reasoning for question two.");

        var result = _parser.Parse(docx);

        Assert.Equal(2, result.Questions.Count);
        Assert.Equal("Question one stem?", result.Questions[0].Text);
        Assert.True(result.Questions[0].Options[0].IsCorrect); // A
        Assert.Contains("Reasoning for question one", result.Questions[0].ExplanationForCorrect);
        Assert.DoesNotContain("Question two stem", result.Questions[0].ExplanationForCorrect); // boundary respected

        Assert.Equal("Question two stem?", result.Questions[1].Text);
        Assert.True(result.Questions[1].Options[2].IsCorrect); // C
        Assert.Equal("Two C", result.Questions[1].Options[2].Text);
    }

    [Fact]
    public void Strips_a_leading_explanation_title_even_when_glued_to_the_text()
    {
        using var docx = BuildDocx(
            "A stem needing explanation?",
            "A. Alpha", "B. Bravo", "C. Charlie", "D. Delta",
            "Correct Answer: A",
            "Explanation:Alpha is correct because of the paramount duty.");

        var result = _parser.Parse(docx);

        var q = Assert.Single(result.Questions);
        Assert.StartsWith("Alpha is correct", q.ExplanationForCorrect);
        Assert.DoesNotContain("Explanation:", q.ExplanationForCorrect);
    }

    [Fact]
    public void Flags_a_placeholder_when_no_explanation_is_present()
    {
        using var docx = BuildDocx(
            "A question with no reasoning after it?",
            "A. First", "B. Second", "C. Third", "D. Fourth",
            "Correct Answer: C");

        var result = _parser.Parse(docx);

        var q = Assert.Single(result.Questions);
        Assert.True(q.Options[2].IsCorrect);
        Assert.Equal("No explanation provided.", q.ExplanationForCorrect);
        Assert.Contains(q.Notes, n => n.Contains("placeholder"));
    }

    [Fact]
    public void Still_recognizes_the_legacy_thats_right_format()
    {
        using var docx = BuildDocx(
            "Legacy question stem?",
            "A. Alpha", "B. Bravo", "C. Charlie", "D. Delta",
            "That's right, the correct answer is B \"Bravo\".",
            "Because Bravo is correct.",
            "Wrong! Not the others.");

        var result = _parser.Parse(docx);

        var q = Assert.Single(result.Questions);
        Assert.True(q.Options[1].IsCorrect);
        Assert.Equal("Bravo", q.Options[1].Text);
        Assert.Contains("Because Bravo is correct", q.ExplanationForCorrect);
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
}
