using System.Diagnostics;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Ports;
using ATS.AiScreening.Infrastructure.Scoring;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ATS.AiScreening.Tests;

/// <summary>Fallback 3 tang (ai-integration.md muc 5): "cuoi cung luon co diem".</summary>
public sealed class PipelineTests
{
    private static AiScoringPipeline Pipeline(IAiScoringService[] tiers, TimeSpan? budget = null)
        => new(tiers, new KeywordScoringAdapter(), budget ?? TimeSpan.FromSeconds(30), NullLogger<AiScoringPipeline>.Instance);

    [Fact]
    public async Task Tang_1_loi_thi_dung_tang_2()
    {
        var openAi = StubScoring.Throwing();
        var embedding = StubScoring.Returning(AdapterNames.Embedding, 61);
        var pipeline = Pipeline([openAi, embedding]);

        var result = await pipeline.ScoreAsync(await Cv.AnonAsync("C# va Docker"), Cv.Jd());

        Assert.Equal(AdapterNames.Embedding, result.AdapterUsed);
        Assert.Equal(61, result.Score.Value);
        Assert.Equal(1, openAi.Calls);
    }

    [Fact]
    public async Task Tang_1_thanh_cong_thi_khong_goi_tang_sau()
    {
        var openAi = StubScoring.Returning(AdapterNames.OpenAi, 88);
        var embedding = StubScoring.Returning(AdapterNames.Embedding);
        var pipeline = Pipeline([openAi, embedding]);

        var result = await pipeline.ScoreAsync(await Cv.AnonAsync("C#"), Cv.Jd());

        Assert.Equal(AdapterNames.OpenAi, result.AdapterUsed);
        Assert.Equal(0, embedding.Calls);
    }

    [Fact]
    public async Task Moi_tang_AI_loi_thi_Keyword_van_cho_diem()
    {
        var pipeline = Pipeline([StubScoring.Throwing(), StubScoring.Throwing()]);

        var result = await pipeline.ScoreAsync(await Cv.AnonAsync("C#, PostgreSQL"), Cv.Jd());

        Assert.Equal(AdapterNames.Keyword, result.AdapterUsed);
        Assert.InRange(result.Score.Value, 0, 100);
    }

    [Fact]
    public async Task Khong_co_tang_AI_nao_thi_chi_chay_Keyword()
    {
        var pipeline = Pipeline([]);

        var result = await pipeline.ScoreAsync(await Cv.AnonAsync("C#"), Cv.Jd());

        Assert.Equal(AdapterNames.Keyword, result.AdapterUsed);
    }

    [Fact]
    public async Task Qua_tran_thoi_gian_thi_roi_thang_xuong_Keyword()
    {
        var slow = StubScoring.Hanging();
        var embedding = StubScoring.Returning(AdapterNames.Embedding);
        var pipeline = Pipeline([slow, embedding], TimeSpan.FromMilliseconds(200));
        var watch = Stopwatch.StartNew();

        var result = await pipeline.ScoreAsync(await Cv.AnonAsync("C#"), Cv.Jd());

        Assert.Equal(AdapterNames.Keyword, result.AdapterUsed);
        Assert.Equal(0, embedding.Calls); // het ngan sach chung thi khong thu tang AI tiep theo
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"Mat {watch.Elapsed} — timeout khong co tac dung.");
    }

    [Fact]
    public async Task Adapter_khong_ton_trong_token_van_bi_cat_dung_tran()
    {
        var pipeline = Pipeline([StubScoring.IgnoringCancellation()], TimeSpan.FromMilliseconds(200));
        var watch = Stopwatch.StartNew();

        var result = await pipeline.ScoreAsync(await Cv.AnonAsync("C#"), Cv.Jd());

        Assert.Equal(AdapterNames.Keyword, result.AdapterUsed);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"Mat {watch.Elapsed} — WaitAsync khong cat duoc.");
    }

    [Fact]
    public async Task Nguoi_goi_huy_thi_nem_loi_huy_chu_khong_roi_xuong()
    {
        var pipeline = Pipeline([StubScoring.Hanging()]);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var cv = await Cv.AnonAsync("C#");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pipeline.ScoreAsync(cv, Cv.Jd(), cts.Token));
    }
}

public sealed class InterviewQuestionPipelineTests
{
    [Fact]
    public async Task OpenAI_loi_thi_dung_bo_cau_hoi_mau()
    {
        var pipeline = new InterviewQuestionPipeline(
            new ThrowingGenerator(),
            new TemplateInterviewQuestionAdapter(),
            TimeSpan.FromSeconds(5),
            NullLogger<InterviewQuestionPipeline>.Instance);

        var questions = await pipeline.GenerateAsync(await Cv.AnonAsync("C#"), Cv.Jd());

        Assert.Equal(TemplateInterviewQuestionAdapter.QuestionCount, questions.Count);
        Assert.All(questions, q => Assert.Equal("Template", q.Source));
    }

    private sealed class ThrowingGenerator : ATS.AiScreening.Domain.Ports.IInterviewQuestionGenerator
    {
        public Task<IReadOnlyList<InterviewQuestion>> GenerateAsync(AnonymizedCv cv, JobRequirement jd, CancellationToken ct = default)
            => throw new InvalidOperationException("het quota OpenAI");
    }
}
