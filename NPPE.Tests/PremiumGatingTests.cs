using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;
using Moq;
using NPPE.Application.DTOs.Exams;
using NPPE.Application.Queries.Exams.GetExamWithQuestions;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;
using NPPE.Web.Pages.Student.Exams;
using NPPE.Web.Resources;
using Xunit;

namespace NPPE.Tests;

/// <summary>
/// Verifies that taking an exam is gated on an <see cref="ExamUnlock"/> and its
/// per-exam attempt cap — students with no unlock are bounced to Pricing, and
/// students who've used all attempts are bounced back to their exams list.
/// </summary>
public class PremiumGatingTests
{
    private readonly Mock<IExamUnlockRepository> _unlocks = new();
    private readonly Mock<IExamAttemptRepository> _attempts = new();

    private TakeModel BuildModel(Mock<IMediator> mediator, string userId = "user_1")
    {
        var localizer = new Mock<IStringLocalizer<SharedResource>>();
        localizer.Setup(l => l[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));

        var model = new TakeModel(mediator.Object, _unlocks.Object, _attempts.Object, localizer.Object);
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "test"));
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };
        model.TempData = new TempDataDictionary(model.PageContext.HttpContext, Mock.Of<ITempDataProvider>());
        return model;
    }

    private void GrantUnlock(Guid examId, int attemptsAllowed = 2, int attemptsUsed = 0, string userId = "user_1")
    {
        _unlocks.Setup(u => u.GetAsync(userId, examId))
            .ReturnsAsync(new ExamUnlock { UserId = userId, ExamId = examId, AttemptsAllowed = attemptsAllowed });
        _attempts.Setup(a => a.CountAttemptsAsync(userId, examId)).ReturnsAsync(attemptsUsed);
    }

    [Fact]
    public async Task OnGet_redirects_user_without_unlock_to_pricing()
    {
        var mediator = new Mock<IMediator>();
        _unlocks.Setup(u => u.GetAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync((ExamUnlock?)null);
        var model = BuildModel(mediator);

        var result = await model.OnGetAsync(Guid.NewGuid());

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Payments/Pricing", redirect.PageName);
        // Gate short-circuits before any exam is loaded.
        mediator.Verify(m => m.Send(It.IsAny<GetExamWithQuestionsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGet_allows_unlocked_user_to_load_the_exam()
    {
        var examId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetExamWithQuestionsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExamWithQuestionsDto { Id = examId, Title = "Ethics" });
        GrantUnlock(examId, attemptsAllowed: 2, attemptsUsed: 0);

        var model = BuildModel(mediator);

        var result = await model.OnGetAsync(examId);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.Exam);
        Assert.Equal(examId, model.ExamId);
        Assert.Equal(2, model.AttemptsRemaining);
    }

    [Fact]
    public async Task OnGet_unlocked_user_missing_exam_returns_not_found()
    {
        var examId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetExamWithQuestionsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ExamWithQuestionsDto?)null);
        GrantUnlock(examId);

        var model = BuildModel(mediator);

        var result = await model.OnGetAsync(examId);

        Assert.IsType<NotFoundResult>(result); // reached the query, so the gate passed
    }

    [Fact]
    public async Task OnGet_redirects_to_exams_list_when_attempts_exhausted()
    {
        var examId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        GrantUnlock(examId, attemptsAllowed: 2, attemptsUsed: 2);

        var model = BuildModel(mediator);

        var result = await model.OnGetAsync(examId);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Student/Exams/Index", redirect.PageName);
        mediator.Verify(m => m.Send(It.IsAny<GetExamWithQuestionsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnPost_re_checks_access_and_redirects_when_unlock_missing()
    {
        var mediator = new Mock<IMediator>();
        _unlocks.Setup(u => u.GetAsync(It.IsAny<string>(), It.IsAny<Guid>())).ReturnsAsync((ExamUnlock?)null);
        var model = BuildModel(mediator);
        model.ExamId = Guid.NewGuid();

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Payments/Pricing", redirect.PageName);
    }
}
