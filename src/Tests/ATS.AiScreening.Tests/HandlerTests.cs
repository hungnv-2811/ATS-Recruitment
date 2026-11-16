using ATS.AiScreening.Application;
using ATS.AiScreening.Application.Contracts;
using ATS.AiScreening.Application.Handlers;
using ATS.AiScreening.Application.Scoring;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Entities;
using ATS.AiScreening.Domain.Ports;
using ATS.AiScreening.Domain.Services;
using ATS.AiScreening.Infrastructure.Queue;
using ATS.AiScreening.Infrastructure.Scoring;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ATS.AiScreening.Tests;

public sealed class PreviewScoreHandlerTests
{
    private readonly Guid _candidate = Guid.NewGuid();
    private readonly FakeRecruitmentReader _recruitment = new();
    private readonly InMemoryCacheRepository _cache = new();

    private PreviewScoreHandler Handler(IAiScoringService scoring, IAiUsageQuota quota)
        => new(
            _recruitment,
            new SimpleAnonymizer(),
            new CachedScoringService(scoring, _cache, new AiScreeningOptions(), NullLogger<CachedScoringService>.Instance),
            quota);

    [Fact]
    public async Task Truot_cache_thi_tru_luot_cham_va_ghi_cache()
    {
        var cv = _recruitment.AddCv(_candidate);
        var job = _recruitment.AddJob(Guid.NewGuid());
        var quota = new CountingQuota();
        var handler = Handler(new FakeAiScoringAdapter(), quota);

        var result = await handler.HandleAsync(_candidate, new ScorePreviewRequest(cv.CvId, job.JobId));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.CacheHit);
        Assert.Equal(AdapterNames.Fake, result.Value.AdapterUsed);
        Assert.Equal(1, quota.PreviewUsed);
        Assert.Equal(19, result.Value.PreviewRemainingToday);
        Assert.Equal(1, _cache.Count);
    }

    [Fact]
    public async Task Trung_cache_thi_KHONG_tru_luot_va_khong_goi_AI()
    {
        var cv = _recruitment.AddCv(_candidate);
        var job = _recruitment.AddJob(Guid.NewGuid());
        var quota = new CountingQuota();
        var scoring = StubScoring.Returning(AdapterNames.OpenAi, 81);
        var handler = Handler(scoring, quota);
        var request = new ScorePreviewRequest(cv.CvId, job.JobId);

        var first = await handler.HandleAsync(_candidate, request);
        var second = await handler.HandleAsync(_candidate, request);

        Assert.True(second.IsSuccess);
        Assert.True(second.Value.CacheHit);
        Assert.Equal(first.Value.Score, second.Value.Score);
        Assert.Equal(AdapterNames.OpenAi, second.Value.AdapterUsed);
        Assert.Equal(1, quota.PreviewUsed);
        Assert.Equal(1, scoring.Calls);
    }

    [Fact]
    public async Task Het_luot_thi_QuotaExceeded_va_khong_goi_AI()
    {
        var cv = _recruitment.AddCv(_candidate);
        var job = _recruitment.AddJob(Guid.NewGuid());
        var scoring = StubScoring.Returning(AdapterNames.OpenAi);
        var handler = Handler(scoring, new CountingQuota(previewLimit: 0));

        var result = await handler.HandleAsync(_candidate, new ScorePreviewRequest(cv.CvId, job.JobId));

        Assert.True(result.IsFailure);
        Assert.Equal("quota_exceeded", result.Error.Code);
        Assert.Equal(0, scoring.Calls);
    }

    [Fact]
    public async Task Het_luot_nhung_trung_cache_van_xem_duoc()
    {
        var cv = _recruitment.AddCv(_candidate);
        var job = _recruitment.AddJob(Guid.NewGuid());
        var quota = new CountingQuota(previewLimit: 1);
        var handler = Handler(StubScoring.Returning(AdapterNames.OpenAi), quota);
        var request = new ScorePreviewRequest(cv.CvId, job.JobId);

        await handler.HandleAsync(_candidate, request);     // dung luot cuoi cung
        var again = await handler.HandleAsync(_candidate, request);

        Assert.True(again.IsSuccess);
        Assert.True(again.Value.CacheHit);
        Assert.Equal(0, again.Value.PreviewRemainingToday);
    }

    [Fact]
    public async Task Ket_qua_Keyword_khong_ghi_cache()
    {
        var cv = _recruitment.AddCv(_candidate);
        var job = _recruitment.AddJob(Guid.NewGuid());
        var handler = Handler(new KeywordScoringAdapter(), new CountingQuota());

        var result = await handler.HandleAsync(_candidate, new ScorePreviewRequest(cv.CvId, job.JobId));

        Assert.Equal(AdapterNames.Keyword, result.Value.AdapterUsed);
        Assert.Equal(0, _cache.Count);
    }

    [Fact]
    public async Task CV_cua_nguoi_khac_tra_not_found()
    {
        var cv = _recruitment.AddCv(Guid.NewGuid());
        var job = _recruitment.AddJob(Guid.NewGuid());
        var quota = new CountingQuota();
        var handler = Handler(new FakeAiScoringAdapter(), quota);

        var result = await handler.HandleAsync(_candidate, new ScorePreviewRequest(cv.CvId, job.JobId));

        Assert.Equal("not_found", result.Error.Code);
        Assert.Equal(0, quota.PreviewUsed);
    }

    [Fact]
    public async Task Tin_da_dong_tra_not_found()
    {
        var cv = _recruitment.AddCv(_candidate);
        var job = _recruitment.AddJob(Guid.NewGuid(), status: "Closed");
        var handler = Handler(new FakeAiScoringAdapter(), new CountingQuota());

        var result = await handler.HandleAsync(_candidate, new ScorePreviewRequest(cv.CvId, job.JobId));

        Assert.Equal("not_found", result.Error.Code);
    }

    [Fact]
    public async Task CV_khong_co_chu_tra_validation_va_khong_tru_luot()
    {
        var cv = _recruitment.AddCv(_candidate, text: "   ");
        var job = _recruitment.AddJob(Guid.NewGuid());
        var quota = new CountingQuota();
        var handler = Handler(new FakeAiScoringAdapter(), quota);

        var result = await handler.HandleAsync(_candidate, new ScorePreviewRequest(cv.CvId, job.JobId));

        Assert.Equal("validation", result.Error.Code);
        Assert.Equal(0, quota.PreviewUsed);
    }
}

public sealed class ProcessScreeningHandlerTests
{
    private readonly FakeRecruitmentReader _recruitment = new();
    private readonly InMemoryScoreRepository _scores = new();
    private readonly InMemoryJobRepository _jobs = new();
    private readonly InMemoryCacheRepository _cache = new();

    private ProcessScreeningHandler Handler(IAiScoringService? scoring = null)
        => new(
            _recruitment,
            new SimpleAnonymizer(),
            new CachedScoringService(scoring ?? new FakeAiScoringAdapter(), _cache, new AiScreeningOptions(), NullLogger<CachedScoringService>.Instance),
            _scores,
            _jobs,
            TimeProvider.System,
            NullLogger<ProcessScreeningHandler>.Instance);

    private (Guid ApplicationId, ScreeningJob Batch) Seed(string cvText, int target = 1)
    {
        var job = _recruitment.AddJob(Guid.NewGuid());
        var applicationId = _recruitment.AddApplication(job, _recruitment.AddCv(Guid.NewGuid(), cvText));
        var batch = ScreeningJob.Start(job.JobId, job.HrOwnerUserId, target, DateTimeOffset.UtcNow);
        _jobs.Rows[batch.Id] = batch;
        return (applicationId, batch);
    }

    [Fact]
    public async Task Cham_thanh_cong_thi_Scored_va_lo_Done()
    {
        var (applicationId, batch) = Seed("3 năm kinh nghiệm C#, Docker");

        await Handler().HandleAsync(new ScreeningTask(applicationId, batch.Id));

        var score = _scores.Rows[applicationId];
        Assert.Equal(ScreeningStatus.Scored, score.ScreeningStatus);
        Assert.Equal(AdapterNames.Fake, score.AdapterUsed);
        Assert.InRange(score.Score!.Value, FakeAiScoringAdapter.MinScore, FakeAiScoringAdapter.MaxScore);
        Assert.NotNull(score.CacheId);
        Assert.Equal(1, batch.DoneCount);
        Assert.Equal(ScreeningJobStatus.Done, batch.Status);
    }

    [Fact]
    public async Task CV_rong_thi_Failed_va_FailedCount_tang()
    {
        var (applicationId, batch) = Seed("  ", target: 2);

        await Handler().HandleAsync(new ScreeningTask(applicationId, batch.Id));

        var score = _scores.Rows[applicationId];
        Assert.Equal(ScreeningStatus.Failed, score.ScreeningStatus);
        Assert.Null(score.Score);
        Assert.NotNull(score.FailureReason);
        Assert.Equal(1, batch.FailedCount);
        Assert.Equal(0, batch.DoneCount);
        Assert.Equal(ScreeningJobStatus.Running, batch.Status);
    }

    [Fact]
    public async Task Khong_doc_duoc_don_thi_Failed()
    {
        var (_, batch) = Seed("C#");
        var missing = Guid.NewGuid();

        await Handler().HandleAsync(new ScreeningTask(missing, batch.Id));

        Assert.Equal(ScreeningStatus.Failed, _scores.Rows[missing].ScreeningStatus);
        Assert.Equal(1, batch.FailedCount);
        Assert.Equal(ScreeningJobStatus.Done, batch.Status);
    }

    [Fact]
    public async Task Adapter_nem_loi_thi_Failed_khong_lam_chet_worker()
    {
        var (applicationId, batch) = Seed("C#");

        await Handler(StubScoring.Throwing()).HandleAsync(new ScreeningTask(applicationId, batch.Id));

        Assert.Equal(ScreeningStatus.Failed, _scores.Rows[applicationId].ScreeningStatus);
        Assert.Equal(1, batch.FailedCount);
    }

    [Fact]
    public async Task Cham_le_sau_khi_nop_don_khong_cap_nhat_lo_nao()
    {
        var (applicationId, _) = Seed("C#");

        await Handler().HandleAsync(new ScreeningTask(applicationId, Guid.Empty));

        Assert.Equal(ScreeningStatus.Scored, _scores.Rows[applicationId].ScreeningStatus);
        Assert.Equal(0, _jobs.ProgressCalls);
    }

    [Fact]
    public async Task Lo_da_huy_thi_bo_qua()
    {
        var (applicationId, batch) = Seed("C#", target: 3);
        batch.Cancel(DateTimeOffset.UtcNow);

        await Handler().HandleAsync(new ScreeningTask(applicationId, batch.Id));

        Assert.False(_scores.Rows.ContainsKey(applicationId));
        Assert.Equal(0, _jobs.ProgressCalls);
    }

    [Fact]
    public async Task Cham_lai_cung_noi_dung_thi_trung_cache()
    {
        var (applicationId, batch) = Seed("C#, PostgreSQL", target: 2);
        var scoring = StubScoring.Returning(AdapterNames.OpenAi, 70);
        var handler = Handler(scoring);

        await handler.HandleAsync(new ScreeningTask(applicationId, batch.Id));
        await handler.HandleAsync(new ScreeningTask(applicationId, batch.Id));

        Assert.Equal(1, scoring.Calls);
        Assert.Equal(1, _cache.Hits);
        Assert.Equal(ScreeningJobStatus.Done, batch.Status);
    }
}

public sealed class StartScreeningHandlerTests
{
    private readonly FakeRecruitmentReader _recruitment = new();
    private readonly InMemoryScoreRepository _scores = new();
    private readonly InMemoryJobRepository _jobs = new();
    private readonly InMemoryScreeningQueue _queue = new();

    private StartScreeningHandler Handler(IAiUsageQuota quota)
        => new(_recruitment, quota, _jobs, _scores, _queue, new AiScreeningOptions(), TimeProvider.System);

    [Fact]
    public async Task HR_chu_tin_tao_lo_va_xep_moi_don_vao_hang_doi()
    {
        var hr = Guid.NewGuid();
        var job = _recruitment.AddJob(hr);
        _recruitment.AddApplication(job, _recruitment.AddCv(Guid.NewGuid()));
        _recruitment.AddApplication(job, _recruitment.AddCv(Guid.NewGuid()));
        var quota = new CountingQuota();

        var result = await Handler(quota).HandleAsync(hr, new StartScreeningRequest(job.JobId));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.TargetCount);
        Assert.Equal("Queued", result.Value.Status);
        Assert.Equal(2, _queue.Count);
        Assert.Equal(1, quota.BatchUsed);
        Assert.All(_scores.Rows.Values, s => Assert.Equal(ScreeningStatus.Pending, s.ScreeningStatus));
    }

    [Fact]
    public async Task HR_khac_bi_cam()
    {
        var job = _recruitment.AddJob(Guid.NewGuid());

        var result = await Handler(new CountingQuota()).HandleAsync(Guid.NewGuid(), new StartScreeningRequest(job.JobId));

        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task Het_luot_lo_thi_QuotaExceeded()
    {
        var hr = Guid.NewGuid();
        var job = _recruitment.AddJob(hr);

        var result = await Handler(new CountingQuota(batchLimit: 0)).HandleAsync(hr, new StartScreeningRequest(job.JobId));

        Assert.Equal("quota_exceeded", result.Error.Code);
        Assert.Empty(_jobs.Rows);
    }

    [Fact]
    public async Task Tin_chua_co_don_thi_lo_Done_ngay()
    {
        var hr = Guid.NewGuid();
        var job = _recruitment.AddJob(hr);

        var result = await Handler(new CountingQuota()).HandleAsync(hr, new StartScreeningRequest(job.JobId));

        Assert.Equal("Done", result.Value.Status);
        Assert.Equal(0, _queue.Count);
    }
}

public sealed class InterviewQuestionHandlerTests
{
    private readonly FakeRecruitmentReader _recruitment = new();

    [Fact]
    public async Task Sinh_cau_hoi_mau_cho_HR_chu_tin()
    {
        var hr = Guid.NewGuid();
        var job = _recruitment.AddJob(hr);
        var applicationId = _recruitment.AddApplication(job, _recruitment.AddCv(Guid.NewGuid()));
        var handler = new GenerateQuestionsHandler(_recruitment, new SimpleAnonymizer(), new TemplateInterviewQuestionAdapter());

        var result = await handler.HandleAsync(hr, applicationId);

        Assert.True(result.IsSuccess);
        Assert.Equal(TemplateInterviewQuestionAdapter.QuestionCount, result.Value.Count);
        Assert.Contains(result.Value, q => q.Question.Contains("C#", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CV_rong_van_sinh_duoc_cau_hoi()
    {
        var hr = Guid.NewGuid();
        var job = _recruitment.AddJob(hr);
        var applicationId = _recruitment.AddApplication(job, _recruitment.AddCv(Guid.NewGuid(), text: ""));
        var handler = new GenerateQuestionsHandler(_recruitment, new SimpleAnonymizer(), new TemplateInterviewQuestionAdapter());

        var result = await handler.HandleAsync(hr, applicationId);

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value);
    }

    [Fact]
    public async Task HR_khac_khong_sinh_duoc()
    {
        var job = _recruitment.AddJob(Guid.NewGuid());
        var applicationId = _recruitment.AddApplication(job, _recruitment.AddCv(Guid.NewGuid()));
        var handler = new GenerateQuestionsHandler(_recruitment, new SimpleAnonymizer(), new TemplateInterviewQuestionAdapter());

        var result = await handler.HandleAsync(Guid.NewGuid(), applicationId);

        Assert.Equal("forbidden", result.Error.Code);
    }

    [Fact]
    public async Task Luu_roi_doc_lai_dung_thu_tu()
    {
        var hr = Guid.NewGuid();
        var job = _recruitment.AddJob(hr);
        var applicationId = _recruitment.AddApplication(job, _recruitment.AddCv(Guid.NewGuid()));
        var interviewId = Guid.NewGuid();
        _recruitment.InterviewToApplication[interviewId] = applicationId;

        using var db = Db.Create();
        var handler = new InterviewQuestionsHandler(
            _recruitment,
            new Infrastructure.Persistence.EfInterviewQuestionRepository(db),
            TimeProvider.System);

        var saved = await handler.SaveAsync(hr, interviewId, new SaveQuestionsRequest(
        [
            new InterviewQuestionDto("technical", "Cau 1?", "AI"),
            new InterviewQuestionDto("Behavioral", "Cau 2?", "Manual"),
        ]));
        await handler.SaveAsync(hr, interviewId, new SaveQuestionsRequest(
        [
            new InterviewQuestionDto("situational", "Cau moi?", "manual"),
            new InterviewQuestionDto("technical", "Cau 1?", "AI"),
        ]));
        var listed = await handler.ListAsync(hr, interviewId);

        Assert.True(saved.IsSuccess);
        Assert.Equal(["Cau moi?", "Cau 1?"], listed.Value.Select(q => q.Question).ToArray());
        Assert.Equal("Manual", listed.Value[0].Source);
    }

    [Fact]
    public async Task Category_sai_thi_validation()
    {
        var hr = Guid.NewGuid();
        var job = _recruitment.AddJob(hr);
        var applicationId = _recruitment.AddApplication(job, _recruitment.AddCv(Guid.NewGuid()));
        var interviewId = Guid.NewGuid();
        _recruitment.InterviewToApplication[interviewId] = applicationId;
        using var db = Db.Create();
        var handler = new InterviewQuestionsHandler(_recruitment, new Infrastructure.Persistence.EfInterviewQuestionRepository(db), TimeProvider.System);

        var result = await handler.SaveAsync(hr, interviewId, new SaveQuestionsRequest([new InterviewQuestionDto("trivia", "?", "AI")]));

        Assert.Equal("validation", result.Error.Code);
    }
}
