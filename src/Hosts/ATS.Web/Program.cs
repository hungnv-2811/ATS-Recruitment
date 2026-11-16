using ATS.Web.Components;
using ATS.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Web KHONG tham chieu project module nao — no goi API qua HTTP.
// Nho vay giao dien khong bao gio doc thang DbContext.
//
// Doc config ra bien local TRUOC: delegate cua AddHttpClient chay lai o MOI lan
// CreateClient, nen de nguyen ben trong thi moi lan goi API deu duyet lai chuoi
// configuration provider va cap phat mot Uri moi. Ngoai ra closure se giu tham
// chieu toi `builder` suot doi tien trinh thay vi chi giu mot Uri.
var apiBaseUri = new Uri(builder.Configuration["ApiBaseUrl"] ?? "http://localhost:8080");

// Timeout 40s > 30s timeout cung cua score-preview (ADR-2): de API tu roi xuong Keyword
// va tra diem, thay vi Web cat ngang truoc.
builder.Services.AddHttpClient("ats-api", client =>
{
    client.BaseAddress = apiBaseUri;
    client.Timeout = TimeSpan.FromSeconds(40);
});

// Moi circuit (mot tab) co phien dang nhap rieng.
builder.Services.AddScoped<AuthSession>();
builder.Services.AddScoped<ApiClient>();
builder.Services.AddScoped<AuthApi>();
builder.Services.AddScoped<CvApi>();
builder.Services.AddScoped<JobApi>();
builder.Services.AddScoped<ApplicationApi>();
builder.Services.AddScoped<InterviewApi>();
builder.Services.AddScoped<AiApi>();
builder.Services.AddScoped<ReportApi>();
builder.Services.AddScoped<AdminApi>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
