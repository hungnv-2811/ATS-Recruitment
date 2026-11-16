using ATS.AiScreening.Application.Ports;
using ATS.AiScreening.Application.Scoring;
using ATS.AiScreening.Infrastructure.Scoring;
using Xunit;

namespace ATS.AiScreening.Tests;

/// <summary>
/// Snapshot 5 cap (CV, JD) mau cho KeywordScoringAdapter (ai-integration.md muc 6).
/// </summary>
/// <remarks>
/// Gia tri ky vong da duoc duyet tay (tinh theo cong thuc ghi o KeywordScoringAdapter). Doi
/// SkillCatalog, JobRequirementParser hay cong thuc ma lam lech snapshot thi PHAI xem lai tung cap
/// roi moi cap nhat — day chinh la luoi bat thay doi ngam vao thang diem HR dang dung.
/// Di qua DUNG duong that: JD van ban tu do → parser; CV tho → SimpleAnonymizer.
/// </remarks>
public sealed class KeywordSnapshotTests
{
    public static TheoryData<string, string, string, int, string[]> Pairs => new()
    {
        {
            "backend-dotnet",
            "- C#, ASP.NET Core\n- PostgreSQL\n- Docker\n- 3 năm kinh nghiệm\nƯu tiên: Redis, Kubernetes",
            "Backend developer với 4 năm kinh nghiệm C#, ASP.NET Core, SQL Server, Docker và Redis.",
            80,
            ["Thiếu PostgreSQL"]
        },
        {
            "frontend-react",
            "React, TypeScript, HTML, CSS\nKinh nghiệm 2 năm\nBiết Next.js là điểm cộng",
            "Frontend dev 1 năm kinh nghiệm ReactJS, JavaScript, HTML5, CSS3, Tailwind.",
            65,
            ["Thiếu TypeScript", "Chưa thấy đủ 2 năm kinh nghiệm"]
        },
        {
            "data-analyst",
            "- Python, Pandas, NumPy\n- SQL\n- Machine Learning\n- Tối thiểu 2 năm",
            "Data analyst 3 năm kinh nghiệm Python, Pandas, Power BI, Excel, MySQL.",
            40,
            ["Thiếu NumPy", "Thiếu SQL", "Thiếu Machine Learning"]
        },
        {
            "jd-khong-ky-nang",
            "Có tinh thần trách nhiệm cao và sẵn sàng học hỏi công nghệ mới mỗi ngày",
            "Bất kỳ CV nào với Java",
            50,
            []
        },
        {
            "java-backend",
            "Java, Spring Boot\nMicroservices\nKafka hoặc RabbitMQ\nTư duy logic tốt\n5 năm kinh nghiệm",
            "Senior Java developer, 6 năm kinh nghiệm Spring, microservice, Kafka, Docker. Tư duy logic tốt.",
            83,
            ["Thiếu RabbitMQ"]
        },
    };

    [Theory]
    [MemberData(nameof(Pairs))]
    public async Task Diem_Keyword_khop_snapshot_da_duyet(string name, string requirements, string cvText, int expectedScore, string[] expectedGaps)
    {
        var job = new JobInfo(Guid.NewGuid(), Guid.NewGuid(), name, "Mo ta", requirements, "Open");
        var jd = JobRequirementParser.Parse(job);
        var cv = await Cv.AnonAsync(cvText);

        var result = await new KeywordScoringAdapter().ScoreAsync(cv, jd);

        Assert.Equal(expectedScore, result.Score.Value);
        Assert.Equal(expectedGaps, result.Gaps);
        Assert.Equal(AdapterNames.Keyword, result.AdapterUsed);
    }
}
