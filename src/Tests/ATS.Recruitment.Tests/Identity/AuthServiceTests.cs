using ATS.Identity.Application;
using ATS.Identity.Application.Contracts;
using ATS.Identity.Domain;
using ATS.Identity.Infrastructure.Security;
using ATS.Recruitment.Domain;
using ATS.Recruitment.Tests.Support;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATS.Recruitment.Tests.Identity;

public sealed class AuthServiceTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task Dang_ky_ung_vien_tao_user_va_ho_so_candidate_trong_cung_mot_lan_luu()
    {
        var auth = await _app.RegisterAsync("  An.Nguyen@Example.com ", "Candidate", "Nguyen Van An");

        Assert.Equal("an.nguyen@example.com", auth.Email);
        Assert.Equal("Candidate", auth.Role);
        Assert.False(string.IsNullOrEmpty(auth.AccessToken));

        var db = _app.Db();
        var user = await db.Set<User>().SingleAsync();
        Assert.NotEqual(TestApp.Password, user.PasswordHash);
        var candidate = await db.Set<Candidate>().SingleAsync();
        Assert.Equal(auth.UserId, candidate.UserId);
        Assert.Equal("Nguyen Van An", candidate.FullName);
        Assert.Empty(db.Set<HrProfile>());
    }

    [Fact]
    public async Task Dang_ky_HR_tao_ho_so_hr_va_role_la_chuoi_HR()
    {
        var auth = await _app.RegisterAsync("hr@example.com", "hr", "Tran Thi HR");

        Assert.Equal("HR", auth.Role);
        Assert.Equal(auth.UserId, (await _app.Db().Set<HrProfile>().SingleAsync()).UserId);
    }

    [Fact]
    public async Task Dang_ky_trung_email_khong_phan_biet_hoa_thuong_tra_Conflict()
    {
        await _app.RegisterAsync("dup@example.com");

        var result = await _app.Service<IAuthService>()
            .RegisterAsync(new RegisterRequest("DUP@example.com", TestApp.Password, "Nguoi Khac", "Candidate"));

        Assert.True(result.IsFailure);
        Assert.Equal("conflict", result.Error.Code);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Boss")]
    public async Task Tu_dang_ky_khong_duoc_chon_vai_Admin_hay_vai_la(string role)
    {
        var result = await _app.Service<IAuthService>()
            .RegisterAsync(new RegisterRequest("x@example.com", TestApp.Password, "X", role));

        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task Mat_khau_duoi_8_ky_tu_bi_tu_choi()
    {
        var result = await _app.Service<IAuthService>()
            .RegisterAsync(new RegisterRequest("x@example.com", "1234567", "X", "Candidate"));

        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task Dang_nhap_sai_mat_khau_tra_Unauthorized()
    {
        await _app.RegisterAsync("a@example.com");

        var result = await _app.Service<IAuthService>().LoginAsync(new LoginRequest("a@example.com", "sai-mat-khau"));

        Assert.Equal("unauthorized", result.Error.Code);
    }

    [Fact]
    public async Task Dang_nhap_email_khong_ton_tai_tra_Unauthorized()
    {
        var result = await _app.Service<IAuthService>().LoginAsync(new LoginRequest("none@example.com", TestApp.Password));

        Assert.Equal("unauthorized", result.Error.Code);
    }

    [Fact]
    public async Task User_bi_khoa_khong_dang_nhap_duoc()
    {
        var admin = await SeedAdminAsync();
        var user = await _app.RegisterAsync("locked@example.com");
        var locked = await _app.Service<IUserAdminService>()
            .UpdateUserAsync(admin, user.UserId, new UpdateUserRequest(IsActive: false, Role: null));
        Assert.True(locked.IsSuccess);

        var result = await _app.Service<IAuthService>().LoginAsync(new LoginRequest("locked@example.com", TestApp.Password));

        Assert.Equal("unauthorized", result.Error.Code);
    }

    [Fact]
    public async Task Dang_nhap_dung_phat_JWT_co_du_claim_sub_email_name_role()
    {
        await _app.RegisterAsync("hr2@example.com", "HR", "Le HR");

        var result = await _app.Service<IAuthService>().LoginAsync(new LoginRequest("HR2@example.com", TestApp.Password));

        Assert.True(result.IsSuccess);
        var claims = JwtTokenIssuer.ReadClaims(result.Value.AccessToken);
        Assert.Equal(result.Value.UserId.ToString(), claims["sub"]);
        Assert.Equal("hr2@example.com", claims["email"]);
        Assert.Equal("Le HR", claims["name"]);
        Assert.Equal("HR", claims["role"]);
        Assert.Equal(_app.Clock.Now.AddMinutes(120), result.Value.ExpiresAt);
    }

    [Fact]
    public async Task Admin_khong_tu_khoa_chinh_minh()
    {
        var admin = await SeedAdminAsync();

        var result = await _app.Service<IUserAdminService>()
            .UpdateUserAsync(admin, admin, new UpdateUserRequest(IsActive: false, Role: null));

        Assert.Equal("validation", result.Error.Code);
    }

    [Fact]
    public async Task Admin_doi_vai_Candidate_sang_HR_tao_ho_so_HR()
    {
        var admin = await SeedAdminAsync();
        var user = await _app.RegisterAsync("switch@example.com", "Candidate", "Doi Vai");

        var result = await _app.Service<IUserAdminService>()
            .UpdateUserAsync(admin, user.UserId, new UpdateUserRequest(IsActive: null, Role: "HR"));

        Assert.True(result.IsSuccess);
        var users = await _app.Service<IUserAdminService>().ListUsersAsync();
        Assert.Equal("HR", users.Value.Single(u => u.Id == user.UserId).Role);
        Assert.Equal(user.UserId, (await _app.Db().Set<HrProfile>().SingleAsync()).UserId);
    }

    private async Task<Guid> SeedAdminAsync()
    {
        var db = _app.Db();
        var admin = User.Create("admin@example.com", "hash", Role.Admin, "Admin", _app.Clock.Now);
        db.Add(admin);
        await db.SaveChangesAsync();
        return admin.Id;
    }
}

public sealed class PasswordResetTests : IDisposable
{
    private readonly TestApp _app = new();

    public void Dispose() => _app.Dispose();

    [Fact]
    public async Task Quen_mat_khau_voi_email_la_van_thanh_cong_va_khong_gui_mail()
    {
        var result = await _app.Service<IAuthService>().ForgotPasswordAsync(new ForgotPasswordRequest("ghost@example.com"));

        Assert.True(result.IsSuccess);
        Assert.Empty(_app.Email.Sent);
        Assert.Empty(_app.Db().Set<PasswordResetToken>());
    }

    [Fact]
    public async Task Quen_mat_khau_luu_HASH_va_gui_link_ve_Web_BaseUrl()
    {
        await _app.RegisterAsync("a@example.com");

        await _app.Service<IAuthService>().ForgotPasswordAsync(new ForgotPasswordRequest("a@example.com"));

        var mail = Assert.Single(_app.Email.Sent);
        Assert.Equal("a@example.com", mail.ToAddress);
        var token = ExtractToken(mail.PlainTextBody!);
        Assert.StartsWith("http://ui.test/reset-password?token=", mail.PlainTextBody!.Split(' ')[^1], StringComparison.Ordinal);

        var stored = await _app.Db().Set<PasswordResetToken>().SingleAsync();
        Assert.NotEqual(token, stored.TokenHash);
        Assert.Equal(AuthService.HashToken(token), stored.TokenHash);
        Assert.Equal(_app.Clock.Now.AddMinutes(30), stored.ExpiresAt);
    }

    [Fact]
    public async Task SMTP_loi_thi_quen_mat_khau_van_thanh_cong()
    {
        await _app.RegisterAsync("a@example.com");
        _app.Email.Fail = true;

        var result = await _app.Service<IAuthService>().ForgotPasswordAsync(new ForgotPasswordRequest("a@example.com"));

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Dat_lai_mat_khau_thanh_cong_roi_token_dung_lan_2_bi_tu_choi()
    {
        await _app.RegisterAsync("a@example.com");
        var token = await RequestTokenAsync("a@example.com");

        var first = await _app.Service<IAuthService>().ResetPasswordAsync(new ResetPasswordRequest(token, "MatKhauMoi123"));
        var second = await _app.Service<IAuthService>().ResetPasswordAsync(new ResetPasswordRequest(token, "MatKhauKhac456"));

        Assert.True(first.IsSuccess);
        Assert.Equal("validation", second.Error.Code);
        Assert.Equal(AuthService.InvalidResetTokenMessage, second.Error.Message);

        var login = await _app.Service<IAuthService>().LoginAsync(new LoginRequest("a@example.com", "MatKhauMoi123"));
        Assert.True(login.IsSuccess);
        var oldLogin = await _app.Service<IAuthService>().LoginAsync(new LoginRequest("a@example.com", TestApp.Password));
        Assert.True(oldLogin.IsFailure);
    }

    [Fact]
    public async Task Token_het_han_sau_30_phut_bi_tu_choi()
    {
        await _app.RegisterAsync("a@example.com");
        var token = await RequestTokenAsync("a@example.com");

        _app.Clock.Advance(TimeSpan.FromMinutes(31));
        var result = await _app.Service<IAuthService>().ResetPasswordAsync(new ResetPasswordRequest(token, "MatKhauMoi123"));

        Assert.Equal("validation", result.Error.Code);
        Assert.Equal(AuthService.InvalidResetTokenMessage, result.Error.Message);
    }

    [Fact]
    public async Task Dat_lai_mat_khau_vo_hieu_moi_token_khac_cua_user()
    {
        await _app.RegisterAsync("a@example.com");
        var older = await RequestTokenAsync("a@example.com");
        var newer = await RequestTokenAsync("a@example.com");

        Assert.True((await _app.Service<IAuthService>().ResetPasswordAsync(new ResetPasswordRequest(newer, "MatKhauMoi123"))).IsSuccess);
        var withOlder = await _app.Service<IAuthService>().ResetPasswordAsync(new ResetPasswordRequest(older, "MatKhauKhac456"));

        Assert.True(withOlder.IsFailure);
        Assert.All(await _app.Db().Set<PasswordResetToken>().ToListAsync(), t => Assert.NotNull(t.UsedAt));
    }

    [Fact]
    public async Task Token_la_bi_tu_choi()
    {
        var result = await _app.Service<IAuthService>().ResetPasswordAsync(new ResetPasswordRequest("khong-ton-tai", "MatKhauMoi123"));

        Assert.Equal("validation", result.Error.Code);
    }

    private async Task<string> RequestTokenAsync(string email)
    {
        _app.Email.Sent.Clear();
        await _app.Service<IAuthService>().ForgotPasswordAsync(new ForgotPasswordRequest(email));
        return ExtractToken(Assert.Single(_app.Email.Sent).PlainTextBody!);
    }

    private static string ExtractToken(string body)
        => Uri.UnescapeDataString(body[(body.IndexOf("token=", StringComparison.Ordinal) + "token=".Length)..].Trim());
}
