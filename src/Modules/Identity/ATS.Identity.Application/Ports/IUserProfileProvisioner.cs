using ATS.Identity.Domain;

namespace ATS.Identity.Application.Ports;

/// <summary>
/// Tao ho so nghiep vu (recruitment.candidates / recruitment.hr_profiles) cho tai khoan moi.
/// </summary>
/// <remarks>
/// Port o ben CHU DONG GOI (Identity), adapter o <c>Recruitment.Infrastructure</c> — cung kieu
/// voi <c>IApplicationScreeningTrigger</c>. Nho vay Identity.Domain/Application khong biet gi ve
/// Candidate/HrProfile, va Recruitment.Domain/Application khong biet gi ve User.
///
/// Adapter CHI them entity vao DbContext dung chung, KHONG goi SaveChanges: AuthService luu
/// user + ho so trong CUNG mot SaveChanges (mot transaction) — khong co user mo coi.
/// Idempotent: da co ho so thi bo qua (dung khi Admin doi vai Candidate &lt;-&gt; HR).
/// </remarks>
public interface IUserProfileProvisioner
{
    Task EnsureProfileAsync(Guid userId, Role role, string fullName, CancellationToken ct = default);
}
