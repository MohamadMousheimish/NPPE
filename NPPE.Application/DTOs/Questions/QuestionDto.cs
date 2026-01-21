namespace NPPE.Application.DTOs.Questions;
public record QuestionDto(Guid Id, Guid ExamId, string Text, string ExplanationForCorrect,
    string ExplanationForIncorrect, List<AnswerOptionDto> Options);