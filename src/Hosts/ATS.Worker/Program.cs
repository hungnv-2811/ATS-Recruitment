using ATS.AiScreening.Infrastructure;
using ATS.Persistence;
using ATS.Recruitment.Infrastructure;
using ATS.Recruitment.Infrastructure.Shared;
using ATS.Worker;

var builder = Host.CreateApplicationBuilder(args);

// ---------------------------------------------------------------------------
// Composition Root cua Worker. Giu GIONG API (src/Hosts/ATS.Api/Program.cs) o moi cho
// lien quan toi du lieu va AI: cung danh sach module cho AtsDbContext, cung
// AddRecruitmentModule → AddAiScreeningModule (thu tu co y nghia: AiScreening ghi de NoOp),
// cung IConfiguration (AiProvider, OpenAI, Ai:*).
//
// Khong goi AddIdentityModule: worker khong phat JWT, khong gui mail dat lai mat khau.
// Assembly Identity van nam trong danh sach module de model DbContext trung voi API.
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default");
var aiProvider = builder.Configuration["AiProvider"] ?? "Fake";
var openAiKeyConfigured = !string.IsNullOrWhiteSpace(builder.Configuration["OpenAI:ApiKey"]);

builder.Services.AddAtsPersistence(
    connectionString,
    typeof(ATS.Recruitment.Infrastructure.InfrastructureAssemblyMarker).Assembly,
    typeof(ATS.AiScreening.Infrastructure.InfrastructureAssemblyMarker).Assembly,
    typeof(ATS.Identity.Infrastructure.InfrastructureAssemblyMarker).Assembly);

builder.Services.AddSharedInfrastructure(builder.Configuration);   // TV1: email, file, trich CV
builder.Services.AddRecruitmentModule(builder.Configuration);      // TV2: IRecruitmentQueries
builder.Services.AddAiScreeningModule(builder.Configuration);      // TV4: queue, handler, adapter AI

builder.Services.Configure<ScreeningWorkerOptions>(
    builder.Configuration.GetSection(ScreeningWorkerOptions.SectionName));
builder.Services.AddHostedService<ScreeningWorker>();

var host = builder.Build();

// Canh bao cau hinh lech — cung thong diep voi API (docs/weekly-plan.md, bang rui ro).
var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger("ATS.Worker");
logger.LogInformation("AiProvider cua worker = {AiProvider}. Gia tri nay PHAI trung voi API.", aiProvider);
if (string.Equals(aiProvider, "OpenAI", StringComparison.OrdinalIgnoreCase) && !openAiKeyConfigured)
{
    logger.LogWarning(
        "AiProvider = OpenAI nhung chua co OpenAI:ApiKey — pipeline se roi xuong adapter du phong.");
}

await host.RunAsync().ConfigureAwait(false);
