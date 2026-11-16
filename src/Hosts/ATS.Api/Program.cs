using System.Text;
using ATS.AiScreening.Infrastructure;
using ATS.Api.Auth;
using ATS.Api.Endpoints;
using ATS.Identity.Infrastructure;
using ATS.Persistence;
using ATS.Persistence.Reporting;
using ATS.Recruitment.Infrastructure;
using ATS.Recruitment.Infrastructure.Shared;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Composition Root. Moi quyet dinh "dung adapter nao" nam O DAY, khong nam
// trong Application hay Domain.
//
// Doc cau hinh ra bien local TRUOC khi dang ky: neu doc ben trong lambda thi
// lambda giu tham chieu toi `builder` (keo theo ca ServiceCollection va
// ConfigurationManager) suot doi tien trinh, va voi AddDbContext thi con duyet
// lai chuoi configuration provider moi lan tao scope.
// ---------------------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("Default");
var aiProvider = builder.Configuration["AiProvider"] ?? "Fake";
var autoMigrate = builder.Configuration.GetValue("Database:AutoMigrate", defaultValue: false);
var seedDemoData = builder.Configuration.GetValue("Database:SeedDemoData", defaultValue: false);

var jwtSecret = builder.Configuration["Jwt:Secret"]
                ?? "dev-only-secret-at-least-32-characters-long!!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ats-recruitment";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ats-recruitment-users";

// Ghi nguoc gia tri da chon vao configuration: JwtTokenIssuer (Identity.Infrastructure) doc
// "Jwt:*" tu IConfiguration, nen ben PHAT va ben KIEM token luon dung cung mot khoa,
// ke ca khi dev chay khong co bien moi truong (fallback o tren).
builder.Configuration["Jwt:Secret"] = jwtSecret;
builder.Configuration["Jwt:Issuer"] = jwtIssuer;
builder.Configuration["Jwt:Audience"] = jwtAudience;

// Danh sach module truyen thang vao day. Them module thu 4 ma quen dong nay
// thi bang cua no khong duoc tao — nhung it nhat chi co DUNG MOT cho phai sua.
builder.Services.AddAtsPersistence(
    connectionString,
    typeof(ATS.Recruitment.Infrastructure.InfrastructureAssemblyMarker).Assembly,
    typeof(ATS.AiScreening.Infrastructure.InfrastructureAssemblyMarker).Assembly,
    typeof(ATS.Identity.Infrastructure.InfrastructureAssemblyMarker).Assembly);

// Moi module tu dang ky service cua minh. THU TU CO Y NGHIA: AiScreening dang ky SAU
// Recruitment de adapter cross-module that ghi de ban NoOp (TryAdd) cua Recruitment.
builder.Services.AddSharedInfrastructure(builder.Configuration);   // TV1: email, file, trich CV
builder.Services.AddIdentityModule(builder.Configuration);         // TV2
builder.Services.AddRecruitmentModule(builder.Configuration);      // TV2
builder.Services.AddAiScreeningModule(builder.Configuration);      // TV4
builder.Services.AddAtsReporting();                                // TV1: dashboard

builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;

        // Giu nguyen ten claim "sub"/"role" thay vi doi sang URI dai cua WS-Federation.
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            NameClaimType = "sub",
            RoleClaimType = "role",
        };
    });

// Phan quyen theo vai ep o API (RB8) — UI an menu chi la lop thu hai.
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthPolicies.CandidateOnly, p => p.RequireRole(Roles.Candidate))
    .AddPolicy(AuthPolicies.HrOnly, p => p.RequireRole(Roles.Hr))
    .AddPolicy(AuthPolicies.AdminOnly, p => p.RequireRole(Roles.Admin));

// Swagger chi phuc vu dev/demo, nen dang ky cung phai nam trong dung dieu kien
// voi luc su dung — khong nap Swashbuckle va hang chuc ServiceDescriptor cua no
// o moi lan khoi dong production cho mot duong ma khong bao gio chay.
var enableSwagger = builder.Environment.IsDevelopment();
if (enableSwagger)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        // Nut "Authorize" trong Swagger UI: dan JWT vao de goi endpoint can dang nhap.
        options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.OpenApiSecurityScheme
        {
            Type = Microsoft.OpenApi.SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
        });
        options.AddSecurityRequirement(document => new Microsoft.OpenApi.OpenApiSecurityRequirement
        {
            [new Microsoft.OpenApi.OpenApiSecuritySchemeReference("Bearer", document)] = [],
        });
    });
}

var app = builder.Build();

// ---------------------------------------------------------------------------
// Canh bao cau hinh lech — xem bang rui ro trong docs/weekly-plan.md.
// `api` va `worker` PHAI dung cung AiProvider, neu khong ung vien se thay diem
// gia trong khi HR thay diem that cho cung mot ho so.
// ---------------------------------------------------------------------------
app.Logger.LogInformation("AiProvider cua API = {AiProvider}. Gia tri nay PHAI trung voi worker.", aiProvider);

// ---------------------------------------------------------------------------
// Ap migration luc khoi dong — CHI khi duoc bat tuong minh.
//
// Mac dinh TAT. Bat o docker-compose (Database__AutoMigrate=true) de
// `docker compose up` cho ra mot he thong dung duoc ngay.
//
// Chi API lam viec nay, worker thi khong: hai process cung migrate mot luc
// se tranh khoa tren bang __EFMigrationsHistory.
//
// Khi deploy that: TAT co nay, chay `dotnet ef database update` trong buoc
// deploy rieng. Mot service tu doi luoc do database luc khoi dong la thu
// khong ai muon gap luc 2 gio sang.
// ---------------------------------------------------------------------------
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
    await db.Database.MigrateAsync();
    app.Logger.LogInformation("Da ap migration. Schema: identity, recruitment, aiscreening.");

    // Seed demo (tuan 10) chi chay SAU migration — bang phai ton tai truoc.
    if (seedDemoData)
    {
        await DatabaseSeeder.SeedAsync(db);
    }
}

if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
}

app.UseStatusCodePages();
app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityEndpoints();      // TV2
app.MapRecruitmentEndpoints();   // TV2
app.MapAiScreeningEndpoints();   // TV4
app.MapReportEndpoints();        // TV1

// Healthcheck: moc deliverable cua tuan 2.
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "ATS.Api",
    aiProvider,
    utc = DateTimeOffset.UtcNow,
}))
.WithName("Health")
.AllowAnonymous();

app.Run();

/// <summary>Lo ra cho ATS.IntegrationTests dung WebApplicationFactory.</summary>
public partial class Program;
