using MediatR;
using NPPE.Application.Repositories;

namespace NPPE.Application.Commands.Exams.UpdateExam;
public record UpdateExamCommand(Guid Id, string Title, string Description, bool IsActive) : IRequest;


public class UpdateExamCommandHandler : IRequestHandler<UpdateExamCommand>
{
    private readonly IExamRepository _examRepository;

    public UpdateExamCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task Handle(UpdateExamCommand request, CancellationToken ct)
    {
        var exam = await _examRepository.GetByIdAsync(request.Id);
        if (exam == null)
            throw new InvalidOperationException("Exam not found.");

        exam.Title = request.Title;
        exam.Description = request.Description;
        exam.IsActive = request.IsActive;

        await _examRepository.UpdateAsync(exam);
    }
}
