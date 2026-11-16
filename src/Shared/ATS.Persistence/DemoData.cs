using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ATS.Persistence;

/// <summary>
/// Bo du lieu demo — TAT DINH: cung mot code luon sinh cung Id, cung so dong (chi moc thoi gian
/// lay tu <c>now</c>). Nho vay seed lai la no-op va test dem duoc so dong.
/// </summary>
/// <remarks>
/// 3 HR, 8 ung vien (12 CV PDF), 10 tin (8 Open, 1 Draft, 1 Closed), 30 don du 5 trang thai,
/// diem AI du 4 adapter + Failed + Pending, 6 dong cache, 1 lo sang loc, 3 buoi phong van,
/// 2 danh gia, 3 cau hoi. Mat khau moi tai khoan: <see cref="DatabaseSeeder.DemoPassword"/>.
/// </remarks>
internal sealed class DemoData
{
    public const int ApplicationCount = 30;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.General);

    private DemoData(DateTimeOffset now) => Now = now;

    public DateTimeOffset Now { get; }

    public List<UserRow> Users { get; } = [];

    public List<HrRow> HrProfiles { get; } = [];

    public List<CandidateRow> Candidates { get; } = [];

    public List<CvRow> Cvs { get; } = [];

    public List<JobRow> Jobs { get; } = [];

    public List<ApplicationRow> Applications { get; } = [];

    public List<CacheRow> Cache { get; } = [];

    public List<ScoreRow> Scores { get; } = [];

    public List<ScreeningJobRow> ScreeningJobs { get; } = [];

    public List<InterviewRow> Interviews { get; } = [];

    public List<EvaluationRow> Evaluations { get; } = [];

    public List<QuestionRow> Questions { get; } = [];

    /// <summary>Id co dinh dang 5eed00TT-0000-0000-0000-NNNNNNNNNNNN (TT = loai bang).</summary>
    public static Guid Id(int table, int n) => Guid.Parse($"5eed00{table:x2}-0000-0000-0000-{n:D12}");

    public static DemoData Build(DateTimeOffset now)
    {
        var d = new DemoData(new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero).AddHours(now.UtcDateTime.Hour));
        d.AddPeople();
        d.AddJobs();
        d.AddApplications();
        d.AddAi();
        d.AddInterviews();
        return d;
    }

    // ------------------------------------------------------------------ nguoi

    private static readonly (string Name, string Dept)[] HrPeople =
    [
        ("Tran Thi Mai", "Khoi Cong nghe"),
        ("Le Van Hung", "Khoi San pham"),
        ("Pham Thu Ha", "Khoi Van hanh"),
    ];

    private static readonly (string Name, string Headline, string Skills, int Years)[] CandidatePeople =
    [
        ("Nguyen Van An", "Backend Developer", "C#, .NET, ASP.NET Core, Entity Framework, PostgreSQL, Docker, REST API", 3),
        ("Tran Minh Chau", "Frontend Developer", "JavaScript, TypeScript, React, HTML, CSS, Redux, Git", 2),
        ("Le Hoang Duc", "Fullstack Developer", "C#, .NET, React, SQL Server, Azure, Docker, Git", 4),
        ("Pham Ngoc Giang", "Data Analyst", "SQL, Python, Pandas, Power BI, Excel, Statistics", 2),
        ("Hoang Thi Hoa", "QA Engineer", "Selenium, Automation Testing, Postman, SQL, Jira, Agile", 3),
        ("Vu Quang Khai", "DevOps Engineer", "Docker, Kubernetes, Linux, CI/CD, AWS, Terraform, Bash", 5),
        ("Dang Thu Linh", "Java Developer", "Java, Spring Boot, MySQL, Microservices, Kafka, Docker", 3),
        ("Bui Tien Minh", "Intern Software Engineer", "C#, Python, SQL, Git, HTML, CSS", 0),
    ];

    private void AddPeople()
    {
        for (var i = 0; i < HrPeople.Length; i++)
        {
            var userId = Id(0x01, i + 1);
            Users.Add(new UserRow(userId, $"hr{i + 1}@ats.local", "Hr", HrPeople[i].Name));
            HrProfiles.Add(new HrRow(Id(0x03, i + 1), userId, HrPeople[i].Name, HrPeople[i].Dept));
        }

        var cvNo = 0;
        for (var i = 0; i < CandidatePeople.Length; i++)
        {
            var p = CandidatePeople[i];
            var userId = Id(0x01, 100 + i + 1);
            var candidateId = Id(0x02, i + 1);
            Users.Add(new UserRow(userId, $"ungvien{i + 1}@ats.local", "Candidate", p.Name));
            Candidates.Add(new CandidateRow(candidateId, userId, p.Name, $"09{i + 1:D8}", "Ha Noi"));

            // Ung vien 1-4 co them CV thu hai (khong mac dinh) — demo "nhieu CV".
            var versions = i < 4 ? 2 : 1;
            for (var v = 0; v < versions; v++)
            {
                cvNo++;
                var name = v == 0 ? $"CV {p.Headline}" : $"CV {p.Headline} (ban tieng Anh)";
                var text = CvText(p.Name, p.Headline, p.Skills, p.Years, english: v == 1);
                Cvs.Add(new CvRow(
                    Id(0x04, cvNo), candidateId, name, $"seed-cv-{cvNo:D2}.pdf", text,
                    IsDefault: v == 0, DemoPdf.Create(text)));
            }
        }
    }

    private static string CvText(string name, string headline, string skills, int years, bool english)
    {
        var experience = years == 0
            ? "Chua co kinh nghiem di lam, da lam do an mon hoc va thuc tap ngan han."
            : $"{years} nam kinh nghiem lam {headline}.";
        var sb = new StringBuilder()
            .AppendLine(name)
            .AppendLine(headline)
            .AppendLine($"Email: {Slug(name)}@example.com - Dien thoai: 0900000000")
            .AppendLine(english ? "SUMMARY" : "TOM TAT")
            .AppendLine(experience)
            .AppendLine(english ? "SKILLS" : "KY NANG")
            .AppendLine(skills)
            .AppendLine(english ? "EXPERIENCE" : "KINH NGHIEM")
            .AppendLine(years == 0
                ? "Do an: website quan ly thu vien (C#, SQL)."
                : $"Cong ty ABC ({2026 - years} - nay): phat trien va bao tri he thong, lam viec nhom Agile/Scrum.")
            .AppendLine(english ? "EDUCATION" : "HOC VAN")
            .AppendLine("Dai hoc Bach khoa Ha Noi - Cong nghe thong tin");
        return sb.ToString();
    }

    private static string Slug(string name) => name.Replace(" ", ".", StringComparison.Ordinal).ToLowerInvariant();

    // ------------------------------------------------------------------ tin

    private static readonly (string Title, string Skills, string Type, int Years)[] JobSpecs =
    [
        ("Backend Developer .NET", "C#, .NET, ASP.NET Core, PostgreSQL, Docker", "FullTime", 2),
        ("Frontend Developer React", "JavaScript, TypeScript, React, HTML, CSS", "FullTime", 1),
        ("Fullstack Developer", "C#, .NET, React, SQL, Git", "FullTime", 3),
        ("Data Engineer", "Python, SQL, Spark, Airflow", "FullTime", 2),          // Draft
        ("QA Automation Engineer", "Selenium, Automation Testing, Postman, SQL", "FullTime", 2),
        ("DevOps Engineer", "Docker, Kubernetes, Linux, CI/CD, AWS", "FullTime", 3),
        ("Java Developer", "Java, Spring Boot, MySQL, Microservices", "Contract", 2),  // Closed
        ("Data Analyst", "SQL, Python, Power BI, Excel", "PartTime", 1),
        ("Thuc tap sinh Lap trinh", "C#, Python, SQL, Git", "Internship", 0),
        ("Tech Lead .NET", "C#, .NET, Microservices, Docker, Azure", "FullTime", 5),
    ];

    private void AddJobs()
    {
        for (var i = 0; i < JobSpecs.Length; i++)
        {
            var spec = JobSpecs[i];
            var hrUser = Users[i < 4 ? 0 : i < 7 ? 1 : 2].Id;
            var status = i == 3 ? "Draft" : i == 6 ? "Closed" : "Open";
            var created = Now.AddDays(-40 + i);
            DateTimeOffset? posted = status == "Draft" ? null : created.AddDays(1);
            DateTimeOffset? closed = status == "Closed" ? Now.AddDays(-5) : null;

            Jobs.Add(new JobRow(
                Id(0x05, i + 1), hrUser, spec.Title,
                $"Cong ty tuyen {spec.Title} tham gia phat trien he thong tuyen dung cho khach hang doanh nghiep.",
                $"Ky nang bat buoc: {spec.Skills}.\nUu tien: Git, Agile.\nKinh nghiem toi thieu {spec.Years} nam.",
                i % 3 == 2 ? "Ho Chi Minh" : "Ha Noi", spec.Type, status, created, posted, closed));
        }
    }

    // ------------------------------------------------------------------ don

    /// <summary>Phan bo 30 don: 8 Applied, 7 Screening, 6 Interview, 3 Hired, 6 Rejected.</summary>
    private static readonly string[] StatusPlan =
    [
        "Applied", "Screening", "Interview", "Rejected", "Applied", "Screening", "Hired", "Applied",
        "Interview", "Rejected", "Screening", "Applied", "Interview", "Rejected", "Hired", "Screening",
        "Applied", "Interview", "Rejected", "Screening", "Applied", "Hired", "Interview", "Rejected",
        "Screening", "Applied", "Interview", "Rejected", "Screening", "Applied",
    ];

    private void AddApplications()
    {
        // Moi tin tru Draft; ung vien i nop lan luot cac tin, bo cap trung — tat dinh.
        var jobIndexes = Enumerable.Range(0, Jobs.Count).Where(j => Jobs[j].Status != "Draft").ToArray();
        var used = new HashSet<(int, int)>();
        var n = 0;
        for (var round = 0; n < ApplicationCount; round++)
        {
            for (var c = 0; c < Candidates.Count && n < ApplicationCount; c++)
            {
                var job = jobIndexes[((c * 3) + round) % jobIndexes.Length];
                if (!used.Add((job, c)))
                {
                    continue;
                }

                var status = StatusPlan[n];
                var applied = Jobs[job].PostedAt!.Value.AddDays(1 + (n % 5));
                var cv = Cvs.First(x => x.CandidateId == Candidates[c].Id && x.IsDefault);
                Applications.Add(new ApplicationRow(
                    Id(0x06, n + 1), Jobs[job].Id, Candidates[c].Id, cv.Id, status,
                    status == "Rejected" ? "Chua phu hop yeu cau kinh nghiem." : null,
                    applied, status == "Applied" ? applied : applied.AddDays(2)));
                n++;
            }
        }
    }

    // ------------------------------------------------------------------ AI

    private static readonly string[] AdapterPlan = ["Fake", "Keyword", "OpenAI", "Embedding"];

    private void AddAi()
    {
        // 6 dong cache (OpenAI/Embedding/Fake) — tong hit_count de dashboard co ti le cache hit.
        string[] cacheAdapters = ["OpenAI", "OpenAI", "Embedding", "Fake", "Fake", "OpenAI"];
        int[] hits = [5, 2, 3, 8, 1, 0];
        for (var i = 0; i < cacheAdapters.Length; i++)
        {
            var score = 60 + (i * 7);
            Cache.Add(new CacheRow(
                Id(0x08, i + 1),
                Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes($"seed-demo-cache|{i}"))),
                score, Summary(score), JsonList("C#", ".NET"), JsonList("Kubernetes"),
                cacheAdapters[i], ModelVersion(cacheAdapters[i]), hits[i]));
        }

        for (var i = 0; i < Applications.Count; i++)
        {
            var app = Applications[i];
            var updated = app.AppliedAt.AddHours(1);
            if (i % 10 == 8)
            {
                // Chua cham: worker chua toi (Pending) — UI hien "dang cho".
                Scores.Add(new ScoreRow(Id(0x07, i + 1), app.Id, null, null, "[]", "[]", "Pending",
                    null, null, null, null, null, updated));
                continue;
            }

            if (i % 10 == 9)
            {
                Scores.Add(new ScoreRow(Id(0x07, i + 1), app.Id, null, null, "[]", "[]", "Failed",
                    null, null, null, "CV khong co noi dung van ban (file scan).", null, updated));
                continue;
            }

            var adapter = AdapterPlan[i % AdapterPlan.Length];
            var score = 40 + ((i * 17) % 55);
            var cache = adapter == "Keyword" ? null : Cache.FirstOrDefault(c => c.AdapterUsed == adapter);
            if (cache is not null)
            {
                score = cache.Score;
            }

            Scores.Add(new ScoreRow(
                Id(0x07, i + 1), app.Id, score, Summary(score),
                JsonList("Dung ky nang chinh cua tin", "Co kinh nghiem du an thuc te"),
                JsonList("Thieu kinh nghiem cloud"),
                "Scored", adapter, ModelVersion(adapter), cache?.Id, null, updated, updated));
        }

        // Mot lo sang loc da xong cho tin 1 (HR 1).
        var job1 = Jobs[0].Id;
        var job1Apps = Applications.Where(a => a.JobId == job1).Select(a => a.Id).ToHashSet();
        var failed = Scores.Count(s => job1Apps.Contains(s.ApplicationId) && s.Status == "Failed");
        ScreeningJobs.Add(new ScreeningJobRow(Id(0x0c, 1), job1, Users[0].Id, job1Apps.Count, job1Apps.Count - failed, failed));
    }

    private static string ModelVersion(string adapter) => adapter switch
    {
        "OpenAI" => "gpt-4o-mini",
        "Embedding" => "text-embedding-3-small",
        "Fake" => "fake-v1",
        _ => "keyword-v1",
    };

    private static string Summary(int score) => score >= 75
        ? "Ho so phu hop tot voi yeu cau, dap ung hau het ky nang bat buoc."
        : score >= 55
            ? "Ho so phu hop mot phan, con thieu mot so ky nang."
            : "Ho so it phu hop voi yeu cau cua tin.";

    private static string JsonList(params string[] items) => JsonSerializer.Serialize(items, Json);

    // ------------------------------------------------------------------ phong van

    private void AddInterviews()
    {
        var interviewApps = Applications.Where(a => a.Status == "Interview").Take(2).ToList();
        var hiredApp = Applications.First(a => a.Status == "Hired");
        var n = 0;

        foreach (var app in interviewApps)
        {
            n++;
            Interviews.Add(new InterviewRow(Id(0x09, n), app.Id, Now.AddDays(2 + n).AddHours(9 - Now.Hour),
                n == 1 ? "online" : "onsite",
                n == 1 ? "https://meet.example.com/ats-demo" : "Tang 5, 1 Dai Co Viet, Ha Noi",
                "Tran Thi Mai", "Scheduled"));
        }

        n++;
        var doneId = Id(0x09, n);
        Interviews.Add(new InterviewRow(doneId, hiredApp.Id, Now.AddDays(-4), "onsite",
            "Tang 5, 1 Dai Co Viet, Ha Noi", "Le Van Hung", "Done"));
        Evaluations.Add(new EvaluationRow(Id(0x0a, 1), doneId, 5, "Nam vung kien thuc, giao tiep tot.", "Hire"));

        // Bo cau hoi (nguon Template) da luu cho buoi sap toi dau tien.
        Questions.Add(new QuestionRow(Id(0x0b, 1), Interviews[0].Id, "technical", "Trinh bay cach ban thiet ke mot REST API co phan trang.", 0));
        Questions.Add(new QuestionRow(Id(0x0b, 2), Interviews[0].Id, "behavioral", "Ke ve mot lan ban bat dong y kien voi dong nghiep va cach giai quyet.", 1));
        Questions.Add(new QuestionRow(Id(0x0b, 3), Interviews[0].Id, "situational", "Neu he thong production loi luc 2 gio sang, ban xu ly the nao?", 2));

        // Them mot danh gia cho buoi Done thu hai neu co don Hired thu hai.
        var secondHired = Applications.Where(a => a.Status == "Hired").Skip(1).FirstOrDefault();
        if (secondHired is not null)
        {
            n++;
            Interviews.Add(new InterviewRow(Id(0x09, n), secondHired.Id, Now.AddDays(-6), "online",
                "https://meet.example.com/ats-demo-2", "Pham Thu Ha", "Done"));
            Evaluations.Add(new EvaluationRow(Id(0x0a, 2), Id(0x09, n), 4, "Ky nang tot, can bo sung kinh nghiem cloud.", "Hire"));
        }
    }

    // ------------------------------------------------------------------ dong

    public sealed record UserRow(Guid Id, string Email, string Role, string FullName);

    public sealed record HrRow(Guid Id, Guid UserId, string FullName, string Department);

    public sealed record CandidateRow(Guid Id, Guid UserId, string FullName, string Phone, string Address);

    public sealed record CvRow(Guid Id, Guid CandidateId, string Name, string FileName, string Text, bool IsDefault, byte[] Pdf);

    public sealed record JobRow(
        Guid Id, Guid HrOwnerId, string Title, string Description, string Requirements, string Location,
        string EmploymentType, string Status, DateTimeOffset CreatedAt, DateTimeOffset? PostedAt, DateTimeOffset? ClosedAt);

    public sealed record ApplicationRow(
        Guid Id, Guid JobId, Guid CandidateId, Guid CvId, string Status, string? HrNote,
        DateTimeOffset AppliedAt, DateTimeOffset UpdatedAt);

    public sealed record CacheRow(
        Guid Id, string CacheKey, int Score, string Summary, string StrengthsJson, string GapsJson,
        string AdapterUsed, string ModelVersion, int HitCount);

    public sealed record ScoreRow(
        Guid Id, Guid ApplicationId, int? Score, string? Summary, string StrengthsJson, string GapsJson, string Status,
        string? AdapterUsed, string? ModelVersion, Guid? CacheId, string? FailureReason,
        DateTimeOffset? ScoredAt, DateTimeOffset UpdatedAt);

    public sealed record ScreeningJobRow(Guid Id, Guid JobId, Guid StartedByHrId, int TargetCount, int DoneCount, int FailedCount);

    public sealed record InterviewRow(
        Guid Id, Guid ApplicationId, DateTimeOffset ScheduledAt, string Mode, string LocationOrLink,
        string InterviewerName, string Status);

    public sealed record EvaluationRow(Guid Id, Guid InterviewId, int Rating, string Comment, string Recommendation);

    public sealed record QuestionRow(Guid Id, Guid InterviewId, string Category, string Question, int OrderIndex);
}
