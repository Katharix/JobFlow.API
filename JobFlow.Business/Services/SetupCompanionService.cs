using JobFlow.Business.DI;
using JobFlow.Business.Services.ServiceInterfaces;
using JobFlow.Domain;
using JobFlow.Domain.Models;

namespace JobFlow.Business.Services;

[ScopedService]
public class SetupCompanionService : ISetupCompanionService
{
    private readonly IUnitOfWork _unitOfWork;

    public SetupCompanionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> TrackEventAsync(Guid organizationId, string sessionId, string questionKey, string? answerKey)
    {
        var ev = new SetupCompanionEvent
        {
            OrganizationId = organizationId,
            SessionId = sessionId,
            QuestionKey = questionKey,
            AnswerKey = answerKey,
            OccurredAt = DateTimeOffset.UtcNow
        };

        await _unitOfWork.RepositoryOf<SetupCompanionEvent>().AddAsync(ev);
        await _unitOfWork.SaveChangesAsync();
        return Result.Success();
    }
}
