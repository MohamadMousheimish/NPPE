using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Questions.BackfillFrenchTranslations;

namespace NPPE.Web.Pages.Admin.Exams;

[Authorize(Roles = "Admin")]
public class ImportFrenchModel : PageModel
{
    private readonly IMediator _mediator;
    public ImportFrenchModel(IMediator mediator) => _mediator = mediator;

    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public BackfillFrenchResult? Result { get; private set; }
    public string? Error { get; private set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            Error = "Please choose a JSON file to upload.";
            return Page();
        }

        FrenchExamSet? payload;
        try
        {
            using var stream = file.OpenReadStream();
            payload = await JsonSerializer.DeserializeAsync<FrenchExamSet>(stream, JsonOpts);
        }
        catch (Exception ex)
        {
            Error = "The file couldn't be read as valid translation JSON: " + ex.Message;
            return Page();
        }

        if (payload == null || payload.Exams == null || payload.Exams.Count == 0)
        {
            Error = "The JSON contained no exams.";
            return Page();
        }

        Result = await _mediator.Send(new BackfillFrenchTranslationsCommand(payload));
        return Page();
    }
}
