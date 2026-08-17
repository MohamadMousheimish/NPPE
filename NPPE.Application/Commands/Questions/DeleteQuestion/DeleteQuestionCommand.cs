using MediatR;
using NPPE.Application.Repositories;

namespace NPPE.Application.Commands.Questions.DeleteQuestion;
public record DeleteQuestionCommand(Guid Id) : IRequest;

public class DeleteQuestionCommandHandler : IRequestHandler<DeleteQuestionCommand>
{
    private readonly IQuestionRepository _questionRepository;

    public DeleteQuestionCommandHandler(IQuestionRepository questionRepository)
    {
        _questionRepository = questionRepository;
    }

    public async Task Handle(DeleteQuestionCommand request, CancellationToken ct)
    {
        var question = await _questionRepository.GetByIdAsync(request.Id);
        if (question == null)
            return; // or throw

        // Soft delete: keep the row so historical attempt results still resolve
        // the question, but hide it from exams and admin listings.
        question.IsActive = false;
        await _questionRepository.UpdateAsync(question);
    }
}