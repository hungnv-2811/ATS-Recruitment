using ATS.Identity.Application;
using ATS.Identity.Application.Contracts;
using ATS.Identity.Domain;
using ATS.Identity.Infrastructure.Security;
using ATS.Recruitment.Tests.Support;
using Xunit;

namespace ATS.Recruitment.Tests.Identity;

/// <summary>
/// Thu hoi JWT: khoa tai khoan / doi vai / doi mat khau phai lam token CU het hieu luc ngay.
/// </summary>
public sealed class SecurityStampTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    [Fact]
    public void Tai_khoan_moi_co_stamp_va_moi_tai_khoan_mot_stamp_khac_nhau()
    {
        var a = User.Create("a@example.com", "hash", Role.Candidate, "A", DateTimeOffset.UtcNow);
        var b = User.Create("b@example.com", "hash", Role.Candidate, "B", DateTimeOffset.UtcNow);

        Assert.False(string.IsNullOrEmpty(a.SecurityStamp));
        Assert.NotEqual(a.SecurityStamp, b.SecurityStamp);
    }

    [Fact]
    public void Khoa_doi_vai_doi_mat_khau_deu_xoay_stamp()
    {
        var user = User.Create("u@example.com", "hash", Role.Candidate, "U", DateTimeOffset.UtcNow);

        var s0 = user.SecurityStamp;
        user.SetActive(false);
        var s1 = user.SecurityStamp;
        user.ChangeRole(Role.Hr);
        var s2 = user.SecurityStamp;
        user.ChangePassword("new-hash");
        var s3 = user.SecurityStamp;

        Assert.Equal(4, new[] { s0, s1, s2, s3 }.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Gan_lai_cung_gia_tri_khong_xoay_stamp_tranh_dang_xuat_oan()
    {
        var user = User.Create("u@example.com", "hash", Role.Hr, "U", DateTimeOffset.UtcNow);
        var before = user.SecurityStamp;

        user.SetActive(true);
        user.ChangeRole(Role.Hr);

        Assert.Equal(before, user.SecurityStamp);
    }

    [Fact]
    public async Task Token_dang_nhap_mang_claim_sstamp_trung_stamp_trong_database()
    {
        var auth = await _app.RegisterAsync("c@example.com");

        var claims = JwtTokenIssuer.ReadClaims(auth.AccessToken);

        Assert.True(await Validator().IsValidAsync(auth.UserId, claims[TokenClaims.SecurityStamp]));
    }

    [Fact]
    public async Task Token_khong_co_stamp_hoac_stamp_sai_bi_tu_choi()
    {
        var auth = await _app.RegisterAsync("c@example.com");

        Assert.False(await Validator().IsValidAsync(auth.UserId, null));
        Assert.False(await Validator().IsValidAsync(auth.UserId, "stamp-gia"));
        Assert.False(await Validator().IsValidAsync(Guid.NewGuid(), "bat-ky"));
    }

    [Fact]
    public async Task Admin_khoa_tai_khoan_thi_token_cu_het_hieu_luc_mo_lai_van_phai_dang_nhap_lai()
    {
        var auth = await _app.RegisterAsync("c@example.com");
        var oldStamp = JwtTokenIssuer.ReadClaims(auth.AccessToken)[TokenClaims.SecurityStamp];
        var admins = _app.Service<IUserAdminService>();

        await admins.UpdateUserAsync(Guid.NewGuid(), auth.UserId, new UpdateUserRequest(IsActive: false, Role: null));
        Assert.False(await Validator().IsValidAsync(auth.UserId, oldStamp));

        await _app.Service<IUserAdminService>().UpdateUserAsync(Guid.NewGuid(), auth.UserId, new UpdateUserRequest(IsActive: true, Role: null));
        Assert.False(await Validator().IsValidAsync(auth.UserId, oldStamp));

        var relogin = await _app.Service<IAuthService>().LoginAsync(new LoginRequest("c@example.com", TestApp.Password));
        Assert.True(await Validator().IsValidAsync(auth.UserId, JwtTokenIssuer.ReadClaims(relogin.Value.AccessToken)[TokenClaims.SecurityStamp]));
    }

    [Fact]
    public async Task Doi_vai_thi_token_mang_vai_cu_het_hieu_luc()
    {
        var auth = await _app.RegisterAsync("c@example.com");
        var oldStamp = JwtTokenIssuer.ReadClaims(auth.AccessToken)[TokenClaims.SecurityStamp];

        await _app.Service<IUserAdminService>().UpdateUserAsync(Guid.NewGuid(), auth.UserId, new UpdateUserRequest(IsActive: null, Role: "HR"));

        Assert.False(await Validator().IsValidAsync(auth.UserId, oldStamp));
    }

    private ITokenSessionValidator Validator() => _app.Service<ITokenSessionValidator>();
}
