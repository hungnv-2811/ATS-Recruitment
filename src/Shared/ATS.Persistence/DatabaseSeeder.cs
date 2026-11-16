using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace ATS.Persistence;

/// <summary>
/// Du lieu demo (docs/database-design.md muc 6) — TV1 so huu.
/// </summary>
/// <remarks>
/// <para>
/// Chay sau MigrateAsync khi <c>Database:SeedDemoData=true</c>. Viet bang SQL THO vi ATS.Persistence
/// khong thay entity cua module nao (cung ly do voi bao cao). Ten bang/cot/enum theo
/// database-design.md muc 2.1 — doi entity ma quen file nay thi seed hong luc khoi dong (log loi
/// ro rang, khong am tham).
/// </para>
/// <para>
/// IDEMPOTENT: moi dong co Id CO DINH (<see cref="DemoData"/>) va chen bang
/// <c>ON CONFLICT DO NOTHING</c>; neu tai khoan HR demo dau tien da ton tai thi bo qua ca khoi.
/// Toan bo chen trong MOT transaction (FK xuyen schema la DEFERRABLE — kiem luc COMMIT).
/// </para>
/// <para>
/// Admin KHONG seed o day: <c>AdminSeeder</c> cua Identity (TV2) tao tu <c>Seed:AdminEmail</c>.
/// File CV demo (PDF that) duoc ghi vao <c>FileStorage:RootPath</c> neu chua co, de "tai CV goc" chay duoc.
/// </para>
/// </remarks>
public static class DatabaseSeeder
{
    /// <summary>Mat khau CHUNG cua moi tai khoan demo (HR + ung vien). Chi dung cho dev/demo.</summary>
    public const string DemoPassword = "Demo@123";

    /// <summary>BCrypt (work factor 11, giong BCryptPasswordHasher) cua <see cref="DemoPassword"/> — tinh san.</summary>
    internal const string DemoPasswordHash = "$2a$11$L2kQmaIrYZO9tAODNp2p2e.fLvPrr9htECSmIcNl5uyM1kBwhKIu6";

    /// <summary>Seed neu chua co. Thu muc file CV lay tu <c>FileStorage:RootPath</c> cua ung dung.</summary>
    /// <returns><c>true</c> neu vua chen du lieu; <c>false</c> neu da co san (bo qua).</returns>
    public static Task<bool> SeedAsync(AtsDbContext db, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);
        return SeedAsync(db, ResolveCvRoot(db), ct);
    }

    /// <param name="db">DbContext da migrate.</param>
    /// <param name="cvFileRoot">Thu muc goc cua LocalFileStorage; <c>null</c> = khong ghi file CV.</param>
    /// <param name="ct">Token huy.</param>
    public static async Task<bool> SeedAsync(AtsDbContext db, string? cvFileRoot, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(db);

        var data = DemoData.Build(DateTimeOffset.UtcNow);

        // File truoc, ngoai transaction: idempotent theo ten file, va van tu phuc hoi khi volume
        // cv-files bi xoa ma database con nguyen.
        if (!string.IsNullOrWhiteSpace(cvFileRoot))
        {
            WriteCvFiles(cvFileRoot, data.Cvs);
        }

        // Da seed (theo Id co dinh) HOAC email demo da bi ai dang ky tay → khong dung vao.
        var emails = data.Users.Select(u => u.Email).ToArray();
        var existing = await db.Database
            .SqlQuery<int>($"""SELECT COUNT(*)::int AS "Value" FROM identity.users WHERE id = {data.Users[0].Id} OR email = ANY({emails})""")
            .SingleAsync(ct)
            .ConfigureAwait(false);
        if (existing > 0)
        {
            return false;
        }

        var tx = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        await using (tx.ConfigureAwait(false))
        {
            await InsertAsync(db, data, ct).ConfigureAwait(false);
            await tx.CommitAsync(ct).ConfigureAwait(false);
        }

        return true;
    }

    private static async Task InsertAsync(AtsDbContext db, DemoData d, CancellationToken ct)
    {
        var database = db.Database;

        foreach (var u in d.Users)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO identity.users (id, email, password_hash, role, is_active, created_at, full_name)
                VALUES ({u.Id}, {u.Email}, {DemoPasswordHash}, {u.Role}, TRUE, {d.Now.AddDays(-60)}, {u.FullName})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }

        foreach (var h in d.HrProfiles)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO recruitment.hr_profiles (id, user_id, full_name, department)
                VALUES ({h.Id}, {h.UserId}, {h.FullName}, {h.Department})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }

        foreach (var c in d.Candidates)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO recruitment.candidates (id, user_id, full_name, phone, address, created_at)
                VALUES ({c.Id}, {c.UserId}, {c.FullName}, {c.Phone}, {c.Address}, {d.Now.AddDays(-50)})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }

        foreach (var cv in d.Cvs)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO recruitment.cvs (id, candidate_id, name, file_path, file_type, file_size, extracted_text, is_default, uploaded_at)
                VALUES ({cv.Id}, {cv.CandidateId}, {cv.Name}, {cv.FileName}, 'pdf', {cv.Pdf.Length}, {cv.Text}, {cv.IsDefault}, {d.Now.AddDays(-45)})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }

        foreach (var j in d.Jobs)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO recruitment.jobs (id, hr_owner_id, title, description, requirements, location, employment_type, status, created_at, posted_at, closed_at)
                VALUES ({j.Id}, {j.HrOwnerId}, {j.Title}, {j.Description}, {j.Requirements}, {j.Location}, {j.EmploymentType}, {j.Status}, {j.CreatedAt}, {j.PostedAt}, {j.ClosedAt})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }

        foreach (var a in d.Applications)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO recruitment.applications (id, job_id, candidate_id, cv_id, status, hr_note, applied_at, updated_at)
                VALUES ({a.Id}, {a.JobId}, {a.CandidateId}, {a.CvId}, {a.Status}, {a.HrNote}, {a.AppliedAt}, {a.UpdatedAt})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }

        foreach (var c in d.Cache)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO aiscreening.ai_score_cache (id, cache_key, score, summary, strengths, gaps, adapter_used, model_version, prompt_version, hit_count, created_at, last_hit_at)
                VALUES ({c.Id}, {c.CacheKey}, {c.Score}, {c.Summary}, {c.StrengthsJson}, {c.GapsJson}, {c.AdapterUsed}, {c.ModelVersion}, 'scoring-v1', {c.HitCount}, {d.Now.AddDays(-30)}, {d.Now.AddDays(-2)})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }

        foreach (var s in d.Scores)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO aiscreening.ai_scores (id, application_id, score, summary, strengths, gaps, screening_status, adapter_used, model_version, cache_id, failure_reason, is_human_reviewed, scored_at, updated_at)
                VALUES ({s.Id}, {s.ApplicationId}, {s.Score}, {s.Summary}, {s.StrengthsJson}, {s.GapsJson}, {s.Status}, {s.AdapterUsed}, {s.ModelVersion}, {s.CacheId}, {s.FailureReason}, FALSE, {s.ScoredAt}, {s.UpdatedAt})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }

        foreach (var sj in d.ScreeningJobs)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO aiscreening.screening_jobs (id, job_id, started_by_hr_id, target_count, done_count, failed_count, status, started_at, finished_at)
                VALUES ({sj.Id}, {sj.JobId}, {sj.StartedByHrId}, {sj.TargetCount}, {sj.DoneCount}, {sj.FailedCount}, 'Done', {d.Now.AddDays(-3)}, {d.Now.AddDays(-3).AddMinutes(2)})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }

        foreach (var i in d.Interviews)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO recruitment.interviews (id, application_id, scheduled_at, mode, location_or_link, interviewer_name, status, email_sent, created_at)
                VALUES ({i.Id}, {i.ApplicationId}, {i.ScheduledAt}, {i.Mode}, {i.LocationOrLink}, {i.InterviewerName}, {i.Status}, TRUE, {d.Now.AddDays(-7)})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }

        foreach (var e in d.Evaluations)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO recruitment.evaluations (id, interview_id, rating, comment, recommendation, created_at)
                VALUES ({e.Id}, {e.InterviewId}, {e.Rating}, {e.Comment}, {e.Recommendation}, {d.Now.AddDays(-1)})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }

        foreach (var q in d.Questions)
        {
            await database.ExecuteSqlAsync($"""
                INSERT INTO aiscreening.interview_questions (id, interview_id, category, question, source, order_index, created_at)
                VALUES ({q.Id}, {q.InterviewId}, {q.Category}, {q.Question}, 'Template', {q.OrderIndex}, {d.Now.AddDays(-6)})
                ON CONFLICT DO NOTHING
                """, ct).ConfigureAwait(false);
        }
    }

    private static void WriteCvFiles(string root, IReadOnlyList<DemoData.CvRow> cvs)
    {
        Directory.CreateDirectory(root);
        foreach (var cv in cvs)
        {
            var path = Path.Combine(root, cv.FileName);
            if (!File.Exists(path))
            {
                File.WriteAllBytes(path, cv.Pdf);
            }
        }
    }

    /// <summary>
    /// Lay <c>FileStorage:RootPath</c> tu service provider cua UNG DUNG (khong phai cua EF) — cung
    /// mac dinh voi SharedInfrastructureModule. Khong co IConfiguration (vd. dotnet-ef) → khong ghi file.
    /// </summary>
    private static string? ResolveCvRoot(AtsDbContext db)
    {
        var appServices = db.GetService<IDbContextOptions>()
            .FindExtension<CoreOptionsExtension>()?.ApplicationServiceProvider;
        if (appServices?.GetService(typeof(IConfiguration)) is not IConfiguration configuration)
        {
            return null;
        }

        var root = configuration["FileStorage:RootPath"];
        return string.IsNullOrWhiteSpace(root)
            ? Path.Combine(AppContext.BaseDirectory, "data", "cvs")
            : Path.GetFullPath(root);
    }
}
