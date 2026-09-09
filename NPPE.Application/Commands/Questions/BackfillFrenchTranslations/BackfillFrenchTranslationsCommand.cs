using MediatR;
using NPPE.Application.Repositories;

namespace NPPE.Application.Commands.Questions.BackfillFrenchTranslations;

/// <summary>Applies French translations to existing exam content, matched by exam title +
/// English question text (options matched by their A–D label). Idempotent: re-running with
/// corrected text overwrites the French. Returns how many questions/options were updated
/// and any entries that couldn't be matched.</summary>
public record BackfillFrenchTranslationsCommand(FrenchExamSet Payload) : IRequest<BackfillFrenchResult>;

public record FrenchExamSet(List<FrenchExam> Exams);
public record FrenchExam(string Title, List<FrenchQuestion> Questions);
public record FrenchQuestion(
    string Text,                 // English stem — the match key
    string? TextFr,
    string? ExplanationFr,       // one combined block, applied to both correct/incorrect
    Dictionary<string, string>? Options);  // label ("A".."D") -> French option text

public record BackfillFrenchResult(int QuestionsUpdated, int OptionsUpdated, List<string> Unmatched);

public class BackfillFrenchTranslationsCommandHandler
    : IRequestHandler<BackfillFrenchTranslationsCommand, BackfillFrenchResult>
{
    private readonly IExamRepository _exams;

    public BackfillFrenchTranslationsCommandHandler(IExamRepository exams)
    {
        _exams = exams;
    }

    public async Task<BackfillFrenchResult> Handle(BackfillFrenchTranslationsCommand request, CancellationToken ct)
    {
        int qUpdated = 0, oUpdated = 0;
        var unmatched = new List<string>();

        foreach (var fx in request.Payload.Exams)
        {
            var exam = await _exams.GetExamByTitleWithQuestionsAsync(fx.Title);
            if (exam == null)
            {
                unmatched.Add($"Exam not found: \"{fx.Title}\"");
                continue;
            }

            foreach (var fq in fx.Questions)
            {
                var q = exam.Questions.FirstOrDefault(x => x.Text.Trim() == fq.Text.Trim());
                if (q == null)
                {
                    unmatched.Add($"[{fx.Title}] question not matched: \"{Trim(fq.Text)}\"");
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(fq.TextFr)) q.TextFr = fq.TextFr.Trim();
                if (!string.IsNullOrWhiteSpace(fq.ExplanationFr))
                {
                    q.ExplanationForCorrectFr = fq.ExplanationFr.Trim();
                    q.ExplanationForIncorrectFr = fq.ExplanationFr.Trim();
                }

                if (fq.Options != null)
                {
                    foreach (var opt in q.Options)
                    {
                        if (fq.Options.TryGetValue(opt.Label.ToString(), out var fr) && !string.IsNullOrWhiteSpace(fr))
                        {
                            opt.TextFr = fr.Trim();
                            oUpdated++;
                        }
                    }
                }
                qUpdated++;
            }

            await _exams.UpdateAsync(exam); // persists the tracked Fr changes on questions/options
        }

        return new BackfillFrenchResult(qUpdated, oUpdated, unmatched);
    }

    private static string Trim(string s) => s.Length <= 70 ? s : s[..67] + "...";
}
