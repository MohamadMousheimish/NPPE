namespace NPPE.Application.DTOs.Questions;
public record AnswerOptionDto(Guid? Id, string Text, char Label, bool IsCorrect);