using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATS.Persistence.Migrations
{
    /// <summary>
    /// Migration dau tien: chi tao 3 schema, chua co bang nao.
    /// </summary>
    /// <remarks>
    /// Tuan 2 chua co entity — dung theo ke hoach. Schema duoc tao truoc de tu tuan 3
    /// moi module chi viec them bang cua minh vao dung cho, khong ai phai sua migration
    /// cua nguoi khac.
    ///
    /// Ba schema nay la "hang rao mem" giua 3 module trong cung mot database
    /// (xem docs/architecture.md muc 3.2). Chung khong chan duoc JOIN cross-module,
    /// nhung lam ranh gioi module HIEN RA trong ERD va trong moi cau query.
    ///
    /// EF khong tu sinh EnsureSchema khi chua co entity nao, nen 3 dong duoi day
    /// viet tay. Tu tuan 3, EF se tu them EnsureSchema cho bang moi.
    /// </remarks>
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // identity  : chi phuc vu xac thuc (users, password_reset_tokens)
            migrationBuilder.EnsureSchema(name: "identity");

            // recruitment: nghiep vu tuyen dung (candidates, hr_profiles, cvs, jobs,
            //              applications, interviews, evaluations)
            migrationBuilder.EnsureSchema(name: "recruitment");

            // aiscreening: lop AI (ai_scores, ai_score_cache, screening_jobs,
            //              interview_questions, ai_usage_quotas)
            migrationBuilder.EnsureSchema(name: "aiscreening");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // DROP SCHEMA khong co CASCADE: neu con bang ben trong thi lenh nay se loi,
            // va do la hanh vi MONG MUON. Rollback migration dau tien tren mot database
            // dang co du lieu phai la mot hanh dong co y thuc, khong phai tai nan.
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS aiscreening;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS recruitment;");
            migrationBuilder.Sql("DROP SCHEMA IF EXISTS identity;");
        }
    }
}
