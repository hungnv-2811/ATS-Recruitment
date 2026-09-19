using System.Text;
using ATS.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Composition Root. Moi quyet dinh "dung adapter nao" nam O DAY, khong nam
// trong Application hay Domain.
// ---------------------------------------------------------------------------

// Moi module tu dang ky assembly Infrastructure cua minh. Nho vay AtsDbContext nap
// duoc IEntityTypeConfiguration cua ca 3 module ma KHONG phai tham chieu project nao
// trong so do — ranh gioi module van nguyen ven (docs/architecture.md muc 3.3).
AtsDbContextConfigurator.Register(typeof(ATS.Recruitment.Infrastructure.InfrastructureAssemblyMarker).Assembly);
AtsDbContextConfigurator.Register(typeof(ATS.AiScreening.Infrastructure.InfrastructureAssemblyMarker).Assembly);
AtsDbContextConfigurator.Register(typeof(ATS.Identity.Infrastructure.InfrastructureAssemblyMarker).Assembly);

builder.Services.AddDbContext<AtsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

// Auth: cau hinh san nhung TUAN 2 CHUA endpoint nao yeu cau [Authorize].
// Tuan 3 moi bat that khi Identity module xong.
var jwtSecret = builder.Configuration["Jwt:Secret"]
                ?? "dev-only-secret-at-least-32-characters-long!!";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "ats-recruitment",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "ats-recruitment-users",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ---------------------------------------------------------------------------
// Canh bao cau hinh lech — xem bang rui ro trong docs/weekly-plan.md.
// `api` va `worker` PHAI dung cung AiProvider, neu khong ung vien se thay diem
// gia trong khi HR thay diem that cho cung mot ho so.
// ---------------------------------------------------------------------------
var aiProvider = app.Configuration["AiProvider"] ?? "Fake";
app.Logger.LogInformation("AiProvider cua API = {AiProvider}. Gia tri nay PHAI trung voi worker.", aiProvider);

// ---------------------------------------------------------------------------
// Ap migration luc khoi dong — CHI khi duoc bat tuong minh.
//
// Mac dinh TAT. Bat o docker-compose (Database__AutoMigrate=true) de
// `docker compose up` cho ra mot he thong dung duoc ngay, khong phai chay
// thu cong them mot lenh nua.
//
// Chi API lam viec nay, worker thi khong: hai process cung migrate mot luc
// se tranh khoa tren bang __EFMigrationsHistory.
//
// Khi deploy that: TAT co nay, chay `dotnet ef database update` trong buoc
// deploy rieng. Mot service tu doi luoc do database luc khoi dong la thu
// khong ai muon gap luc 2 gio sang.
// ---------------------------------------------------------------------------
if (app.Configuration.GetValue("Database:AutoMigrate", defaultValue: false))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AtsDbContext>();
    await db.Database.MigrateAsync();
    app.Logger.LogInformation("Da ap migration. Schema: identity, recruitment, aiscreening.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

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
