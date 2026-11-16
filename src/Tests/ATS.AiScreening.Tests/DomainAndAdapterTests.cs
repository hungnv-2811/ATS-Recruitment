using ATS.AiScreening.Application.Ports;
using ATS.AiScreening.Application.Scoring;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Entities;
using ATS.AiScreening.Infrastructure.Scoring;
using Xunit;

namespace ATS.AiScreening.Tests;

public sealed class ScreeningJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 16, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Lo_moi_o_trang_thai_Queued()
    {
        var job = ScreeningJob.Start(Guid.NewGuid(), Guid.NewGuid(), 3, Now);

        Assert.Equal(ScreeningJobStatus.Queued, job.Status);
        Assert.Null(job.FinishedAt);
    }

    [Fact]
    public void Tien_do_chuyen_Running_roi_tu_Done_khi_du_so()
    {
        var job = ScreeningJob.Start(Guid.NewGuid(), Guid.NewGuid(), 3, Now);

        job.RecordDone(Now);
        Assert.Equal(ScreeningJobStatus.Running, job.Status);

        job.RecordFailed(Now);
        job.RecordDone(Now.AddMinutes(1));

        Assert.Equal(2, job.DoneCount);
        Assert.Equal(1, job.FailedCount);
        Assert.Equal(ScreeningJobStatus.Done, job.Status);
        Assert.Equal(Now.AddMinutes(1), job.FinishedAt);
    }

    [Fact]
    public void Lo_rong_Done_ngay_khi_tao()
    {
        var job = ScreeningJob.Start(Guid.NewGuid(), Guid.NewGuid(), 0, Now);

        Assert.Equal(ScreeningJobStatus.Done, job.Status);
    }

    [Fact]
    public void Khong_dem_vuot_TargetCount()
    {
        var job = ScreeningJob.Start(Guid.NewGuid(), Guid.NewGuid(), 2, Now);

        job.RecordProgress(done: 5, failed: 5, Now);

        Assert.Equal(2, job.DoneCount);
        Assert.Equal(0, job.FailedCount);
        Assert.Equal(ScreeningJobStatus.Done, job.Status);
    }

    [Fact]
    public void Lo_da_huy_khong_nhan_tien_do_nua()
    {
        var job = ScreeningJob.Start(Guid.NewGuid(), Guid.NewGuid(), 2, Now);

        job.Cancel(Now);
        job.RecordDone(Now);

        Assert.Equal(ScreeningJobStatus.Cancelled, job.Status);
        Assert.Equal(0, job.DoneCount);
    }
}

public sealed class AiScoreTests
{
    [Fact]
    public void Failed_xoa_diem_cu_va_ghi_ly_do()
    {
        var score = AiScore.CreatePending(Guid.NewGuid(), DateTimeOffset.UtcNow);
        score.MarkScored(new ScreeningResult(new Score(80), "ok", ["a"], [], "OpenAI"), "gpt-4o-mini", Guid.NewGuid(), DateTimeOffset.UtcNow);

        score.MarkFailed("CV rong", DateTimeOffset.UtcNow);

        Assert.Equal(ScreeningStatus.Failed, score.ScreeningStatus);
        Assert.Null(score.Score);
        Assert.Null(score.AdapterUsed);
        Assert.Equal("CV rong", score.FailureReason);
    }

    [Fact]
    public void Xep_lai_hang_doi_giu_diem_cu_den_khi_co_diem_moi()
    {
        var score = AiScore.CreatePending(Guid.NewGuid(), DateTimeOffset.UtcNow);
        score.MarkScored(new ScreeningResult(new Score(66), "ok", [], [], "Keyword"), "x", null, DateTimeOffset.UtcNow);

        score.MarkPending(DateTimeOffset.UtcNow);

        Assert.Equal(ScreeningStatus.Pending, score.ScreeningStatus);
        Assert.Equal(66, score.Score);
    }
}

public sealed class KeywordScoringAdapterTests
{
    private readonly KeywordScoringAdapter _adapter = new();

    [Fact]
    public async Task Khop_het_ky_nang_bat_buoc_thi_100()
    {
        var result = await _adapter.ScoreAsync(await Cv.AnonAsync("C#, PostgreSQL, Docker"), Cv.Jd());

        Assert.Equal(100, result.Score.Value);
        Assert.Equal(AdapterNames.Keyword, result.AdapterUsed);
        Assert.Empty(result.Gaps);
    }

    [Fact]
    public async Task Khop_mot_phan_thi_ti_le_va_liet_ke_ky_nang_thieu()
    {
        var result = await _adapter.ScoreAsync(await Cv.AnonAsync("Toi biet C# va Postgres"), Cv.Jd());

        Assert.Equal(67, result.Score.Value);
        Assert.Equal(["Thiếu Docker"], result.Gaps);
    }

    [Fact]
    public async Task Thieu_nam_kinh_nghiem_bi_tru_10()
    {
        var cv = await Cv.AnonAsync("2 năm kinh nghiệm C#, PostgreSQL, Docker");

        var result = await _adapter.ScoreAsync(cv, Cv.Jd(minYears: 3));

        Assert.Equal(90, result.Score.Value);
        Assert.Contains(result.Gaps, g => g.Contains("3 năm", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Ky_nang_uu_tien_cong_toi_da_10()
    {
        var cv = await Cv.AnonAsync("C#, Redis, Kafka, RabbitMQ");

        var result = await _adapter.ScoreAsync(cv, Cv.Jd(required: ["C#", "Docker"], nice: ["Redis", "Kafka", "RabbitMQ"]));

        Assert.Equal(60, result.Score.Value);
    }

    [Fact]
    public async Task JD_khong_co_ky_nang_thi_diem_trung_tinh()
    {
        var result = await _adapter.ScoreAsync(await Cv.AnonAsync("Bat ky"), Cv.Jd(required: []));

        Assert.Equal(KeywordScoringAdapter.NeutralScore, result.Score.Value);
    }
}

public sealed class FakeAiScoringAdapterTests
{
    private readonly FakeAiScoringAdapter _adapter = new();

    [Fact]
    public async Task Cung_dau_vao_cung_diem()
    {
        var cv = await Cv.AnonAsync("C#, Docker, 3 năm kinh nghiệm");

        var a = await _adapter.ScoreAsync(cv, Cv.Jd());
        var b = await _adapter.ScoreAsync(cv, Cv.Jd());

        Assert.Equal(a, b with { Strengths = a.Strengths, Gaps = a.Gaps });
        Assert.Equal(a.Score, b.Score);
        Assert.Equal(AdapterNames.Fake, a.AdapterUsed);
    }

    [Fact]
    public async Task Diem_luon_trong_khoang_40_95()
    {
        for (var i = 0; i < 50; i++)
        {
            var result = await _adapter.ScoreAsync(await Cv.AnonAsync($"CV so {i} biet C#"), Cv.Jd());
            Assert.InRange(result.Score.Value, FakeAiScoringAdapter.MinScore, FakeAiScoringAdapter.MaxScore);
        }
    }

    [Fact]
    public async Task Noi_dung_khac_thuong_cho_diem_khac()
    {
        var scores = new HashSet<int>();
        for (var i = 0; i < 20; i++)
        {
            scores.Add((await _adapter.ScoreAsync(await Cv.AnonAsync($"CV {i}"), Cv.Jd())).Score.Value);
        }

        Assert.True(scores.Count > 5, "Fake phai phan tan diem, khong tra mot hang so.");
    }
}

public sealed class TemplateQuestionTests
{
    [Fact]
    public async Task Sinh_10_cau_dung_ba_nhom()
    {
        var questions = await new TemplateInterviewQuestionAdapter()
            .GenerateAsync(await Cv.AnonAsync("Docker"), Cv.Jd(required: ["C#", "Docker"]));

        Assert.Equal(10, questions.Count);
        Assert.Equal(6, questions.Count(q => q.Category == "technical"));
        Assert.Equal(2, questions.Count(q => q.Category == "behavioral"));
        Assert.Equal(2, questions.Count(q => q.Category == "situational"));
        Assert.Contains("Docker", questions[0].Question, StringComparison.Ordinal); // ky nang CV co nhac toi dung dau
    }
}

public sealed class JobRequirementParserTests
{
    private static JobInfo Job(string requirements, string description = "")
        => new(Guid.NewGuid(), Guid.NewGuid(), "Backend .NET", description, requirements, "Open");

    [Fact]
    public void Tach_theo_dong_dau_phay_va_gach_dau_dong()
    {
        var jd = JobRequirementParser.Parse(Job("""
            - Thành thạo C#, ASP.NET Core
            - SQL Server; Redis
            * Có kinh nghiệm Docker
            1. Giao tiếp tốt
            """));

        Assert.Equal(["C#", "ASP.NET Core", "SQL", "SQL Server", "Redis", "Docker", "Giao tiếp tốt"], jd.RequiredSkills);
    }

    [Fact]
    public void Lay_so_nam_kinh_nghiem_lon_nhat()
    {
        var jd = JobRequirementParser.Parse(Job("Tối thiểu 3 năm kinh nghiệm .NET\nTrong đó 2 năm với PostgreSQL"));

        Assert.Equal(3, jd.MinYearsExperience);
        Assert.DoesNotContain(jd.RequiredSkills, s => s.Contains("năm", StringComparison.Ordinal));
    }

    [Fact]
    public void Dong_uu_tien_vao_NiceToHave()
    {
        var jd = JobRequirementParser.Parse(Job("""
            Yêu cầu:
            - C#, PostgreSQL
            Ưu tiên:
            - Kubernetes
            - Biết Kafka là lợi thế
            """));

        Assert.Equal(["C#", "PostgreSQL"], jd.RequiredSkills);
        Assert.Equal(["Kubernetes", "Kafka"], jd.NiceToHaveSkills);
    }

    [Fact]
    public void Cau_dai_khong_co_ky_nang_bi_bo_qua()
    {
        var jd = JobRequirementParser.Parse(Job("Có tinh thần trách nhiệm cao và sẵn sàng học hỏi công nghệ mới mỗi ngày"));

        Assert.Empty(jd.RequiredSkills);
    }

    [Fact]
    public void Khong_co_so_nam_trong_requirements_thi_doc_tu_description()
    {
        var jd = JobRequirementParser.Parse(Job("C#", description: "Cần 4+ years experience"));

        Assert.Equal(4, jd.MinYearsExperience);
    }
}
