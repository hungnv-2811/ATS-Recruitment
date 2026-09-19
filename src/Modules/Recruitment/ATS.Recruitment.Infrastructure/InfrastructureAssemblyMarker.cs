namespace ATS.Recruitment.Infrastructure;

/// <summary>
/// Diem neo cua assembly nay.
/// </summary>
/// <remarks>
/// Hai cong dung: (1) Composition Root dang ky assembly voi
/// <c>AtsDbContextConfigurator</c> de nap IEntityTypeConfiguration cua module;
/// (2) giu project reference khong bi compiler loai bo khi module chua co type nao.
/// </remarks>
public sealed class InfrastructureAssemblyMarker;
