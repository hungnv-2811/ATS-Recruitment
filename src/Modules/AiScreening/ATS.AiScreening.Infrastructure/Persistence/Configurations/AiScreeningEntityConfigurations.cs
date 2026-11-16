using System.Text.Json;
using ATS.AiScreening.Domain.Entities;
using ATS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATS.AiScreening.Infrastructure.Persistence.Configurations;

// Bang schema `aiscreening` (docs/database-design.md). Ten cot snake_case do UseSnakeCaseNamingConvention
// sinh tu dong — o day chi dat ten bang, khoa, index, kieu luu enum (CHUOI) va do dai.
// KHONG co FK sang recruitment.applications/interviews trong code: AiScreening khong tham chieu entity
// cua Recruitment; rang buoc do (neu muon) them o migration tich hop.

internal static class StringListColumn
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.General);

    /// <summary>Danh sach chuoi (strengths/gaps) luu thanh JSON trong mot cot text — khop ERD (cot `text`).</summary>
    public static PropertyBuilder<IReadOnlyList<string>> AsJsonText(this PropertyBuilder<IReadOnlyList<string>> property)
        => property
            .HasConversion(
                v => JsonSerializer.Serialize(v, Json),
                s => (IReadOnlyList<string>)(JsonSerializer.Deserialize<string[]>(s, Json) ?? Array.Empty<string>()),
                new ValueComparer<IReadOnlyList<string>>(
                    (a, b) => (a == null && b == null) || (a != null && b != null && a.SequenceEqual(b)),
                    v => v.Aggregate(17, (h, s) => HashCode.Combine(h, s.GetHashCode(StringComparison.Ordinal))),
                    v => v.ToArray()))
            .IsRequired();
}

internal sealed class AiScoreConfiguration : IEntityTypeConfiguration<AiScore>
{
    public void Configure(EntityTypeBuilder<AiScore> builder)
    {
        builder.ToTable("ai_scores", AtsDbContext.AiScreeningSchema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Mot don toi da mot diem; INCLUDE de truy van xep hang chi doc index (database-design muc 4).
        builder.HasIndex(x => x.ApplicationId)
            .IsUnique()
            .IncludeProperties(x => new { x.Score, x.ScreeningStatus });

        builder.Property(x => x.ScreeningStatus).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AdapterUsed).HasMaxLength(20);
        builder.Property(x => x.ModelVersion).HasMaxLength(100);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.Strengths).AsJsonText();
        builder.Property(x => x.Gaps).AsJsonText();

        builder.HasOne<AiScoreCacheEntry>()
            .WithMany()
            .HasForeignKey(x => x.CacheId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class AiScoreCacheEntryConfiguration : IEntityTypeConfiguration<AiScoreCacheEntry>
{
    public void Configure(EntityTypeBuilder<AiScoreCacheEntry> builder)
    {
        builder.ToTable("ai_score_cache", AtsDbContext.AiScreeningSchema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.CacheKey).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.CacheKey).IsUnique();

        builder.Property(x => x.Summary).IsRequired();
        builder.Property(x => x.AdapterUsed).HasMaxLength(20).IsRequired();
        builder.Property(x => x.ModelVersion).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PromptVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Strengths).AsJsonText();
        builder.Property(x => x.Gaps).AsJsonText();
    }
}

internal sealed class ScreeningJobConfiguration : IEntityTypeConfiguration<ScreeningJob>
{
    public void Configure(EntityTypeBuilder<ScreeningJob> builder)
    {
        builder.ToTable("screening_jobs", AtsDbContext.AiScreeningSchema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        builder.Ignore(x => x.ProcessedCount);
        builder.Ignore(x => x.IsFinished);

        // "Lo moi nhat cua tin X" — UI noi lai thanh tien do.
        builder.HasIndex(x => new { x.JobId, x.StartedAt });
    }
}

internal sealed class AiUsageQuotaConfiguration : IEntityTypeConfiguration<AiUsageQuota>
{
    public void Configure(EntityTypeBuilder<AiUsageQuota> builder)
    {
        builder.ToTable("ai_usage_quotas", AtsDbContext.AiScreeningSchema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // Dich cua INSERT ... ON CONFLICT (user_id, usage_date) — thieu index nay cau lenh se loi.
        builder.HasIndex(x => new { x.UserId, x.UsageDate }).IsUnique();
    }
}

internal sealed class InterviewQuestionEntryConfiguration : IEntityTypeConfiguration<InterviewQuestionEntry>
{
    public void Configure(EntityTypeBuilder<InterviewQuestionEntry> builder)
    {
        builder.ToTable("interview_questions", AtsDbContext.AiScreeningSchema);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.Category).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Question).HasMaxLength(InterviewQuestionEntry.MaxQuestionLength).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(20).IsRequired();

        builder.HasIndex(x => new { x.InterviewId, x.OrderIndex });
    }
}
