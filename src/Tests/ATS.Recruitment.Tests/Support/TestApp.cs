using ATS.Identity.Application.Contracts;
using ATS.Identity.Domain.Ports;
using ATS.Identity.Infrastructure;
using ATS.Identity.Infrastructure.Security;
using ATS.Persistence;
using ATS.Recruitment.Application.Contracts;
using ATS.Recruitment.Application.Ports;
using ATS.Recruitment.Domain.Ports;
using ATS.Recruitment.Infrastructure;
using ATS.SharedKernel;
using ATS.SharedKernel.Ports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ATS.Recruitment.Tests.Support;

/// <summary>
/// Dung DUNG wiring that (AddIdentityModule + AddRecruitmentModule) tren EF InMemory, thay
/// adapter ha tang (email, file, trich text, AiScreening) bang fake.
/// </summary>
/// <remarks>
/// Moi lan <see cref="Service{T}"/> tao MOT scope moi (= mot request, mot DbContext moi) de
/// test bat duoc loi "quen SaveChanges" ma change tracker dung chung se che mat.
/// </remarks>
internal sealed class TestApp : IDisposable
{
    public const string Password = "Passw0rd!";

    private readonly ServiceProvider _root;
    private readonly List<IServiceScope> _scopes = [];

    public TestApp()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = "test-secret-at-least-32-characters-long!!",
                ["Jwt:Issuer"] = "ats-test",
                ["Jwt:Audience"] = "ats-test-users",
                ["Web:BaseUrl"] = "http://ui.test",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));

        var dbName = Guid.NewGuid().ToString("N");
        services.AddSingleton(new ModuleAssemblies(
            typeof(ATS.Recruitment.Infrastructure.InfrastructureAssemblyMarker).Assembly,
            typeof(ATS.Identity.Infrastructure.InfrastructureAssemblyMarker).Assembly));
        services.AddDbContext<AtsDbContext>(o => o.UseInMemoryDatabase(dbName));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AtsDbContext>());

        services.AddSingleton<TimeProvider>(Clock);
        services.AddIdentityModule(configuration);
        services.AddRecruitmentModule(configuration);

        // Ghi de adapter ha tang bang fake (dang ky SAU → thang khi resolve).
        services.AddSingleton<IPasswordHasher>(new BCryptPasswordHasher(workFactor: 4));
        services.AddSingleton<IEmailSender>(Email);
        services.AddSingleton<IFileStorage>(Files);
        services.AddSingleton<ICvTextExtractor>(Extractor);
        services.AddSingleton<IApplicationScreeningTrigger>(Trigger);
        services.AddSingleton<IApplicationScoreReader>(Scores);

        _root = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public FakeClock Clock { get; } = new();

    public FakeEmailSender Email { get; } = new();

    public FakeFileStorage Files { get; } = new();

    public FakeTextExtractor Extractor { get; } = new();

    public RecordingScreeningTrigger Trigger { get; } = new();

    public FakeScoreReader Scores { get; } = new();

    public T Service<T>()
        where T : notnull
    {
        var scope = _root.CreateScope();
        _scopes.Add(scope);
        return scope.ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>DbContext cua mot scope moi — de doc lai trang thai da LUU.</summary>
    public AtsDbContext Db() => Service<AtsDbContext>();

    public async Task<AuthResponse> RegisterAsync(string email, string role = "Candidate", string fullName = "Nguyen Van A")
    {
        var result = await Service<IAuthService>().RegisterAsync(new RegisterRequest(email, Password, fullName, role));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value;
    }

    public async Task<JobDto> CreateJobAsync(Guid hrUserId, bool publish = true, string title = "Backend .NET")
    {
        var result = await Service<IJobService>().CreateAsync(
            hrUserId,
            new CreateJobRequest(title, "Xay dung API", "C#, EF Core", "Ha Noi", "FullTime", publish));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value;
    }

    public async Task<CvDto> UploadCvAsync(Guid candidateUserId, string name = "CV Backend", string fileName = "cv.pdf", int size = 2048)
    {
        using var content = new MemoryStream(new byte[size]);
        var result = await Service<ICvService>().UploadAsync(
            candidateUserId,
            new UploadCvCommand(name, fileName, size, content));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value;
    }

    public async Task<ApplicationDto> ApplyAsync(Guid candidateUserId, Guid jobId, Guid cvId)
    {
        var result = await Service<IApplicationService>().ApplyAsync(candidateUserId, new ApplyRequest(jobId, cvId));
        Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Message : null);
        return result.Value;
    }

    public void Dispose()
    {
        foreach (var scope in _scopes)
        {
            scope.Dispose();
        }

        _root.Dispose();
    }
}
