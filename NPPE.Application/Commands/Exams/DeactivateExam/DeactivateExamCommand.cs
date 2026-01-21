using MediatR;
using NPPE.Application.Repositories;

namespace NPPE.Application.Commands.Exams.DeactivateExam;
public record DeactivateExamCommand(Guid Id) : IRequest;

public class DeactivateExamCommandHandler : IRequestHandler<DeactivateExamCommand>
{
    private readonly IExamRepository _examRepository;

    public DeactivateExamCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task Handle(DeactivateExamCommand request, CancellationToken ct)
    {
        var exam = await _examRepository.GetByIdAsync(request.Id);
        if (exam == null)
            throw new InvalidOperationException("Exam not found.");

        exam.IsActive = false; // soft delete
        await _examRepository.UpdateAsync(exam);
    }
}