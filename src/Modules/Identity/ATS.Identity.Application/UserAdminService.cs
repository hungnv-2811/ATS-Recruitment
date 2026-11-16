using ATS.Identity.Application.Contracts;
using ATS.Identity.Application.Ports;
using ATS.Identity.Domain.Ports;
using ATS.SharedKernel;

namespace ATS.Identity.Application;

/// <summary>Quan tri tai khoan (Admin): xem danh sach, khoa/mo, doi vai.</summary>
public sealed class UserAdminService : IUserAdminService
{
    private readonly IUserRepository _users;
    private readonly IUserProfileProvisioner _profiles;
    private readonly IUnitOfWork _uow;

    public UserAdminService(IUserRepository users, IUserProfileProvisioner profiles, IUnitOfWork uow)
    {
        _users = users;
        _profiles = profiles;
        _uow = uow;
    }

    public async Task<Result<IReadOnlyList<UserDto>>> ListUsersAsync(CancellationToken ct = default)
    {
        var users = await _users.ListAsync(ct);
        IReadOnlyList<UserDto> dtos = users
            .Select(u => new UserDto(u.Id, u.Email, u.FullName, RoleNames.ToName(u.Role), u.IsActive, u.CreatedAt))
            .ToList();
        return Result.Success(dtos);
    }

    public async Task<Result> UpdateUserAsync(Guid adminUserId, Guid userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _users.GetByIdAsync(userId, ct);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("Khong tim thay tai khoan."));
        }

        Domain.Role? newRole = null;
        if (request.Role is not null)
        {
            if (!RoleNames.TryParse(request.Role, out var parsed))
            {
                return Result.Failure(Error.Validation("Vai phai la 'Candidate', 'HR' hoac 'Admin'."));
            }

            newRole = parsed;
        }

        // Admin tu khoa minh / tu ha vai minh = he thong co the khong con Admin nao.
        if (userId == adminUserId && (request.IsActive == false || (newRole is not null && newRole != Domain.Role.Admin)))
        {
            return Result.Failure(Error.Validation("Khong the tu khoa hoac tu doi vai cua chinh minh."));
        }

        if (request.IsActive is { } isActive)
        {
            user.SetActive(isActive);
        }

        if (newRole is { } role && role != user.Role)
        {
            user.ChangeRole(role);
            if (role != Domain.Role.Admin)
            {
                await _profiles.EnsureProfileAsync(user.Id, role, user.FullName, ct);
            }
        }

        await _uow.SaveChangesAsync(ct);
        return Result.Success();
    }
}
