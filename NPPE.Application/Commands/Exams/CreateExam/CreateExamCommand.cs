using MediatR;
using NPPE.Application.Repositories;
using NPPE.Domain.Entities;

namespace NPPE.Application.Commands.Exams.CreateExam;
public record CreateExamCommand(string Title, string Description, bool IsActive = true) : IRequest<Guid>;

public class CreateExamCommandHandler : IRequestHandler<CreateExamCommand, Guid>
{
    private readonly IExamRepository _examRepository;

    public CreateExamCommandHandler(IExamRepository examRepository)
    {
        _examRepository = examRepository;
    }

    public async Task<Guid> Handle(CreateExamCommand request, CancellationToken cancellationToken)
    {
        var exam = new Exam
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            IsActive = request.IsActive
        };

        await _examRepository.AddAsync(exam);
        return exam.Id;
    }
}