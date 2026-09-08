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

        // Track the deactivation moment so the cleanup job can age it out; clear it on reactivation.
        if (request.IsActive)
            exam.DeactivatedAt = null;
        else if (exam.IsActive)
            exam.DeactivatedAt = DateTime.UtcNow;
        exam.IsActive = request.IsActive;

        await _examRepository.UpdateAsync(exam);
    }
}
