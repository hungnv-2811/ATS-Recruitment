using System.Reflection;
using ATS.AiScreening.Domain;
using ATS.AiScreening.Domain.Ports;
using Xunit;

namespace ATS.ArchitectureTests;

/// <summary>
/// Quy tac bo sung (tuan 5+) cho ranh gioi cua module AiScreening — TV4.
/// </summary>
/// <remarks>
/// Nam quy tac goc o <see cref="ArchitectureRulesTests"/> giu nguyen. O day them nhung thu
/// chi lo ra khi module AI co code that: doc du lieu tuyen dung di qua dau, SDK AI duoc phep
/// nam o tang nao, va ai duoc phep "lam" anonymizer.
/// </remarks>
public sealed class AiScreeningBoundaryTests
{
    private static readonly Assembly AiDomain = typeof(AnonymizedCv).Assembly;
    private static readonly Assembly AiApplication = typeof(ATS.AiScreening.Application.AiScreeningOptions).Assembly;
    private static readonly Assembly AiInfrastructure = typeof(ATS.AiScreening.Infrastructure.InfrastructureAssemblyMarker).Assembly;
    private static readonly Assembly ApiAssembly = typeof(global::Program).Assembly;

    // -----------------------------------------------------------------------
    // QUY TAC 6 — Domain/Application cua AiScreening khong biet Recruitment ton tai.
    // Du lieu tuyen dung di qua port rieng IRecruitmentReader; adapter o Infrastructure.
    // -----------------------------------------------------------------------
    [Fact]
    public void QuyTac6_Domain_va_Application_AiScreening_khong_tham_chieu_Recruitment()
    {
        var viPham = new[] { AiDomain, AiApplication }
            .SelectMany(a => TransitiveReferenceNames(a).Select(n => $"{a.GetName().Name} -> {n}"))
            .Where(x => x.Contains("ATS.Recruitment", StringComparison.Ordinal)
                        || x.Contains("ATS.Identity", StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            viPham.Length == 0,
            $"Vi pham ranh gioi module: {string.Join(", ", viPham)}. " +
            "AiScreening chi doc Recruitment qua IRecruitmentQueries, va chi tu Infrastructure.");
    }

    // -----------------------------------------------------------------------
    // QUY TAC 7 — Trong AiScreening, CHI Infrastructure duoc cham Recruitment, va chi
    // cham Recruitment.Application (contract), khong bao gio Domain/Infrastructure cua no.
    // -----------------------------------------------------------------------
    [Fact]
    public void QuyTac7_AiScreening_Infrastructure_chi_tham_chieu_Recruitment_Application()
    {
        var truocTiep = AiInfrastructure.GetReferencedAssemblies()
            .Select(a => a.Name ?? string.Empty)
            .ToArray();

        var cam = truocTiep
            .Where(n => n is "ATS.Recruitment.Domain" or "ATS.Recruitment.Infrastructure"
                        || n.StartsWith("ATS.Identity", StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            cam.Length == 0,
            $"AiScreening.Infrastructure tham chieu truc tiep {string.Join(", ", cam)}. " +
            "Chi duoc dung contract o ATS.Recruitment.Application.");
    }

    // -----------------------------------------------------------------------
    // QUY TAC 8 — SDK ha tang (EF, Npgsql, OpenAI, Redis) chi o Infrastructure.
    // Application doi adapter (OpenAI -> Fake, Redis -> InMemory) ma khong phai sua dong nao.
    // -----------------------------------------------------------------------
    [Fact]
    public void QuyTac8_AiScreening_Application_khong_phu_thuoc_SDK_ha_tang()
    {
        string[] cam = ["Microsoft.EntityFrameworkCore", "Npgsql", "OpenAI", "StackExchange.Redis", "System.ClientModel", "ATS.Persistence"];

        var viPham = TransitiveReferenceNames(AiApplication)
            .Where(n => cam.Any(prefix => n.StartsWith(prefix, StringComparison.Ordinal)))
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            viPham.Length == 0,
            $"ATS.AiScreening.Application phu thuoc (ke ca bac cau) vao: {string.Join(", ", viPham)}. " +
            "SDK ha tang chi duoc nam o AiScreening.Infrastructure.");
    }

    // -----------------------------------------------------------------------
    // QUY TAC 9 — Khong ai ngoai AiScreening.Domain duoc hien thuc IAnonymizer.
    // Hien thuc o Infrastructure = phai mo InternalsVisibleTo de tao AnonymizedCv (pha ADR-3).
    // LLM muon go PII thi hien thuc IPiiRedactor (chi string).
    // -----------------------------------------------------------------------
    [Fact]
    public void QuyTac9_Chi_AiScreening_Domain_hien_thuc_IAnonymizer()
    {
        var viPham = new[] { AiApplication, AiInfrastructure, ApiAssembly }
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IAnonymizer).IsAssignableFrom(t) && !t.IsInterface)
            .Select(t => t.FullName ?? t.Name)
            .ToArray();

        Assert.True(
            viPham.Length == 0,
            $"Hien thuc IAnonymizer ngoai Domain: {string.Join(", ", viPham)}. Dung IPiiRedactor — xem ADR-3.");
    }

    // -----------------------------------------------------------------------
    // QUY TAC 10 — Moi port trong AiScreening.Domain.Ports nhan AnonymizedCv thi KHONG nhan them string.
    // Quy tac 2 chi kiem 2 port co ten; port AI moi them sau nay (vd IAiSummaryService) cung phai theo.
    // -----------------------------------------------------------------------
    [Fact]
    public void QuyTac10_Moi_port_nhan_AnonymizedCv_khong_nhan_string()
    {
        var viPham = AiDomain.GetTypes()
            .Where(t => t.IsInterface && t.Namespace == typeof(IAiScoringService).Namespace)
            .SelectMany(t => t.GetMethods().Select(m => (t, m)))
            .Where(x => x.m.GetParameters().Any(p => p.ParameterType == typeof(AnonymizedCv))
                        && x.m.GetParameters().Any(p => p.ParameterType == typeof(string)))
            .Select(x => $"{x.t.Name}.{x.m.Name}")
            .ToArray();

        Assert.True(
            viPham.Length == 0,
            $"Port AI nhan ca AnonymizedCv lan string: {string.Join(", ", viPham)}. CV tho co the lot qua tham so string.");
    }

    private static IEnumerable<string> TransitiveReferenceNames(Assembly root)
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

                yield return name;

                Assembly? loaded = null;
                try
                {
                    loaded = Assembly.Load(reference);
                }
                catch (Exception e) when (e is FileNotFoundException or BadImageFormatException)
                {
                    // Khong nap duoc thi khong duyet tiep — ten da duoc kiem o tren.
                }

                if (loaded is not null && !IsRuntime(loaded))
                {
                    queue.Enqueue(loaded);
                }
            }
        }
    }

    private static bool IsRuntime(Assembly assembly)
    {
        var name = assembly.GetName().Name ?? string.Empty;
        return name.StartsWith("System", StringComparison.Ordinal) || name is "mscorlib" or "netstandard";
    }
}
