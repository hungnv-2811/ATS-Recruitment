using ATS.AiScreening.Domain;

namespace ATS.AiScreening.Application.Abstractions;

/// <summary>Worker goi handler nay cho moi viec lay ra tu hang doi (docs/contracts.md muc 6).</summary>
public interface IProcessScreeningHandler
{
    Task HandleAsync(ScreeningTask task, CancellationToken ct = default);
}
