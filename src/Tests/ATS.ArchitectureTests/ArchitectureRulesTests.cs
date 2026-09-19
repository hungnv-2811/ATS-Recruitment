using System.Reflection;
using System.Runtime.CompilerServices;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATS.ArchitectureTests;

/// <summary>
/// Ba quy tac kien truc bat buoc (docs/architecture.md muc 3.1) + hai quy tac
/// giu cho ADR-3 khong bi pha ngam.
/// </summary>
/// <remarks>
/// Vi pham bat ky quy tac nao o day => CI do => khong merge duoc.
/// Day la cho duy nhat bien "chung em co Clean Architecture" thanh mot thu
/// kiem chung duoc thay vi mot loi hua trong bao cao.
/// </remarks>
public sealed class ArchitectureRulesTests
{
    private static readonly Assembly SharedKernel = typeof(ATS.SharedKernel.Result).Assembly;
    private static readonly Assembly RecruitmentDomain = typeof(ATS.Recruitment.Domain.ApplicationStatus).Assembly;
    private static readonly Assembly AiScreeningDomain = typeof(AnonymizedCv).Assembly;
    private static readonly Assembly IdentityDomain = typeof(ATS.Identity.Domain.Role).Assembly;
    private static readonly Assembly ApiAssembly = typeof(global::Program).Assembly;

    /// <summary>Ten assembly/namespace bi cam xuat hien trong tang Domain.</summary>
    private static readonly string[] InfrastructureMarkers =
    [
        "Infrastructure",
        "Microsoft.EntityFrameworkCore",
        "Npgsql",
        "StackExchange.Redis",
        "OpenAI",
        "Swashbuckle",
        "Microsoft.AspNetCore",
    ];

    public static TheoryData<string, Assembly> DomainAssemblies => new()
    {
        { "ATS.SharedKernel", SharedKernel },
        { "ATS.Recruitment.Domain", RecruitmentDomain },
        { "ATS.AiScreening.Domain", AiScreeningDomain },
        { "ATS.Identity.Domain", IdentityDomain },
    };

    // -----------------------------------------------------------------------
    // QUY TAC 1 — Domain khong duoc reference Infrastructure.
    // Phu thuoc luon huong VAO TRONG.
    // -----------------------------------------------------------------------
    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void QuyTac1_Domain_khong_phu_thuoc_Infrastructure(string name, Assembly domain)
    {
        var viPham = domain.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .Where(n => InfrastructureMarkers.Any(m => n.Contains(m, StringComparison.Ordinal)))
            .Distinct()
            .ToArray();

        Assert.True(
            viPham.Length == 0,
            $"{name} dang phu thuoc vao ha tang: {string.Join(", ", viPham)}. " +
            "Phu thuoc phai huong vao trong — xem docs/architecture.md muc 3.1.");
    }

    // -----------------------------------------------------------------------
    // QUY TAC 2 — Port AI chi nhan AnonymizedCv, khong nhan string hay Cv tho.
    // Day la cach hien thuc RB3 o compile-time (ADR-3).
    // -----------------------------------------------------------------------
    [Theory]
    [InlineData(typeof(IAiScoringService))]
    [InlineData(typeof(IInterviewQuestionGenerator))]
    public void QuyTac2_Port_AI_chi_nhan_AnonymizedCv(Type port)
    {
        foreach (var method in port.GetMethods())
        {
            var thamSo = method.GetParameters();

            Assert.True(
                thamSo.Any(p => p.ParameterType == typeof(AnonymizedCv)),
                $"{port.Name}.{method.Name} phai nhan AnonymizedCv.");

            var thamSoTho = thamSo
                .Where(p => p.ParameterType == typeof(string))
                .Select(p => p.Name ?? "?")
                .ToArray();

            Assert.True(
                thamSoTho.Length == 0,
                $"{port.Name}.{method.Name} nhan tham so string ({string.Join(", ", thamSoTho)}). " +
                "CV tho khong duoc di vao port AI — xem ADR-3.");
        }
    }

    // -----------------------------------------------------------------------
    // QUY TAC 3 — Controller khong duoc dung thang DbContext.
    // -----------------------------------------------------------------------
    // Tuan 2 chua co controller nao nen phep kiem nay con rong. No bat dau co
    // tac dung tu tuan 3, khi ATS.Api co controller dau tien.
    [Fact]
    public void QuyTac3_Controller_khong_duoc_inject_DbContext()
    {
        var controllers = ApiAssembly.GetTypes()
            .Where(t => t.Name.EndsWith("Controller", StringComparison.Ordinal))
            .ToArray();

        var viPham = new List<string>();

        foreach (var controller in controllers)
        {
            foreach (var ctor in controller.GetConstructors())
            {
                viPham.AddRange(ctor.GetParameters()
                    .Where(p => typeof(DbContext).IsAssignableFrom(p.ParameterType))
                    .Select(p => $"{controller.Name}(ctor: {p.ParameterType.Name})"));
            }

            viPham.AddRange(controller
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => typeof(DbContext).IsAssignableFrom(f.FieldType))
                .Select(f => $"{controller.Name}.{f.Name}"));
        }

        Assert.True(
            viPham.Count == 0,
            $"Controller dang dung thang DbContext: {string.Join(", ", viPham)}. " +
            "Phai di qua Application layer.");
    }

    // -----------------------------------------------------------------------
    // QUY TAC 4 — Recruitment khong duoc biet gi ve AnonymizedCv.
    // Giu cho ranh gioi module (muc 3.3) va cho ban than ADR-3 con dung.
    // -----------------------------------------------------------------------
    [Fact]
    public void QuyTac4_Recruitment_khong_tham_chieu_AiScreening()
    {
        var viPham = RecruitmentDomain.GetReferencedAssemblies()
            .Concat(typeof(ATS.Recruitment.Application.Ports.IApplicationScreeningTrigger)
                .Assembly.GetReferencedAssemblies())
            .Select(a => a.Name ?? string.Empty)
            .Where(n => n.Contains("AiScreening", StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        Assert.True(
            viPham.Length == 0,
            $"Recruitment dang tham chieu {string.Join(", ", viPham)}. " +
            "Cross-module phai di qua IApplicationScreeningTrigger — xem muc 3.3.");
    }

    // -----------------------------------------------------------------------
    // QUY TAC 5 — Bao ve co che internal ctor cua AnonymizedCv.
    // Day la quy tac de bi pha ngam nhat: chi can mot dong InternalsVisibleTo
    // la toan bo bao dam compile-time cua RB3 bien mat ma khong ai nhan ra.
    // -----------------------------------------------------------------------
    [Fact]
    public void QuyTac5_AnonymizedCv_khong_the_tao_tu_ben_ngoai()
    {
        var ctorCong = typeof(AnonymizedCv)
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        Assert.True(
            ctorCong.Length == 0,
            "AnonymizedCv dang co constructor public. Phai la internal — xem ADR-3.");

        var moRong = AiScreeningDomain
            .GetCustomAttributes<InternalsVisibleToAttribute>()
            .Select(a => a.AssemblyName)
            .ToArray();

        Assert.True(
            moRong.Length == 0,
            $"ATS.AiScreening.Domain dang mo InternalsVisibleTo cho: {string.Join(", ", moRong)}. " +
            "Lam vay la vut bo bao dam compile-time cua RB3. Anonymizer bang LLM phai " +
            "di qua IPiiRedactor thay vi implement IAnonymizer o Infrastructure.");
    }
}

