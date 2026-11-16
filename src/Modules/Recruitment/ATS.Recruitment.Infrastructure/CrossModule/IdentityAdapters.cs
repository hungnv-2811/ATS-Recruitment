using ATS.Identity.Application.Ports;
using ATS.Identity.Domain;
using ATS.Identity.Domain.Ports;
using ATS.Recruitment.Application.Ports;
using ATS.Recruitment.Domain;
using ATS.Recruitment.Domain.Ports;

namespace ATS.Recruitment.Infrastructure.CrossModule;

/// <summary>
/// Identity → Recruitment: dang ky tai khoan Candidate/HR thi tao luon ho so nghiep vu.
/// </summary>
/// <remarks>
/// Chi Add vao DbContext dung chung, KHONG SaveChanges: AuthService luu user + ho so trong
/// cung mot SaveChanges (mot transaction).
/// </remarks>
internal sealed class RecruitmentProfileProvisioner : IUserProfileProvisioner
{
    private readonly ICandidateRepository _candidates;
    private readonly IHrProfileRepository _hrProfiles;
    private readonly TimeProvider _clock;

    public RecruitmentProfileProvisioner(ICandidateRepository candidates, IHrProfileRepository hrProfiles, TimeProvider clock)
    {
        _candidates = candidates;
        _hrProfiles = hrProfiles;
        _clock = clock;
    }

    public async Task EnsureProfileAsync(Guid userId, Role role, string fullName, CancellationToken ct = default)
    {
        switch (role)
        {
            case Role.Candidate when await _candidates.GetByUserIdAsync(userId, ct) is null:
                await _candidates.AddAsync(Candidate.Create(userId, fullName, _clock.GetUtcNow()), ct);
                break;
            case Role.Hr when await _hrProfiles.GetByUserIdAsync(userId, ct) is null:
                await _hrProfiles.AddAsync(HrProfile.Create(userId, fullName), ct);
                break;
            default:
                // Admin khong co ho so nghiep vu; da co ho so thi bo qua (idempotent).
                break;
        }
    }
}

/// <summary>Recruitment → Identity: lay email dang nhap cua user (gui loi moi phong van).</summary>
internal sealed class IdentityUserDirectory : IUserDirectory
{
    private readonly IUserRepository _users;

    public IdentityUserDirectory(IUserRepository users) => _users = users;

    public async Task<string?> GetEmailAsync(Guid userId, CancellationToken ct = default)
        => (await _users.GetByIdAsync(userId, ct))?.Email;
}
