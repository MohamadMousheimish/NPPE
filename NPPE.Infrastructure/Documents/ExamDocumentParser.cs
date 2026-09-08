using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using NPPE.Application.Documents;

namespace NPPE.Infrastructure.Documents;

/// <summary>
/// Parses the NPPE ".docx" exam template into questions. Each question is:
///   [stem paragraph] [4 options] ["Correct Answer: X …"] [explanation paragraphs …].
/// Options may be one-per-paragraph ("A. text") or all four glued into a single
/// paragraph ("A. t1B. t2C. t3D. t4"); both are handled. The correct-answer line may
/// read "Correct Answer: B" (current format) or "That's right, the correct answer is B"
/// (legacy). Parsing is best-effort — uncertain results are flagged as notes for review.
/// </summary>
public class ExamDocumentParser : IExamDocumentParser
{
    private const string DefaultExplanation = "No explanation provided.";

    // Correct-answer anchors: "Correct Answer: B …" (current) or "That's right, the correct answer is B" (legacy).
    private static readonly Regex CorrectAnswerLineRe =
        new(@"^\s*Correct\s+Answer\s*[:\-–—]?\s*([A-D])\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LegacyAnswerLineRe =
        new(@"^\s*That.*?correct answer is\s+([A-D])\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Four options glued into one paragraph: "A. …B. …C. …D. …".
    private static readonly Regex GluedOptionsRe =
        new(@"^\s*A[\.\)]\s*(.*?)B[\.\)]\s*(.*?)C[\.\)]\s*(.*?)D[\.\)]\s*(.*)$",
            RegexOptions.Singleline | RegexOptions.Compiled);
    // A single lettered option paragraph: "A. text" / "B) text".
    private static readonly Regex LetteredOptionRe =
        new(@"^\s*([A-D])[\.\)]\s+(.*)$", RegexOptions.Singleline | RegexOptions.Compiled);

    // Standalone header lines inside the explanation block that add no value.
    private static readonly HashSet<string> ExplanationHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Explanation", "Explanation:", "Why the other choices are wrong", "Why the other choices are wrong:",
        "Why the other answers are wrong", "Why the other answers are wrong:"
    };

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

        // Locate every correct-answer anchor and the letter it names.
        var anchors = new List<(int Index, char Letter)>();
        for (int i = 0; i < paras.Count; i++)
        {
            var letter = AnchorLetter(paras[i]);
            if (letter != null) anchors.Add((i, letter.Value));
        }

        if (anchors.Count == 0)
        {
            result.Recognized = false;
            result.Error = "This doesn't look like a valid NPPE exam document. Expected each question to be " +
                           "followed by a line like “Correct Answer: B”. None were found.";
            return result;
        }

        result.Recognized = true;

        // First pass: resolve each question's options + where its stem sits, so we can
        // bound each explanation block at the start of the next question's stem.
        var blocks = new List<(int Anchor, char Letter, List<string> Options, int StemIndex, bool ByOrder)>();
        foreach (var (index, letter) in anchors)
        {
            var (options, optionsStart, byOrder) = ExtractOptions(paras, index);
            int stemIndex = optionsStart >= 0 ? optionsStart - 1 : -1;
            blocks.Add((index, letter, options, stemIndex, byOrder));
        }

        for (int k = 0; k < blocks.Count; k++)
        {
            var b = blocks[k];
            var q = new ParsedQuestion();

            // stem: the paragraph immediately before the options
            q.Text = b.StemIndex >= 0 ? Capitalize(paras[b.StemIndex]) : string.Empty;

            // options
            for (int j = 0; j < b.Options.Count; j++)
                q.Options.Add(new ParsedOption { Label = (char)('A' + j), Text = Capitalize(b.Options[j]) });

            int ci = b.Letter - 'A';
            if (ci >= 0 && ci < q.Options.Count)
                q.Options[ci].IsCorrect = true;

            // explanation: everything after the answer line up to the next question's stem
            int explEnd = (k + 1 < blocks.Count && blocks[k + 1].StemIndex >= 0)
                ? blocks[k + 1].StemIndex
                : paras.Count;
            var explLines = new List<string>();
            for (int j = b.Anchor + 1; j < explEnd && j < paras.Count; j++)
            {
                if (ExplanationHeaders.Contains(paras[j].Trim())) continue;
                explLines.Add(paras[j]);
            }
            var explanation = string.Join("\n", explLines).Trim();
            if (string.IsNullOrWhiteSpace(explanation))
            {
                explanation = DefaultExplanation;
                q.Notes.Add("No explanation was found — a placeholder was inserted.");
            }
            // Single combined explanation is shown whether the student is right or wrong.
            q.ExplanationForCorrect = explanation;
            q.ExplanationForIncorrect = explanation;

            // review flags
            if (b.Options.Count != 4)
                q.Notes.Add($"Expected 4 options but found {b.Options.Count}.");
            if (ci < 0 || ci >= q.Options.Count)
                q.Notes.Add($"The correct answer “{b.Letter}” does not match any option.");
            if (string.IsNullOrWhiteSpace(q.Text))
                q.Notes.Add("The question text is empty.");
            if (b.ByOrder)
                q.Notes.Add("Options had no A–D labels — they were assigned by order.");

            result.Questions.Add(q);
        }

        return result;
    }

    /// <summary>Returns the answer letter if the line is a correct-answer anchor, else null.</summary>
    private static char? AnchorLetter(string line)
    {
        var m = CorrectAnswerLineRe.Match(line);
        if (m.Success) return char.ToUpperInvariant(m.Groups[1].Value[0]);
        m = LegacyAnswerLineRe.Match(line);
        if (m.Success) return char.ToUpperInvariant(m.Groups[1].Value[0]);
        return null;
    }

    /// <summary>
    /// Finds the four options for the question whose answer line is at <paramref name="anchor"/>.
    /// Returns the option texts (prefix-stripped), the paragraph index where the options begin,
    /// and whether labels had to be assigned by order.
    /// </summary>
    private static (List<string> Options, int OptionsStart, bool ByOrder) ExtractOptions(List<string> paras, int anchor)
    {
        // Case 1 — all four glued into the single paragraph right before the anchor.
        if (anchor - 1 >= 0)
        {
            var glued = GluedOptionsRe.Match(paras[anchor - 1]);
            if (glued.Success)
            {
                var opts = new List<string>
                {
                    glued.Groups[1].Value.Trim(), glued.Groups[2].Value.Trim(),
                    glued.Groups[3].Value.Trim(), glued.Groups[4].Value.Trim()
                };
                return (opts, anchor - 1, false);
            }
        }

        // Case 2 — four separate paragraphs before the anchor.
        if (anchor - 4 >= 0)
        {
            var block = paras.GetRange(anchor - 4, 4);

            // 2a: lettered "A. …" … "D. …" in order → strip the prefixes.
            bool lettered = true;
            var stripped = new List<string>();
            for (int j = 0; j < 4; j++)
            {
                var mm = LetteredOptionRe.Match(block[j]);
                if (mm.Success && char.ToUpperInvariant(mm.Groups[1].Value[0]) == (char)('A' + j))
                    stripped.Add(mm.Groups[2].Value.Trim());
                else { lettered = false; break; }
            }
            if (lettered) return (stripped, anchor - 4, false);

            // 2b: four unlettered paragraphs → take them verbatim, labels by order.
            if (block.All(l => !string.IsNullOrWhiteSpace(l)))
                return (block.Select(l => l.Trim()).ToList(), anchor - 4, true);
        }

        // Couldn't confidently find four options.
        return (new List<string>(), -1, false);
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

    /// <summary>Upper-cases the first letter (leaves the rest untouched).</summary>
    private static string Capitalize(string s)
    {
        s = s.Trim();
        if (s.Length == 0) return s;
        return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
