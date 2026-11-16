using System.Net.Http.Headers;
using System.Text;
using ATS.AiScreening.Application;
using ATS.Persistence;
using ATS.Recruitment.Application.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ATS.IntegrationTests;

/// <summary>
/// API that trong bo nho: EF InMemory thay Postgres, AiProvider=Fake, hang doi trong bo nho,
/// IRecruitmentQueries gia (nhanh Recruitment cua TV2 chua merge). Khong can container nao.
/// </summary>
public sealed class AiApiFactory : WebApplicationFactory<Program>
{
    // Trung voi fallback trong Program.cs — ben PHAT (test) va ben KIEM (API) dung cung khoa.
    public const string JwtSecret = "dev-only-secret-at-least-32-characters-long!!";
    public const string JwtIssuer = "ats-recruitment";
    public const string JwtAudience = "ats-recruitment-users";
    public const int PreviewLimit = 2;

    private readonly string _databaseName = $"ats-it-{Guid.NewGuid()}";

    public StubRecruitmentQueries Recruitment { get; } = new();

    public HttpClient ClientAs(Guid userId, string role)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", IssueToken(userId, role));
        return client;
    }

    public static string IssueToken(Guid userId, string role)
        => new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = JwtIssuer,
            Audience = JwtAudience,
            Expires = DateTime.UtcNow.AddHours(1),
            Claims = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["sub"] = userId.ToString(),
                ["role"] = role,
                ["email"] = $"{role.ToLowerInvariant()}@test.local",
                ["name"] = "Test User",
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)),
                SecurityAlgorithms.HmacSha256),
        });

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Development");
        builder.UseSetting("AiProvider", "Fake");
        builder.UseSetting("Redis:ConnectionString", string.Empty);

        builder.ConfigureTestServices(services =>
        {
            // Go MOI dang ky DbContext cua Npgsql (ke ca IDbContextOptionsConfiguration<T> cua EF 9+),
            // neu khong EF bao "hai database provider cung duoc dang ky".
            var dbDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions)
                            || d.ServiceType == typeof(AtsDbContext)
                            || (d.ServiceType.IsGenericType && d.ServiceType.GetGenericArguments().Contains(typeof(AtsDbContext))))
                .ToList();
            foreach (var descriptor in dbDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AtsDbContext>(o => o.UseInMemoryDatabase(_databaseName));

            // Token o day tu ky cho user GIA (khong co trong identity.users): bo qua buoc kiem
            // security stamp — buoc do co test rieng voi user that (SessionRevocationTests).
            services.RemoveAll<ATS.Identity.Application.ITokenSessionValidator>();
            services.AddSingleton<ATS.Identity.Application.ITokenSessionValidator>(new AllowAllSessions());

            services.RemoveAll<IRecruitmentQueries>();
            services.AddSingleton<IRecruitmentQueries>(Recruitment);

            // Tran nho de test 429 ma khong phai goi 20 lan.
            services.RemoveAll<AiScreeningOptions>();
            services.AddSingleton(new AiScreeningOptions { ModelVersion = "fake-v1", PreviewLimitPerDay = PreviewLimit });
        });
    }
}

/// <summary>Du lieu tuyen dung gia cho test API — thay cho ban that cua TV2 o pha tich hop.</summary>
public sealed class StubRecruitmentQueries : IRecruitmentQueries
{
    private readonly Dictionary<Guid, JobSnapshot> _jobs = [];
    private readonly Dictionary<Guid, CvSnapshot> _cvs = [];
    private readonly Dictionary<Guid, (Guid JobId, Guid CvId)> _applications = [];
    private readonly object _gate = new();
    private int _sequence;

    /// <summary>
    /// Moi tin co tieu de RIENG: cache AI khoa theo NOI DUNG, nen hai tin giong het nhau se trung
    /// cache cua nhau (dung thiet ke) va lam test han muc/cacheHit phu thuoc thu tu chay.
    /// </summary>
    public JobSnapshot AddJob(Guid hrUserId, string status = "Open")
    {
        var n = Interlocked.Increment(ref _sequence);
        var job = new JobSnapshot(Guid.NewGuid(), hrUserId, $"Backend .NET so {n}", "Phat trien API", "C#, PostgreSQL, Docker\n3 năm kinh nghiệm", status);
        lock (_gate)
        {
            _jobs[job.JobId] = job;
        }

        return job;
    }

    public CvSnapshot AddCv(Guid candidateUserId, string? text = null)
    {
        var n = Interlocked.Increment(ref _sequence);
        text ??= $"Ho so so {n}. 4 năm kinh nghiệm C#, PostgreSQL. Email: a@b.com";
        var cv = new CvSnapshot(Guid.NewGuid(), candidateUserId, "CV", text);
        lock (_gate)
        {
            _cvs[cv.CvId] = cv;
        }

        return cv;
    }

    public Guid AddApplication(JobSnapshot job, CvSnapshot cv)
    {
        var id = Guid.NewGuid();
        lock (_gate)
        {
            _applications[id] = (job.JobId, cv.CvId);
        }

        return id;
    }

    public Task<ApplicationSnapshot?> GetApplicationAsync(Guid applicationId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_applications.TryGetValue(applicationId, out var a)
                ? new ApplicationSnapshot(applicationId, "Applied", _jobs[a.JobId], _cvs[a.CvId])
                : null);
        }
    }

    public Task<JobSnapshot?> GetJobAsync(Guid jobId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_jobs.GetValueOrDefault(jobId));
        }
    }

    public Task<CvSnapshot?> GetCvAsync(Guid cvId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            return Task.FromResult(_cvs.GetValueOrDefault(cvId));
        }
    }

    public Task<Guid?> GetApplicationIdOfInterviewAsync(Guid interviewId, CancellationToken ct = default)
        => Task.FromResult<Guid?>(null);

    public Task<IReadOnlyList<Guid>> ListApplicationIdsByJobAsync(Guid jobId, CancellationToken ct = default)
    {
        lock (_gate)
        {
            return Task.FromResult<IReadOnlyList<Guid>>(_applications.Where(a => a.Value.JobId == jobId).Select(a => a.Key).ToArray());
        }
    }
}

/// <summary>Chap nhan moi token da qua kiem chu ky — chi dung cho test endpoint AI voi user gia.</summary>
internal sealed class AllowAllSessions : ATS.Identity.Application.ITokenSessionValidator
{
    public Task<bool> IsValidAsync(Guid userId, string? securityStamp, CancellationToken ct = default)
        => Task.FromResult(true);
}
