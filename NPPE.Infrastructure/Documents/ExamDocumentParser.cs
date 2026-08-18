using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NPPE.Application.Documents;

namespace NPPE.Infrastructure.Documents;

/// <summary>
/// Parses the NPPE ".docx" exam template into questions. The format per question is:
///   [stem paragraphs] [4 option paragraphs] ["That's right, the correct answer is X ..." + reasoning]
///   ["Wrong! ... but the correct answer is X ..." + reasoning].
/// Parsing is best-effort — uncertain results are surfaced as notes for review rather than rejected.
/// </summary>
public class ExamDocumentParser : IExamDocumentParser
{
    private const string DefaultExplanation = "No explanation provided.";

    private static readonly Regex CorrectAnswerRe =
        new(@"correct answer is\s+([A-D])\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex OptionPrefixRe =
        new(@"^\s*([A-D])[\.\)]?\s*", RegexOptions.Compiled);
    private static readonly Regex StripPrefixRe =
        new(@"^\s*[A-D][\.\)]?\s*(.*)$", RegexOptions.Singleline | RegexOptions.Compiled);

    public ParsedExamResult Parse(Stream documentStream)
    {
        var result = new ParsedExamResult();
        List<string> paras;
        try
        {
            paras = ReadParagraphs(documentStream);
        }
        catch (Exception)
        {
            result.Recognized = false;
            result.Error = "The file could not be read as a Word (.docx) document.";
            return result;
        }

        var correctIdx = new List<int>();
        for (int i = 0; i < paras.Count; i++)
            if (IsCorrectAnchor(paras[i]))
                correctIdx.Add(i);

        if (correctIdx.Count == 0)
        {
            result.Recognized = false;
            result.Error = "This doesn't look like a valid NPPE exam document. Expected each question to be " +
                           "followed by a line like “That’s right, the correct answer is B …”. " +
                           "None were found.";
            return result;
        }

        result.Recognized = true;
        int stemStart = 0;

        for (int k = 0; k < correctIdx.Count; k++)
        {
            int c = correctIdx[k];
            int nextC = (k + 1 < correctIdx.Count) ? correctIdx[k + 1] : paras.Count;
            var q = new ParsedQuestion();

            // --- options: the 4 paragraphs immediately before the "That's right" line ---
            int optStart = c - 4;
            var optionLines = new List<string>();
            for (int j = Math.Max(optStart, stemStart); j < c; j++)
                optionLines.Add(paras[j]);
            ParseOptions(optionLines, q);

            // --- stem: everything from the running start up to the options ---
            var stemLines = new List<string>();
            for (int j = stemStart; j < optStart && j >= 0; j++)
                stemLines.Add(paras[j]);
            q.Text = string.Join(" ", stemLines).Trim();

            // --- correct answer letter (trusted; the quoted restatement is not, it can be mispasted) ---
            var m = CorrectAnswerRe.Match(paras[c]);
            char correctLetter = char.ToUpperInvariant(m.Groups[1].Value[0]);
            int ci = correctLetter - 'A';
            if (ci >= 0 && ci < q.Options.Count)
                q.Options[ci].IsCorrect = true;

            // --- "Wrong!" anchor within this question's span ---
            int w = -1;
            for (int j = c + 1; j < nextC; j++)
                if (IsWrongAnchor(paras[j])) { w = j; break; }

            // --- correct-answer explanation: paragraphs between the two anchors ---
            int explEnd = (w >= 0) ? w : nextC;
            var correctExpl = new List<string>();
            for (int j = c + 1; j < explEnd; j++)
                correctExpl.Add(paras[j]);
            q.ExplanationForCorrect = string.Join("\n", correctExpl).Trim();

            // --- incorrect-answer explanation + find where the NEXT stem begins ---
            if (w >= 0)
            {
                var correctNorm = Normalize(q.ExplanationForCorrect);
                var wrongLines = new List<string>();
                int j = w + 1;
                int limit = nextC - 4; // don't consume the next question's options
                while (j < limit && IsPartOf(paras[j], correctNorm))
                {
                    wrongLines.Add(paras[j]);
                    j++;
                }
                q.ExplanationForIncorrect = wrongLines.Count > 0
                    ? string.Join("\n", wrongLines).Trim()
                    : q.ExplanationForCorrect;
                stemStart = j;
            }
            else
            {
                q.ExplanationForIncorrect = q.ExplanationForCorrect;
                stemStart = Math.Max(c + 1, nextC - 4);
                q.Notes.Add("No “Wrong!” response was found for this question.");
            }

            // --- auto-fill empty explanations (per your choice), flag it ---
            if (string.IsNullOrWhiteSpace(q.ExplanationForCorrect))
            {
                q.ExplanationForCorrect = DefaultExplanation;
                q.Notes.Add("No “correct” explanation was found — a placeholder was inserted.");
            }
            if (string.IsNullOrWhiteSpace(q.ExplanationForIncorrect))
                q.ExplanationForIncorrect = DefaultExplanation;

            // --- notes / flags for review ---
            if (optionLines.Count != 4)
                q.Notes.Add($"Expected 4 options but found {optionLines.Count}.");
            if (ci < 0 || ci >= q.Options.Count)
                q.Notes.Add($"The correct answer “{correctLetter}” does not match any option.");
            if (string.IsNullOrWhiteSpace(q.Text))
                q.Notes.Add("The question text is empty.");

            result.Questions.Add(q);
        }

        return result;
    }

    private static void ParseOptions(List<string> lines, ParsedQuestion q)
    {
        // Prefixed only if all 4 lines start with A, B, C, D in order.
        bool prefixed = lines.Count == 4;
        for (int i = 0; i < lines.Count && prefixed; i++)
        {
            var mm = OptionPrefixRe.Match(lines[i]);
            if (!(mm.Success && char.ToUpperInvariant(mm.Groups[1].Value[0]) == (char)('A' + i)))
                prefixed = false;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            var text = lines[i];
            if (prefixed)
            {
                var mm = StripPrefixRe.Match(lines[i]);
                if (mm.Success) text = mm.Groups[1].Value;
            }
            q.Options.Add(new ParsedOption { Label = (char)('A' + i), Text = text.Trim() });
        }

        if (!prefixed && lines.Count == 4)
            q.Notes.Add("Options had no A–D labels — they were assigned by order.");
    }

    private static List<string> ReadParagraphs(Stream stream)
    {
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document?.Body;
        var res = new List<string>();
        if (body == null) return res;
        foreach (var p in body.Descendants<Paragraph>())
        {
            var text = p.InnerText.Trim();
            if (text.Length > 0) res.Add(text);
        }
        return res;
    }

    private static bool IsCorrectAnchor(string s)
    {
        var t = s.TrimStart();
        return t.StartsWith("That", StringComparison.OrdinalIgnoreCase) && CorrectAnswerRe.IsMatch(s);
    }

    private static bool IsWrongAnchor(string s) =>
        s.TrimStart().StartsWith("Wrong", StringComparison.OrdinalIgnoreCase);

    /// <summary>A wrong-block paragraph is treated as duplicated reasoning if it appears within the correct reasoning.</summary>
    private static bool IsPartOf(string paragraph, string correctReasonNorm)
    {
        var p = Normalize(paragraph);
        if (p.Length == 0) return true;
        if (correctReasonNorm.Length == 0) return false;
        return correctReasonNorm.Contains(p) || p.Contains(correctReasonNorm);
    }

    private static string Normalize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s.ToLowerInvariant())
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        return sb.ToString();
    }
}
