namespace NPPE.Application.Documents;

/// <summary>Parses an uploaded exam document (.docx) into questions, options, correct answers and explanations.</summary>
public interface IExamDocumentParser
{
    ParsedExamResult Parse(Stream documentStream);
}
