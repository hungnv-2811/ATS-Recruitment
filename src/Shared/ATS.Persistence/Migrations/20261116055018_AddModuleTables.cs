using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ATS.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "aiscreening");

            migrationBuilder.EnsureSchema(
                name: "recruitment");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.CreateTable(
                name: "ai_score_cache",
                schema: "aiscreening",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cache_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    strengths = table.Column<string>(type: "text", nullable: false),
                    gaps = table.Column<string>(type: "text", nullable: false),
                    adapter_used = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    model_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prompt_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    hit_count = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_hit_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_score_cache", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_usage_quotas",
                schema: "aiscreening",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    usage_date = table.Column<DateOnly>(type: "date", nullable: false),
                    preview_count = table.Column<int>(type: "integer", nullable: false),
                    batch_count = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_usage_quotas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "candidates",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "hr_profiles",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    department = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hr_profiles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "interview_questions",
                schema: "aiscreening",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    question = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    order_index = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_questions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "jobs",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    hr_owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    requirements = table.Column<string>(type: "text", nullable: false),
                    location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    employment_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    posted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "screening_jobs",
                schema: "aiscreening",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_by_hr_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_count = table.Column<int>(type: "integer", nullable: false),
                    done_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    started_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_screening_jobs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_scores",
                schema: "aiscreening",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    strengths = table.Column<string>(type: "text", nullable: false),
                    gaps = table.Column<string>(type: "text", nullable: false),
                    screening_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    adapter_used = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    model_version = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    cache_id = table.Column<Guid>(type: "uuid", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_human_reviewed = table.Column<bool>(type: "boolean", nullable: false),
                    scored_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ai_scores", x => x.id);
                    table.ForeignKey(
                        name: "fk_ai_scores_ai_score_cache_cache_id",
                        column: x => x.cache_id,
                        principalSchema: "aiscreening",
                        principalTable: "ai_score_cache",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "cvs",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    file_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    file_size = table.Column<int>(type: "integer", nullable: false),
                    extracted_text = table.Column<string>(type: "text", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cvs", x => x.id);
                    table.ForeignKey(
                        name: "fk_cvs_candidates_candidate_id",
                        column: x => x.candidate_id,
                        principalSchema: "recruitment",
                        principalTable: "candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_reset_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_password_reset_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_password_reset_tokens_user_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "applications",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cv_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    hr_note = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_applications", x => x.id);
                    table.ForeignKey(
                        name: "fk_applications_candidate_candidate_id",
                        column: x => x.candidate_id,
                        principalSchema: "recruitment",
                        principalTable: "candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_applications_cv_cv_id",
                        column: x => x.cv_id,
                        principalSchema: "recruitment",
                        principalTable: "cvs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_applications_job_job_id",
                        column: x => x.job_id,
                        principalSchema: "recruitment",
                        principalTable: "jobs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "interviews",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    location_or_link = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    interviewer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    email_sent = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interviews", x => x.id);
                    table.ForeignKey(
                        name: "fk_interviews_applications_application_id",
                        column: x => x.application_id,
                        principalSchema: "recruitment",
                        principalTable: "applications",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "evaluations",
                schema: "recruitment",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    interview_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    recommendation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_evaluations", x => x.id);
                    table.ForeignKey(
                        name: "fk_evaluations_interview_interview_id",
                        column: x => x.interview_id,
                        principalSchema: "recruitment",
                        principalTable: "interviews",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_ai_score_cache_cache_key",
                schema: "aiscreening",
                table: "ai_score_cache",
                column: "cache_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ai_scores_application_id",
                schema: "aiscreening",
                table: "ai_scores",
                column: "application_id",
                unique: true)
                .Annotation("Npgsql:IndexInclude", new[] { "score", "screening_status" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_scores_cache_id",
                schema: "aiscreening",
                table: "ai_scores",
                column: "cache_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_usage_quotas_user_id_usage_date",
                schema: "aiscreening",
                table: "ai_usage_quotas",
                columns: new[] { "user_id", "usage_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_applications_candidate_id",
                schema: "recruitment",
                table: "applications",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_applications_cv_id",
                schema: "recruitment",
                table: "applications",
                column: "cv_id");

            migrationBuilder.CreateIndex(
                name: "ix_applications_job_id_candidate_id",
                schema: "recruitment",
                table: "applications",
                columns: new[] { "job_id", "candidate_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_applications_job_id_status",
                schema: "recruitment",
                table: "applications",
                columns: new[] { "job_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_candidates_user_id",
                schema: "recruitment",
                table: "candidates",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cvs_candidate_id",
                schema: "recruitment",
                table: "cvs",
                column: "candidate_id");

            migrationBuilder.CreateIndex(
                name: "ix_cvs_default",
                schema: "recruitment",
                table: "cvs",
                column: "candidate_id",
                unique: true,
                filter: "is_default");

            migrationBuilder.CreateIndex(
                name: "ix_evaluations_interview_id",
                schema: "recruitment",
                table: "evaluations",
                column: "interview_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hr_profiles_user_id",
                schema: "recruitment",
                table: "hr_profiles",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_interview_questions_interview_id_order_index",
                schema: "aiscreening",
                table: "interview_questions",
                columns: new[] { "interview_id", "order_index" });

            migrationBuilder.CreateIndex(
                name: "ix_interviews_application_id",
                schema: "recruitment",
                table: "interviews",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_interviews_status_scheduled_at",
                schema: "recruitment",
                table: "interviews",
                columns: new[] { "status", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "ix_jobs_hr_owner_id",
                schema: "recruitment",
                table: "jobs",
                column: "hr_owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_jobs_status",
                schema: "recruitment",
                table: "jobs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_token_hash",
                schema: "identity",
                table: "password_reset_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_password_reset_tokens_user_id",
                schema: "identity",
                table: "password_reset_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_screening_jobs_job_id_started_at",
                schema: "aiscreening",
                table: "screening_jobs",
                columns: new[] { "job_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                schema: "identity",
                table: "users",
                column: "email",
                unique: true);

            // ------------------------------------------------------------------
            // VIET TAY (TV1, pha tich hop) — nhung thu model EF khong the biet:
            //
            // 1. FK XUYEN SCHEMA/MODULE. Module khong thay entity cua nhau nen EF khong sinh
            //    duoc, nhung database van phai giu toan ven. Tat ca DEFERRABLE INITIALLY
            //    DEFERRED: EF khong biet thu tu INSERT giua hai bang nay (vd. dang ky = them
            //    users + candidates trong CUNG mot SaveChanges), nen kiem tra doi den COMMIT.
            //    NO ACTION (khong CASCADE): users/applications khong bao gio bi xoa vat ly.
            //
            // 2. Rang buoc 1 (database-design muc 3): applications.cv_id phai thuoc dung
            //    applications.candidate_id — FK kep (cv_id, candidate_id) → cvs(id, candidate_id),
            //    khong can trigger hay ham.
            // ------------------------------------------------------------------
            migrationBuilder.Sql(CrossModuleConstraintsUp);
        }

        private const string CrossModuleConstraintsUp = """
            ALTER TABLE recruitment.candidates ADD CONSTRAINT fk_candidates_users_user_id
                FOREIGN KEY (user_id) REFERENCES identity.users (id) DEFERRABLE INITIALLY DEFERRED;
            ALTER TABLE recruitment.hr_profiles ADD CONSTRAINT fk_hr_profiles_users_user_id
                FOREIGN KEY (user_id) REFERENCES identity.users (id) DEFERRABLE INITIALLY DEFERRED;
            ALTER TABLE recruitment.jobs ADD CONSTRAINT fk_jobs_users_hr_owner_id
                FOREIGN KEY (hr_owner_id) REFERENCES identity.users (id) DEFERRABLE INITIALLY DEFERRED;
            ALTER TABLE aiscreening.ai_scores ADD CONSTRAINT fk_ai_scores_applications_application_id
                FOREIGN KEY (application_id) REFERENCES recruitment.applications (id) DEFERRABLE INITIALLY DEFERRED;
            ALTER TABLE aiscreening.interview_questions ADD CONSTRAINT fk_interview_questions_interviews_interview_id
                FOREIGN KEY (interview_id) REFERENCES recruitment.interviews (id) DEFERRABLE INITIALLY DEFERRED;
            ALTER TABLE aiscreening.screening_jobs ADD CONSTRAINT fk_screening_jobs_jobs_job_id
                FOREIGN KEY (job_id) REFERENCES recruitment.jobs (id) DEFERRABLE INITIALLY DEFERRED;
            ALTER TABLE aiscreening.ai_usage_quotas ADD CONSTRAINT fk_ai_usage_quotas_users_user_id
                FOREIGN KEY (user_id) REFERENCES identity.users (id) DEFERRABLE INITIALLY DEFERRED;

            CREATE UNIQUE INDEX ux_cvs_id_candidate_id ON recruitment.cvs (id, candidate_id);
            ALTER TABLE recruitment.applications ADD CONSTRAINT fk_applications_cv_owner
                FOREIGN KEY (cv_id, candidate_id) REFERENCES recruitment.cvs (id, candidate_id);
            """;

        private const string CrossModuleConstraintsDown = """
            ALTER TABLE recruitment.applications DROP CONSTRAINT IF EXISTS fk_applications_cv_owner;
            DROP INDEX IF EXISTS recruitment.ux_cvs_id_candidate_id;
            ALTER TABLE aiscreening.ai_usage_quotas DROP CONSTRAINT IF EXISTS fk_ai_usage_quotas_users_user_id;
            ALTER TABLE aiscreening.screening_jobs DROP CONSTRAINT IF EXISTS fk_screening_jobs_jobs_job_id;
            ALTER TABLE aiscreening.interview_questions DROP CONSTRAINT IF EXISTS fk_interview_questions_interviews_interview_id;
            ALTER TABLE aiscreening.ai_scores DROP CONSTRAINT IF EXISTS fk_ai_scores_applications_application_id;
            ALTER TABLE recruitment.jobs DROP CONSTRAINT IF EXISTS fk_jobs_users_hr_owner_id;
            ALTER TABLE recruitment.hr_profiles DROP CONSTRAINT IF EXISTS fk_hr_profiles_users_user_id;
            ALTER TABLE recruitment.candidates DROP CONSTRAINT IF EXISTS fk_candidates_users_user_id;
            """;

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(CrossModuleConstraintsDown);

            migrationBuilder.DropTable(
                name: "ai_scores",
                schema: "aiscreening");

            migrationBuilder.DropTable(
                name: "ai_usage_quotas",
                schema: "aiscreening");

            migrationBuilder.DropTable(
                name: "evaluations",
                schema: "recruitment");

            migrationBuilder.DropTable(
                name: "hr_profiles",
                schema: "recruitment");

            migrationBuilder.DropTable(
                name: "interview_questions",
                schema: "aiscreening");

            migrationBuilder.DropTable(
                name: "password_reset_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "screening_jobs",
                schema: "aiscreening");

            migrationBuilder.DropTable(
                name: "ai_score_cache",
                schema: "aiscreening");

            migrationBuilder.DropTable(
                name: "interviews",
                schema: "recruitment");

            migrationBuilder.DropTable(
                name: "users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "applications",
                schema: "recruitment");

            migrationBuilder.DropTable(
                name: "cvs",
                schema: "recruitment");

            migrationBuilder.DropTable(
                name: "jobs",
                schema: "recruitment");

            migrationBuilder.DropTable(
                name: "candidates",
                schema: "recruitment");
        }
    }
}
