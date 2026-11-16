using ATS.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ATS.Recruitment.Tests.Persistence;

/// <summary>
/// Sinh script DDL Postgres tu model THAT (Npgsql + snake_case) — khong can ket noi DB.
/// Bat loi cau hinh EF ma provider InMemory bo qua (partial index, unique, ten bang/cot).
/// </summary>
public sealed class EfModelTests
{
    private static readonly string Script = CreateScript();

    [Theory]
    [InlineData("CREATE TABLE identity.users")]
    [InlineData("CREATE TABLE identity.password_reset_tokens")]
    [InlineData("CREATE TABLE recruitment.candidates")]
    [InlineData("CREATE TABLE recruitment.hr_profiles")]
    [InlineData("CREATE TABLE recruitment.cvs")]
    [InlineData("CREATE TABLE recruitment.jobs")]
    [InlineData("CREATE TABLE recruitment.applications")]
    [InlineData("CREATE TABLE recruitment.interviews")]
    [InlineData("CREATE TABLE recruitment.evaluations")]
    public void Bang_dung_schema_va_ten(string ddl)
        => Assert.Contains(ddl, Script, StringComparison.Ordinal);

    [Theory]
    [InlineData("full_name character varying(200) NOT NULL")]
    [InlineData("hr_owner_id uuid NOT NULL")]
    [InlineData("extracted_text text NOT NULL")]
    [InlineData("email_sent boolean NOT NULL")]
    [InlineData("role character varying(20) NOT NULL")]
    public void Cot_snake_case_va_enum_luu_chuoi(string column)
        => Assert.Contains(column, Script, StringComparison.Ordinal);

    [Theory]
    [InlineData("CREATE UNIQUE INDEX ix_users_email ON identity.users (email);")]
    [InlineData("CREATE UNIQUE INDEX ix_cvs_default ON recruitment.cvs (candidate_id) WHERE is_default;")]
    [InlineData("CREATE UNIQUE INDEX ix_applications_job_id_candidate_id ON recruitment.applications (job_id, candidate_id);")]
    [InlineData("CREATE INDEX ix_applications_job_id_status ON recruitment.applications (job_id, status);")]
    [InlineData("CREATE INDEX ix_applications_candidate_id ON recruitment.applications (candidate_id);")]
    [InlineData("CREATE UNIQUE INDEX ix_password_reset_tokens_token_hash ON identity.password_reset_tokens (token_hash);")]
    public void Index_quan_trong_ton_tai(string index)
        => Assert.Contains(index, Script, StringComparison.Ordinal);

    private static string CreateScript()
    {
        var options = new DbContextOptionsBuilder<AtsDbContext>()
            .UseNpgsql("Host=localhost;Database=ats_model_only")
            .UseSnakeCaseNamingConvention()
            .Options;
        using var db = new AtsDbContext(options, new ModuleAssemblies(
            typeof(ATS.Recruitment.Infrastructure.InfrastructureAssemblyMarker).Assembly,
            typeof(ATS.Identity.Infrastructure.InfrastructureAssemblyMarker).Assembly));
        return db.Database.GenerateCreateScript();
    }
}
