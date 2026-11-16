using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ATS.IntegrationTests.EndToEnd;

/// <summary>
/// Pha tich hop: mot vong doi tuyen dung DAY DU qua HTTP — dang ky → dang tin → upload CV → preview AI →
/// nop don → Worker cham → HR xep hang → sang loc lo → cau hoi + phong van → danh gia → Hired.
/// </summary>
public sealed class ApplyAndScreeningFlowTests : IClassFixture<E2eApiFactory>
{
    private const string Hr = "HR";
    private const string Candidate = "Candidate";

    private static readonly string[] CvParagraphs =
    [
        "NGUYỄN VĂN AN",
        "Email: an.nguyen@example.com | SĐT: 0912 345 678",
        "Backend developer, 4 năm kinh nghiệm C#, ASP.NET Core, PostgreSQL, Docker.",
    ];

    private readonly E2eApiFactory _factory;

    public ApplyAndScreeningFlowTests(E2eApiFactory factory) => _factory = factory;

    // ---------------------------------------------------------------------------------------
    // 1. Luong chinh
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Vong_doi_day_du_tu_dang_ky_toi_Hired()
    {
        // Hang doi la singleton cua fixture: don cua test khac (cung class, chay tuan tu) con nam
        // trong do. Rut sach truoc de so viec Worker xu ly duoi day chi la cua test nay.
        await _factory.RunWorkerAsync();
        var (hr, _, _) = await _factory.RegisterAsync(Hr, "HR Mot");
        var (candidate, _, _) = await _factory.RegisterAsync(Candidate, "Ung Vien Mot");

        // HR tao tin nhap roi dang.
        var jobId = await CreateOpenJobAsync(hr, $"Backend .NET {Guid.NewGuid():N}");

        // Ung vien upload CV docx that → extractor that phai trich duoc chu.
        var cv = await UploadCvAsync(candidate, DocxBuilder.Build(CvParagraphs), "CV Backend");
        Assert.True(cv.GetProperty("hasText").GetBoolean(), "OpenXmlTextExtractor khong trich duoc chu tu docx hop le.");
        var cvId = cv.GetProperty("id").GetGuid();

        // Preview: lan 2 trung cache, KHONG tru them luot.
        var preview1 = await PostJsonAsync(candidate, "/api/ai/score-preview", new { cvId, jobId });
        var preview2 = await PostJsonAsync(candidate, "/api/ai/score-preview", new { cvId, jobId });
        Assert.Equal("Fake", preview1.GetProperty("adapterUsed").GetString());
        Assert.False(preview1.GetProperty("cacheHit").GetBoolean());
        Assert.True(preview2.GetProperty("cacheHit").GetBoolean());
        Assert.Equal(preview1.GetProperty("score").GetInt32(), preview2.GetProperty("score").GetInt32());
        Assert.Equal(
            preview1.GetProperty("previewRemainingToday").GetInt32(),
            preview2.GetProperty("previewRemainingToday").GetInt32());
        var quota = await candidate.GetFromJsonAsync<JsonElement>("/api/me/ai-quota");
        Assert.Equal(1, quota.GetProperty("previewUsedToday").GetInt32());

        // Nop don → trigger ghi Pending + xep hang doi.
        var applied = await candidate.PostAsJsonAsync("/api/applications", new { jobId, cvId });
        Assert.Equal(HttpStatusCode.Created, applied.StatusCode);
        var applicationId = (await applied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Worker cham.
        Assert.Equal(1, await _factory.RunWorkerAsync());

        // HR thay danh sach xep hang co diem + nguon diem.
        var ranked = await hr.GetFromJsonAsync<JsonElement>($"/api/jobs/{jobId}/applications");
        var row = Assert.Single(ranked.EnumerateArray());
        Assert.Equal(applicationId, row.GetProperty("id").GetGuid());
        Assert.Equal("Scored", row.GetProperty("screeningStatus").GetString());
        Assert.Equal("Fake", row.GetProperty("aiAdapterUsed").GetString());
        Assert.InRange(row.GetProperty("aiScore").GetInt32(), 40, 95);
        Assert.Equal("Ung Vien Mot", row.GetProperty("candidateName").GetString());

        // Diem cham le trung cache cua preview (cung CV + JD) → cung so diem.
        Assert.Equal(preview1.GetProperty("score").GetInt32(), row.GetProperty("aiScore").GetInt32());

        // HR sang loc lo → 202 → Worker → polling toi Done.
        var started = await hr.PostAsJsonAsync("/api/screening-jobs", new { jobId });
        Assert.Equal(HttpStatusCode.Accepted, started.StatusCode);
        var screeningJobId = (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(1, await _factory.RunWorkerAsync());
        var progress = await hr.GetFromJsonAsync<JsonElement>($"/api/screening-jobs/{screeningJobId}");
        Assert.Equal("Done", progress.GetProperty("status").GetString());
        Assert.Equal(1, progress.GetProperty("doneCount").GetInt32());
        Assert.Equal(0, progress.GetProperty("failedCount").GetInt32());

        // Applied → Screening.
        await PatchStatusAsync(hr, applicationId, "Screening", HttpStatusCode.NoContent);

        // HR sinh cau hoi (Template khi Fake).
        var generated = await PostJsonAsync(hr, $"/api/applications/{applicationId}/questions", new { });
        Assert.Equal(10, generated.GetArrayLength());

        // Hen phong van: SMTP chet van 201, EmailSent=false; don tu sang Interview.
        var attemptsBefore = _factory.Email.Attempts;
        var scheduled = await hr.PostAsJsonAsync($"/api/applications/{applicationId}/interviews", new
        {
            scheduledAt = DateTimeOffset.UtcNow.AddDays(3),
            mode = "online",
            locationOrLink = "https://meet.example/abc",
            interviewerName = "Tran Phong Van",
        });
        Assert.Equal(HttpStatusCode.Created, scheduled.StatusCode);
        var interview = await scheduled.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(interview.GetProperty("emailSent").GetBoolean());
        Assert.True(_factory.Email.Attempts > attemptsBefore, "Khong he thu gui email moi phong van.");
        var interviewId = interview.GetProperty("id").GetGuid();
        var detail = await hr.GetFromJsonAsync<JsonElement>($"/api/applications/{applicationId}");
        Assert.Equal("Interview", detail.GetProperty("status").GetString());

        // Luu bo cau hoi (lay 3 cau dau + 1 cau HR tu viet) roi doc lai dung thu tu.
        var chosen = generated.EnumerateArray().Take(3)
            .Select(q => new
            {
                category = q.GetProperty("category").GetString(),
                question = q.GetProperty("question").GetString(),
                source = q.GetProperty("source").GetString(),
            })
            .Append(new { category = (string?)"situational", question = (string?)"Neu khach hang doi yeu cau sat ngay release?", source = (string?)"Manual" })
            .ToArray();
        var saved = await hr.PutAsJsonAsync($"/api/interviews/{interviewId}/questions", new { questions = chosen });
        Assert.Equal(HttpStatusCode.NoContent, saved.StatusCode);
        var stored = await hr.GetFromJsonAsync<JsonElement>($"/api/interviews/{interviewId}/questions");
        Assert.Equal(4, stored.GetArrayLength());
        Assert.Equal("Manual", stored[3].GetProperty("source").GetString());

        // Danh gia sau phong van.
        var evaluation = await hr.PostAsJsonAsync($"/api/interviews/{interviewId}/evaluations", new
        {
            rating = 5,
            comment = "Tot",
            recommendation = "Hire",
        });
        Assert.Equal(HttpStatusCode.Created, evaluation.StatusCode);

        // Interview → Hired.
        await PatchStatusAsync(hr, applicationId, "Hired", HttpStatusCode.NoContent);

        // Ung vien thay ket qua cuoi + diem AI kem nguon.
        var mine = await candidate.GetFromJsonAsync<JsonElement>("/api/me/applications");
        var myApplication = Assert.Single(mine.EnumerateArray());
        Assert.Equal("Hired", myApplication.GetProperty("status").GetString());
        Assert.Equal("Fake", myApplication.GetProperty("aiAdapterUsed").GetString());
        Assert.Equal("Scored", myApplication.GetProperty("screeningStatus").GetString());
    }

    [Fact]
    public async Task Nhay_coc_trang_thai_thi_400()
    {
        var (hr, _, applicationId, _) = await SeedApplicationAsync();

        await PatchStatusAsync(hr, applicationId, "Hired", HttpStatusCode.BadRequest);
        await PatchStatusAsync(hr, applicationId, "Interview", HttpStatusCode.BadRequest);

        var detail = await hr.GetFromJsonAsync<JsonElement>($"/api/applications/{applicationId}");
        Assert.Equal("Applied", detail.GetProperty("status").GetString());
    }

    // ---------------------------------------------------------------------------------------
    // 2. Phan quyen cheo (RB8)
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Ung_vien_goi_endpoint_cua_HR_thi_403()
    {
        var (_, candidate, applicationId, jobId) = await SeedApplicationAsync();

        var createJob = await candidate.PostAsJsonAsync("/api/jobs", new
        {
            title = "Hack",
            description = "x",
            employmentType = "FullTime",
            publish = true,
        });
        var list = await candidate.GetAsync($"/api/jobs/{jobId}/applications");
        var status = await candidate.PatchAsJsonAsync($"/api/applications/{applicationId}/status", new { status = "Hired" });
        var batch = await candidate.PostAsJsonAsync("/api/screening-jobs", new { jobId });
        var questions = await candidate.PostAsync($"/api/applications/{applicationId}/questions", null);

        Assert.Equal(HttpStatusCode.Forbidden, createJob.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, list.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, status.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, batch.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, questions.StatusCode);
    }

    [Fact]
    public async Task HR_khong_chu_tin_khong_xem_va_khong_thao_tac_duoc_don()
    {
        var (_, _, applicationId, jobId) = await SeedApplicationAsync();
        var (otherHr, _, _) = await _factory.RegisterAsync(Hr, "HR Khac");

        var list = await otherHr.GetAsync($"/api/jobs/{jobId}/applications");
        var detail = await otherHr.GetAsync($"/api/applications/{applicationId}");
        var status = await otherHr.PatchAsJsonAsync($"/api/applications/{applicationId}/status", new { status = "Screening" });
        var batch = await otherHr.PostAsJsonAsync("/api/screening-jobs", new { jobId });
        var score = await otherHr.GetAsync($"/api/applications/{applicationId}/ai-score");
        var questions = await otherHr.PostAsync($"/api/applications/{applicationId}/questions", null);

        foreach (var response in new[] { list, detail, status, batch, score, questions })
        {
            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
        }
    }

    [Fact]
    public async Task Ung_vien_A_khong_xem_duoc_don_cua_ung_vien_B()
    {
        var (_, _, applicationOfB, _) = await SeedApplicationAsync();
        var (candidateA, _, _) = await _factory.RegisterAsync(Candidate, "Ung Vien A");

        var detail = await candidateA.GetAsync($"/api/applications/{applicationOfB}");
        var cvFile = await candidateA.GetAsync($"/api/applications/{applicationOfB}/cv-file");
        var interviews = await candidateA.GetAsync($"/api/applications/{applicationOfB}/interviews");
        var score = await candidateA.GetAsync($"/api/applications/{applicationOfB}/ai-score");

        foreach (var response in new[] { detail, cvFile, interviews, score })
        {
            Assert.Contains(response.StatusCode, new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound });
        }

        var mine = await candidateA.GetFromJsonAsync<JsonElement>("/api/me/applications");
        Assert.Equal(0, mine.GetArrayLength());
    }

    [Fact]
    public async Task Ung_vien_khong_preview_duoc_bang_CV_cua_nguoi_khac()
    {
        var (_, _, _, jobId) = await SeedApplicationAsync();
        var (owner, _, _) = await _factory.RegisterAsync(Candidate, "Chu CV");
        var cvId = (await UploadCvAsync(owner, DocxBuilder.Build(CvParagraphs), "CV")).GetProperty("id").GetGuid();
        var (stranger, _, _) = await _factory.RegisterAsync(Candidate, "Nguoi La");

        var response = await stranger.PostAsJsonAsync("/api/ai/score-preview", new { cvId, jobId });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Quen_mat_khau_voi_email_la_van_204()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/auth/forgot-password",
            new { email = $"khong-ton-tai-{Guid.NewGuid():N}@e2e.local" });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Quen_mat_khau_voi_email_that_khi_SMTP_chet_van_204()
    {
        var (_, _, email) = await _factory.RegisterAsync(Candidate, "Quen Mat Khau");

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/forgot-password", new { email });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    // ---------------------------------------------------------------------------------------
    // 3. RB9 — diem AI Failed khong chan nghiep vu
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Don_cham_Failed_van_hien_cuoi_bang_va_van_chuyen_trang_thai_duoc()
    {
        await _factory.RunWorkerAsync(); // rut viec con sot cua test khac trong fixture
        var (hr, _, _) = await _factory.RegisterAsync(Hr, "HR RB9");
        var jobId = await CreateOpenJobAsync(hr, $"Data {Guid.NewGuid():N}");

        // Ung vien 1: CV co chu → Scored.
        var (good, _, _) = await _factory.RegisterAsync(Candidate, "Co Chu");
        var goodCv = (await UploadCvAsync(good, DocxBuilder.Build(CvParagraphs), "CV tot")).GetProperty("id").GetGuid();
        var goodApp = await ApplyAsync(good, jobId, goodCv);

        // Ung vien 2: docx KHONG co chu (scan/anh) → HasText=false, van nop duoc, AI cham Failed.
        var (blank, _, _) = await _factory.RegisterAsync(Candidate, "Khong Chu");
        var blankCvDto = await UploadCvAsync(blank, DocxBuilder.Build(), "CV scan");
        Assert.False(blankCvDto.GetProperty("hasText").GetBoolean());
        var blankApp = await ApplyAsync(blank, jobId, blankCvDto.GetProperty("id").GetGuid());

        Assert.Equal(2, await _factory.RunWorkerAsync());

        var ranked = (await hr.GetFromJsonAsync<JsonElement>($"/api/jobs/{jobId}/applications")).EnumerateArray().ToArray();
        Assert.Equal(2, ranked.Length);
        Assert.Equal(goodApp, ranked[0].GetProperty("id").GetGuid());
        Assert.Equal(blankApp, ranked[1].GetProperty("id").GetGuid());
        Assert.Equal("Failed", ranked[1].GetProperty("screeningStatus").GetString());
        Assert.Equal(JsonValueKind.Null, ranked[1].GetProperty("aiScore").ValueKind);

        // Lo sang loc: don Failed tinh vao failedCount, lo van Done.
        var started = await hr.PostAsJsonAsync("/api/screening-jobs", new { jobId });
        var screeningJobId = (await started.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        await _factory.RunWorkerAsync();
        var progress = await hr.GetFromJsonAsync<JsonElement>($"/api/screening-jobs/{screeningJobId}");
        Assert.Equal("Done", progress.GetProperty("status").GetString());
        Assert.Equal(1, progress.GetProperty("doneCount").GetInt32());
        Assert.Equal(1, progress.GetProperty("failedCount").GetInt32());

        // HR van chuyen trang thai don Failed duoc.
        await PatchStatusAsync(hr, blankApp, "Screening", HttpStatusCode.NoContent);
        await PatchStatusAsync(hr, blankApp, "Rejected", HttpStatusCode.NoContent);
        var detail = await hr.GetFromJsonAsync<JsonElement>($"/api/applications/{blankApp}");
        Assert.Equal("Rejected", detail.GetProperty("status").GetString());
        Assert.Equal("Failed", detail.GetProperty("screeningStatus").GetString());
    }

    // ---------------------------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------------------------

    private async Task<(HttpClient Hr, HttpClient Candidate, Guid ApplicationId, Guid JobId)> SeedApplicationAsync()
    {
        var (hr, _, _) = await _factory.RegisterAsync(Hr, "HR Seed");
        var (candidate, _, _) = await _factory.RegisterAsync(Candidate, "Ung Vien Seed");
        var jobId = await CreateOpenJobAsync(hr, $"Tin {Guid.NewGuid():N}");
        var cvId = (await UploadCvAsync(candidate, DocxBuilder.Build(CvParagraphs), "CV")).GetProperty("id").GetGuid();
        var applicationId = await ApplyAsync(candidate, jobId, cvId);
        return (hr, candidate, applicationId, jobId);
    }

    private static async Task<Guid> CreateOpenJobAsync(HttpClient hr, string title)
    {
        var created = await hr.PostAsJsonAsync("/api/jobs", new
        {
            title,
            description = "Phat trien API tuyen dung",
            requirements = "- C#, ASP.NET Core\n- PostgreSQL\n- Docker\n- 3 năm kinh nghiệm",
            location = "Ha Noi",
            employmentType = "FullTime",
            publish = false,
        });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var job = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Draft", job.GetProperty("status").GetString());
        var jobId = job.GetProperty("id").GetGuid();

        var published = await hr.PostAsync($"/api/jobs/{jobId}/publish", null);
        Assert.Equal(HttpStatusCode.NoContent, published.StatusCode);
        return jobId;
    }

    private static async Task<JsonElement> UploadCvAsync(HttpClient candidate, byte[] docx, string name)
    {
        using var form = DocxBuilder.UploadForm(docx, name);
        var response = await candidate.PostAsync("/api/me/cvs", form);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<Guid> ApplyAsync(HttpClient candidate, Guid jobId, Guid cvId)
    {
        var response = await candidate.PostAsJsonAsync("/api/applications", new { jobId, cvId });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> PostJsonAsync(HttpClient client, string url, object body)
    {
        var response = await client.PostAsJsonAsync(url, body);
        Assert.True(response.IsSuccessStatusCode, $"POST {url} → {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task PatchStatusAsync(HttpClient hr, Guid applicationId, string status, HttpStatusCode expected)
    {
        var response = await hr.PatchAsJsonAsync($"/api/applications/{applicationId}/status", new { status, note = (string?)null });
        Assert.True(
            response.StatusCode == expected,
            $"PATCH status {status}: mong {expected}, nhan {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}");
    }
}
