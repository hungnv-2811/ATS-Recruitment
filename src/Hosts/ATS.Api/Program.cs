using System.Text;
using ATS.Persistence;
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

var jwtSecret = builder.Configuration["Jwt:Secret"]
                ?? "dev-only-secret-at-least-32-characters-long!!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ats-recruitment";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ats-recruitment-users";

// Danh sach module truyen thang vao day. Them module thu 4 ma quen dong nay
// thi bang cua no khong duoc tao — nhung it nhat chi co DUNG MOT cho phai sua.
builder.Services.AddAtsPersistence(
    connectionString,
    typeof(ATS.Recruitment.Infrastructure.InfrastructureAssemblyMarker).Assembly,
    typeof(ATS.AiScreening.Infrastructure.InfrastructureAssemblyMarker).Assembly,
    typeof(ATS.Identity.Infrastructure.InfrastructureAssemblyMarker).Assembly);

// Auth: cau hinh san nhung TUAN 2 CHUA endpoint nao yeu cau [Authorize].
// Tuan 3 moi bat that khi Identity module xong.
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
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        };
    });

builder.Services.AddAuthorization();

// Swagger chi phuc vu dev/demo, nen dang ky cung phai nam trong dung dieu kien
// voi luc su dung — khong nap Swashbuckle va hang chuc ServiceDescriptor cua no
// o moi lan khoi dong production cho mot duong ma khong bao gio chay.
var enableSwagger = builder.Environment.IsDevelopment();
if (enableSwagger)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
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
}

if (enableSwagger)
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
