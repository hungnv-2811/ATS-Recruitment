using System.Reflection;
using System.Runtime.CompilerServices;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Ports;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATS.ArchitectureTests;

/// <summary>
/// Nam quy tac kien truc bat buoc (docs/architecture.md muc 3.1).
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

    /// <summary>Public key token cua cac assembly di kem .NET runtime.</summary>
    /// <remarks>
    /// Day la ALLOWLIST, khong phai denylist. Denylist phai doan truoc ten tung
    /// package ha tang (EntityFrameworkCore, Npgsql, ...) nen Dapper, MongoDB.Driver
    /// hay System.Data.SqlClient them vao ngay mai se lot qua ma khong ai hay.
    ///
    /// Nhan dien theo TOKEN chu khong theo ten, vi hai ly do: BCL co nhung assembly
    /// khong mang tien to "System." (vi du Microsoft.Win32.Primitives), va nguoc lai
    /// mot package ben thu ba hoan toan co the tu dat ten "System.Something".
    /// </remarks>
    private static readonly string[] RuntimePublicKeyTokens =
    [
        "b03f5f7f11d50a3a",   // phan lon System.* va Microsoft.Win32.*
        "7cec85d7bea7798e",   // System.Private.CoreLib
        "b77a5c561934e089",   // mscorlib, System
        "cc7b13ffcd2ddd51",   // netstandard
    ];

    public static TheoryData<string, Assembly> DomainAssemblies => new()
    {
        { "ATS.SharedKernel", SharedKernel },
        { "ATS.Recruitment.Domain", RecruitmentDomain },
        { "ATS.AiScreening.Domain", AiScreeningDomain },
        { "ATS.Identity.Domain", IdentityDomain },
    };

    // -----------------------------------------------------------------------
    // QUY TAC 1 — Domain chi duoc phu thuoc BCL va SharedKernel.
    // Phu thuoc luon huong VAO TRONG.
    // -----------------------------------------------------------------------
    [Theory]
    [MemberData(nameof(DomainAssemblies))]
    public void QuyTac1_Domain_chi_phu_thuoc_BCL_va_SharedKernel(string name, Assembly domain)
    {
        var ownName = domain.GetName().Name ?? string.Empty;

        var viPham = TransitiveReferences(domain)
            .Where(referenced => !IsAllowedForDomain(referenced, ownName))
            .Select(referenced => referenced.Name ?? "?")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            viPham.Length == 0,
            $"{name} phu thuoc (ke ca BAC CAU) vao: {string.Join(", ", viPham)}. " +
            "Tang Domain chi duoc cham BCL va ATS.SharedKernel — xem docs/architecture.md muc 3.1.");
    }

    /// <summary>Duyet toan bo bao dong tham chieu, khong chi tham chieu truc tiep.</summary>
    /// <remarks>
    /// Chi doc GetReferencedAssemblies() cua rieng assembly goc la bo sot duong
    /// Domain -> SharedKernel -> EF Core: dung cai kich ban ma muc 3.2 loai bo
    /// bang lap luan, nhung khong co gi ep.
    /// </remarks>
    private static IEnumerable<AssemblyName> TransitiveReferences(Assembly root)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<Assembly>();
        queue.Enqueue(root);

        while (queue.Count > 0)
        {
            foreach (var reference in queue.Dequeue().GetReferencedAssemblies())
            {
                var name = reference.Name ?? string.Empty;
                if (!seen.Add(name))
                {
                    continue;
                }

                yield return reference;

                Assembly? loaded = null;
                try
                {
                    loaded = Assembly.Load(reference);
                }
                catch (Exception e) when (e is FileNotFoundException or BadImageFormatException)
                {
                    // Khong nap duoc thi khong duyet tiep duoc — ten van da duoc kiem o tren.
                }

                if (loaded is not null)
                {
                    queue.Enqueue(loaded);
                }
            }
        }
    }

    private static bool IsAllowedForDomain(AssemblyName reference, string ownAssemblyName)
    {
        var name = reference.Name ?? string.Empty;
        if (name == "ATS.SharedKernel" || name == ownAssemblyName)
        {
            return true;
        }

        var token = Convert.ToHexString(reference.GetPublicKeyToken() ?? []).ToLowerInvariant();
        return RuntimePublicKeyTokens.Contains(token, StringComparer.Ordinal);
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
    // QUY TAC 3 — Ngoai Composition Root, khong type nao trong ATS.Api duoc cham DbContext.
    // -----------------------------------------------------------------------
    // Quet MOI thanh vien (ctor, field, property, tham so method) chu khong chi
    // ctor, va quet moi type chu khong chi type ten *Controller — vi API nay dung
    // Minimal API, co the se khong bao gio co class nao ten Controller.
    //
    // GIOI HAN da biet: neu handler duoc viet thang thanh lambda trong Program.cs
    // thi no nam trong closure cua Composition Root va duoc mien tru o day. Chan
    // duoc ca truong hop do thi phai quet IL (Mono.Cecil) — chua lam o tuan 2.
    // Tu tuan 3, handler phai nam trong class rieng de quy tac nay co hieu luc.
    [Fact]
    public void QuyTac3_Ngoai_Composition_Root_khong_ai_duoc_cham_DbContext()
    {
        var viPham = new List<string>();

        foreach (var type in ApiAssembly.GetTypes().Where(t => !IsCompositionRoot(t)))
        {
            foreach (var ctor in type.GetConstructors())
            {
                viPham.AddRange(ctor.GetParameters()
                    .Where(p => IsDbContext(p.ParameterType))
                    .Select(p => $"{type.Name}(ctor: {p.ParameterType.Name})"));
            }

            viPham.AddRange(type
                .GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => IsDbContext(f.FieldType))
                .Select(f => $"{type.Name}.{f.Name}"));

            viPham.AddRange(type
                .GetProperties(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(p => IsDbContext(p.PropertyType))
                .Select(p => $"{type.Name}.{p.Name}"));

            viPham.AddRange(type
                .GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly)
                .SelectMany(m => m.GetParameters().Select(p => (m, p)))
                .Where(x => IsDbContext(x.p.ParameterType))
                .Select(x => $"{type.Name}.{x.m.Name}({x.p.ParameterType.Name})"));
        }

        Assert.True(
            viPham.Count == 0,
            $"Type trong ATS.Api dang cham thang DbContext: {string.Join(", ", viPham.Distinct())}. " +
            "Phai di qua Application layer.");
    }

    private static bool IsDbContext(Type type) => typeof(DbContext).IsAssignableFrom(type);

    /// <summary>Program + cac type do compiler sinh ra cho top-level statement.</summary>
    private static bool IsCompositionRoot(Type type)
    {
        for (var current = type; current is not null; current = current.DeclaringType)
        {
            if (current.Name == "Program")
            {
                return true;
            }
        }

        return type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
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
            .Distinct(StringComparer.Ordinal)
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
