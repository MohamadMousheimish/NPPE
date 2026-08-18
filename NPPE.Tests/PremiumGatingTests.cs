using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Localization;
using Moq;
using NPPE.Application.DTOs.Exams;
using NPPE.Application.Queries.Exams.GetExamWithQuestions;
using NPPE.Domain.Entities;
using NPPE.Web.Pages.Student.Exams;
using NPPE.Web.Resources;
using Xunit;

namespace NPPE.Tests;

/// <summary>
/// Verifies that taking an exam is gated on the live AppUser.IsPremium flag —
/// non-premium students are bounced to Pricing on both GET and POST.
/// </summary>
public class PremiumGatingTests
{
    private static Mock<UserManager<AppUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static TakeModel BuildModel(Mock<IMediator> mediator, AppUser user, string userId = "user_1")
    {
        var userManager = MockUserManager();
        userManager.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);

        var localizer = new Mock<IStringLocalizer<SharedResource>>();
        localizer.Setup(l => l[It.IsAny<string>()]).Returns<string>(k => new LocalizedString(k, k));

        var model = new TakeModel(mediator.Object, userManager.Object, localizer.Object);
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId) }, "test"));
        model.PageContext = new PageContext { HttpContext = new DefaultHttpContext { User = principal } };
        return model;
    }

    private static AppUser User(bool premium) =>
        new() { Id = "user_1", Email = "s@nppe.ca", FirstName = "S", LastName = "T", IsPremium = premium };

    [Fact]
    public async Task OnGet_redirects_non_premium_user_to_pricing()
    {
        var mediator = new Mock<IMediator>();
        var model = BuildModel(mediator, User(premium: false));

        var result = await model.OnGetAsync(Guid.NewGuid());

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Payments/Pricing", redirect.PageName);
        // Gate short-circuits before any exam is loaded.
        mediator.Verify(m => m.Send(It.IsAny<GetExamWithQuestionsQuery>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnGet_allows_premium_user_to_load_the_exam()
    {
        var examId = Guid.NewGuid();
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetExamWithQuestionsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ExamWithQuestionsDto { Id = examId, Title = "Ethics" });

        var model = BuildModel(mediator, User(premium: true));

        var result = await model.OnGetAsync(examId);

        Assert.IsType<PageResult>(result);
        Assert.NotNull(model.Exam);
        Assert.Equal(examId, model.ExamId);
    }

    [Fact]
    public async Task OnGet_premium_user_missing_exam_returns_not_found()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(m => m.Send(It.IsAny<GetExamWithQuestionsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ExamWithQuestionsDto?)null);

        var model = BuildModel(mediator, User(premium: true));

        var result = await model.OnGetAsync(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result); // reached the query, so the gate passed
    }

    [Fact]
    public async Task OnPost_re_checks_premium_and_redirects_when_it_lapsed()
    {
        var mediator = new Mock<IMediator>();
        var model = BuildModel(mediator, User(premium: false));
        model.ExamId = Guid.NewGuid();

        var result = await model.OnPostAsync();

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Payments/Pricing", redirect.PageName);
    }
}
