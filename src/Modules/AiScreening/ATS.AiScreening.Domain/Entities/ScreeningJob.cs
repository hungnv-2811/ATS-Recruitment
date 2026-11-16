using ATS.SharedKernel;

namespace ATS.AiScreening.Domain.Entities;

public enum ScreeningJobStatus
{
    Queued = 0,
    Running = 1,
    Done = 2,
    Cancelled = 3,
}

/// <summary>
/// Mot lo sang loc HR chay cho mot tin (bang <c>aiscreening.screening_jobs</c>) — UI polling tien do.
/// </summary>
/// <remarks>
/// Bat bien: <c>DoneCount + FailedCount &lt;= TargetCount</c>; dat bang TargetCount thi tu Done.
/// Lo rong (tin chua co don nao) Done ngay luc tao — khong bao gio treo o Queued.
/// </remarks>
public sealed class ScreeningJob : Entity<Guid>
{
    private ScreeningJob(Guid id, Guid jobId, Guid startedByHrId, int targetCount, DateTimeOffset now)
        : base(id)
    {
        JobId = jobId;
        StartedByHrId = startedByHrId;
        TargetCount = targetCount;
        Status = ScreeningJobStatus.Queued;
        StartedAt = now;
        CompleteIfFinished(now);
    }

    /// <summary>Danh cho EF Core.</summary>
    private ScreeningJob()
    {
    }

    public Guid JobId { get; private set; }

    /// <summary>Id trong identity.users cua HR bam "Sang loc".</summary>
    public Guid StartedByHrId { get; private set; }

    public int TargetCount { get; private set; }

    public int DoneCount { get; private set; }

    public int FailedCount { get; private set; }

    public ScreeningJobStatus Status { get; private set; }

    public DateTimeOffset StartedAt { get; private set; }

    public DateTimeOffset? FinishedAt { get; private set; }

    public int ProcessedCount => DoneCount + FailedCount;

    public bool IsFinished => Status is ScreeningJobStatus.Done or ScreeningJobStatus.Cancelled;

    public static ScreeningJob Start(Guid jobId, Guid startedByHrId, int targetCount, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(targetCount);
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("JobId khong duoc rong.", nameof(jobId));
        }

        return new ScreeningJob(Guid.CreateVersion7(), jobId, startedByHrId, targetCount, now);
    }

    public void RecordDone(DateTimeOffset now) => RecordProgress(done: 1, failed: 0, now);

    public void RecordFailed(DateTimeOffset now) => RecordProgress(done: 0, failed: 1, now);

    /// <summary>Cong don tien do. Lo da ket thuc thi bo qua (viec den tre sau khi huy/xong).</summary>
    public void RecordProgress(int done, int failed, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(done);
        ArgumentOutOfRangeException.ThrowIfNegative(failed);

        if (IsFinished)
        {
            return;
        }

        var room = TargetCount - ProcessedCount;
        done = Math.Min(done, room);
        failed = Math.Min(failed, room - done);

        DoneCount += done;
        FailedCount += failed;
        Status = ScreeningJobStatus.Running;
        CompleteIfFinished(now);
    }

    public void Cancel(DateTimeOffset now)
    {
        if (IsFinished)
        {
            return;
        }

        Status = ScreeningJobStatus.Cancelled;
        FinishedAt = now;
    }

    private void CompleteIfFinished(DateTimeOffset now)
    {
        if (ProcessedCount >= TargetCount)
        {
            Status = ScreeningJobStatus.Done;
            FinishedAt = now;
        }
    }
}
