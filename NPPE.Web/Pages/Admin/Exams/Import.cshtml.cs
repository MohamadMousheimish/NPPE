using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NPPE.Application.Commands.Exams.CreateExamFromImport;
using NPPE.Application.Documents;

namespace NPPE.Web.Pages.Admin.Exams
{
    [Authorize(Roles = "Admin")]
    public class ImportModel : PageModel
    {
        private readonly IExamDocumentParser _parser;
        private readonly IMediator _mediator;

        public ImportModel(IExamDocumentParser parser, IMediator mediator)
        {
            _parser = parser;
            _mediator = mediator;
        }

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public string? ErrorMessage { get; set; }

        [BindProperty] public string Title { get; set; } = string.Empty;
        [BindProperty] public string Description { get; set; } = string.Empty;
        [BindProperty] public bool IsActive { get; set; } = true;
        [BindProperty] public string QuestionsJson { get; set; } = string.Empty;

        public void OnGet() { }

        /// <summary>AJAX: parse an uploaded .docx and return the structured result as JSON.</summary>
        public async Task<IActionResult> OnPostParseAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return new JsonResult(new ParsedExamResult { Recognized = false, Error = "Please choose a file to upload." });

            if (!file.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                return new JsonResult(new ParsedExamResult { Recognized = false, Error = "Only Word (.docx) files are supported." });

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            ms.Position = 0;

            var result = _parser.Parse(ms);
            return new JsonResult(result, JsonOpts);
        }

        /// <summary>Final submit: validate the reviewed data server-side and create the exam atomically.</summary>
        public async Task<IActionResult> OnPostCreateAsync()
        {
            List<QuestionInput>? edited;
            try
            {
                edited = JsonSerializer.Deserialize<List<QuestionInput>>(QuestionsJson, JsonOpts);
            }
            catch
            {
                ErrorMessage = "The reviewed questions could not be read. Please try again.";
                return Page();
            }

            if (edited == null || edited.Count == 0)
            {
                ErrorMessage = "There are no questions to import.";
                return Page();
            }

            // Rebuild the command, re-deriving option labels A–D by position (never trust client labels).
            var questions = edited.Select(q => new ImportedQuestion(
                q.Text ?? string.Empty,
                q.ExplanationForCorrect ?? string.Empty,
                q.ExplanationForIncorrect ?? string.Empty,
                (q.Options ?? new()).Select((o, i) => new ImportedOption(
                    o.Text ?? string.Empty, (char)('A' + i), o.IsCorrect)).ToList()
            )).ToList();

            try
            {
                var examId = await _mediator.Send(new CreateExamFromImportCommand(Title, Description, IsActive, questions));
                TempData["SuccessMessage"] = "Exam imported successfully.";
                return RedirectToPage("/Admin/Exams/Details", new { id = examId });
            }
            catch (ArgumentException ex)
            {
                ErrorMessage = ex.Message;
                return Page();
            }
        }

        public class QuestionInput
        {
            public string? Text { get; set; }
            public string? ExplanationForCorrect { get; set; }
            public string? ExplanationForIncorrect { get; set; }
            public List<OptionInput>? Options { get; set; }
        }

        public class OptionInput
        {
            public string? Text { get; set; }
            public bool IsCorrect { get; set; }
        }
    }
}
