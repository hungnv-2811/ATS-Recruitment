using ATS.Recruitment.Application.Ports;

namespace ATS.Recruitment.Infrastructure.CrossModule;

/// <summary>Dung khi module AiScreening khong duoc nap (test Recruitment don le).</summary>
internal sealed class NoOpScreeningTrigger : IApplicationScreeningTrigger
{
    public Task RequestScreeningAsync(Guid applicationId, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Moi don deu "Pending" — dung RB9: thieu diem khong chan gi ca.</summary>
internal sealed class NoScoreReader : IApplicationScoreReader
{
    public Task<IReadOnlyDictionary<Guid, ApplicationAiScore>> GetScoresAsync(
        IReadOnlyCollection<Guid> applicationIds,
        CancellationToken ct = default)
        => Task.FromResult<IReadOnlyDictionary<Guid, ApplicationAiScore>>(new Dictionary<Guid, ApplicationAiScore>());
}
