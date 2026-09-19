namespace ATS.AiScreening.Domain.Ports;

/// <summary>Hang doi sang loc AI. Adapter that dung Redis (ADR-2).</summary>
public interface IScreeningQueue
{
    Task EnqueueAsync(ScreeningTask task, CancellationToken ct = default);

    Task<ScreeningTask?> DequeueAsync(CancellationToken ct = default);
}
