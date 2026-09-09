using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NPPE.Application.Commands.Questions.BackfillFrenchTranslations;
using NPPE.Domain.Entities;
using NPPE.Infrastructure.Persistence;
using Xunit;

namespace NPPE.IntegrationTests;

public class FrenchBackfillTests : IClassFixture<NppeWebAppFactory>
{
    private readonly NppeWebAppFactory _factory;
    public FrenchBackfillTests(NppeWebAppFactory factory) => _factory = factory;

    [Fact]
    public async Task Backfill_sets_french_by_text_and_option_label()
    {
        const string title = "FR Backfill Exam";
        const string stem = "What is a professional's paramount duty?";
        Guid examId;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var exam = new Exam { Title = title, Description = "x", IsActive = true };
            exam.Questions.Add(new Question
            {
                Text = stem,
                ExplanationForCorrect = "Public safety is paramount.",
                ExplanationForIncorrect = "Public safety is paramount.",
                IsActive = true,
                Options = new List<AnswerOption>
                {
                    new() { Label = 'A', Text = "Protect the employer", IsCorrect = false },
                    new() { Label = 'B', Text = "Hold public safety paramount", IsCorrect = true },
                    new() { Label = 'C', Text = "Maximize profit", IsCorrect = false },
                    new() { Label = 'D', Text = "Follow all orders", IsCorrect = false },
                }
            });
            db.Exams.Add(exam);
            await db.SaveChangesAsync();
            examId = exam.Id;
        }

        var payload = new FrenchExamSet(new List<FrenchExam>
        {
            new(title, new List<FrenchQuestion>
            {
                new(
                    Text: stem,
                    TextFr: "Quel est le devoir primordial d'un professionnel?",
                    ExplanationFr: "La sécurité du public est primordiale.",
                    Options: new Dictionary<string, string>
                    {
                        ["A"] = "Protéger l'employeur",
                        ["B"] = "Faire primer la sécurité du public",
                        ["C"] = "Maximiser le profit",
                        ["D"] = "Suivre tous les ordres"
                    })
            })
        });

        BackfillFrenchResult result;
        using (var scope = _factory.Services.CreateScope())
        {
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            result = await mediator.Send(new BackfillFrenchTranslationsCommand(payload));
        }

        Assert.Equal(1, result.QuestionsUpdated);
        Assert.Equal(4, result.OptionsUpdated);
        Assert.Empty(result.Unmatched);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var q = await db.Questions.Include(x => x.Options).FirstAsync(x => x.ExamId == examId);
            Assert.Equal("Quel est le devoir primordial d'un professionnel?", q.TextFr);
            Assert.Equal("La sécurité du public est primordiale.", q.ExplanationForCorrectFr);
            Assert.Equal("La sécurité du public est primordiale.", q.ExplanationForIncorrectFr);
            Assert.Equal("Faire primer la sécurité du public", q.Options.First(o => o.Label == 'B').TextFr);
            Assert.Equal("Protéger l'employeur", q.Options.First(o => o.Label == 'A').TextFr);
        }
    }
}
