using ATS.AiScreening.Application.Ports;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Entities;
using ATS.AiScreening.Domain.Ports;
using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Application.Ports;
using Microsoft.Extensions.Logging;

namespace ATS.AiScreening.Infrastructure.CrossModule;

/// <summary>
/// Adapter cua port rieng <see cref="IRecruitmentReader"/> (AiScreening.Application) tren
/// <see cref="IRecruitmentQueries"/> (Recruitment.Application). Day la cho DUY NHAT trong AiScreening
/// biet hinh dang du lieu cua Recruitment.
/// </summary>
public sealed class RecruitmentReaderAdapter : IRecruitmentReader
{
    private readonly IRecruitmentQueries _queries;

    public RecruitmentReaderAdapter(IRecruitmentQueries queries) => _queries = queries;

    public async Task<JobInfo?> GetJobAsync(Guid jobId, CancellationToken ct = default)
    {
        var job = await _queries.GetJobAsync(jobId, ct).ConfigureAwait(false);
        return job is null ? null : Map(job);
    }

    public async Task<CvInfo?> GetCvAsync(Guid cvId, CancellationToken ct = default)
    {
        var cv = await _queries.GetCvAsync(cvId, ct).ConfigureAwait(false);
        return cv is null ? null : Map(cv);
    }

    public async Task<ApplicationInfo?> GetApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        var app = await _queries.GetApplicationAsync(applicationId, ct).ConfigureAwait(false);
        return app is null ? null : new ApplicationInfo(app.ApplicationId, app.Status, Map(app.Job), Map(app.Cv));
    }

    public Task<Guid?> GetApplicationIdOfInterviewAsync(Guid interviewId, CancellationToken ct = default)
        => _queries.GetApplicationIdOfInterviewAsync(interviewId, ct);

    public Task<IReadOnlyList<Guid>> ListApplicationIdsByJobAsync(Guid jobId, CancellationToken ct = default)
        => _queries.ListApplicationIdsByJobAsync(jobId, ct);

    private static JobInfo Map(JobSnapshot job)
        => new(job.JobId, job.HrOwnerUserId, job.Title, job.Description ?? string.Empty, job.Requirements ?? string.Empty, job.Status);

    private static CvInfo Map(CvSnapshot cv)
        => new(cv.CvId, cv.CandidateUserId, cv.Name, cv.ExtractedText ?? string.Empty);
}

/// <summary>
/// Ban rong cua <see cref="IRecruitmentQueries"/> — chi dang ky bang TryAdd khi Recruitment chua
/// hien thuc (nhanh AiScreening tu boot/test duoc). Moi truy van tra "khong co".
/// </summary>
public sealed class EmptyRecruitmentQueries : IRecruitmentQueries
{
    public Task<ApplicationSnapshot?> GetApplicationAsync(Guid applicationId, CancellationToken ct = default)
        => Task.FromResult<ApplicationSnapshot?>(null);

    public Task<JobSnapshot?> GetJobAsync(Guid jobId, CancellationToken ct = default)
        => Task.FromResult<JobSnapshot?>(null);

    public Task<CvSnapshot?> GetCvAsync(Guid cvId, CancellationToken ct = default)
        => Task.FromResult<CvSnapshot?>(null);

    public Task<Guid?> GetApplicationIdOfInterviewAsync(Guid interviewId, CancellationToken ct = default)
        => Task.FromResult<Guid?>(null);

    public Task<IReadOnlyList<Guid>> ListApplicationIdsByJobAsync(Guid jobId, CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Guid>>([]);
}

/// <summary>
/// Recruitment goi sau khi nop don thanh cong: ghi AiScore <c>Pending</c> roi day viec cham le
/// (<c>ScreeningJobId</c> rong) vao hang doi.
/// </summary>
/// <remarks>
/// KHONG BAO GIO nem loi nguoc ve Recruitment: don da luu roi, Redis/DB AI chet khong duoc bien
/// thanh "nop don that bai". Diem o lai Pending; HR chay sang loc lo sau van cham duoc (RB9).
/// </remarks>
public sealed partial class ScreeningTriggerAdapter : IApplicationScreeningTrigger
{
    private readonly IAiScoreRepository _scores;
    private readonly IScreeningQueue _queue;
    private readonly TimeProvider _clock;
    private readonly ILogger<ScreeningTriggerAdapter> _logger;

    public ScreeningTriggerAdapter(
        IAiScoreRepository scores,
        IScreeningQueue queue,
        TimeProvider clock,
        ILogger<ScreeningTriggerAdapter> logger)
    {
        _scores = scores;
        _queue = queue;
        _clock = clock;
        _logger = logger;
    }

    public async Task RequestScreeningAsync(Guid applicationId, CancellationToken ct = default)
    {
        try
        {
            var now = _clock.GetUtcNow();
            var score = await _scores.GetByApplicationAsync(applicationId, ct).ConfigureAwait(false)
                        ?? AiScore.CreatePending(applicationId, now);
            score.MarkPending(now);
            await _scores.UpsertAsync(score, ct).ConfigureAwait(false);

            await _queue.EnqueueAsync(new ScreeningTask(applicationId, Guid.Empty), ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogEnqueueFailed(_logger, ex, applicationId);
        }
    }

    [LoggerMessage(EventId = 4140, Level = LogLevel.Warning, Message = "Khong xep duoc don {ApplicationId} vao hang doi cham AI — diem giu Pending")]
    private static partial void LogEnqueueFailed(ILogger logger, Exception ex, Guid applicationId);
}

/// <summary>
/// Recruitment hoi diem AI cua nhieu don (danh sach xep hang, lich su ung tuyen). Chi tra kieu
/// nguyen thuy. Don khong co dong = chua cham — Recruitment van hien (RB9).
/// </summary>
public sealed class ApplicationScoreReader : IApplicationScoreReader
{
    private readonly IAiScoreRepository _scores;

    public ApplicationScoreReader(IAiScoreRepository scores) => _scores = scores;

    public async Task<IReadOnlyDictionary<Guid, ApplicationAiScore>> GetScoresAsync(
        IReadOnlyCollection<Guid> applicationIds,
        CancellationToken ct = default)
    {
        var rows = await _scores.ListByApplicationsAsync(applicationIds, ct).ConfigureAwait(false);
        return rows.ToDictionary(
            r => r.ApplicationId,
            r => new ApplicationAiScore(r.Score, r.Summary, r.AdapterUsed, r.ScreeningStatus.ToString()));
    }
}
