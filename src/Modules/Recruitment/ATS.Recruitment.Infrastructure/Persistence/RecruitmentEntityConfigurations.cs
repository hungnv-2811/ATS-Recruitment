using ATS.Persistence;
using ATS.Recruitment.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using JobApplication = ATS.Recruitment.Domain.Application;

namespace ATS.Recruitment.Infrastructure.Persistence;

// Ten bang/schema dung docs/database-design.md; ten cot snake_case tu dong
// (UseSnakeCaseNamingConvention). Enum luu CHUOI dung ten enum.
// Khong co FK sang identity.users (khac schema, khac module): user_id / hr_owner_id chi la Guid.

internal sealed class CandidateConfiguration : IEntityTypeConfiguration<Candidate>
{
    public void Configure(EntityTypeBuilder<Candidate> builder)
    {
        builder.ToTable("candidates", AtsDbContext.RecruitmentSchema);
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.FullName).HasMaxLength(200).IsRequired();
        builder.Property(c => c.Phone).HasMaxLength(30);
        builder.Property(c => c.Address).HasMaxLength(500);
        builder.HasIndex(c => c.UserId).IsUnique();
    }
}

internal sealed class HrProfileConfiguration : IEntityTypeConfiguration<HrProfile>
{
    public void Configure(EntityTypeBuilder<HrProfile> builder)
    {
        builder.ToTable("hr_profiles", AtsDbContext.RecruitmentSchema);
        builder.HasKey(h => h.Id);
        builder.Property(h => h.Id).ValueGeneratedNever();
        builder.Property(h => h.FullName).HasMaxLength(200).IsRequired();
        builder.Property(h => h.Department).HasMaxLength(200);
        builder.HasIndex(h => h.UserId).IsUnique();
    }
}

internal sealed class CvConfiguration : IEntityTypeConfiguration<Cv>
{
    public void Configure(EntityTypeBuilder<Cv> builder)
    {
        builder.ToTable("cvs", AtsDbContext.RecruitmentSchema);
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.Name).HasMaxLength(Cv.NameMaxLength).IsRequired();
        builder.Property(c => c.FilePath).HasMaxLength(500).IsRequired();
        builder.Property(c => c.FileType).HasMaxLength(10).IsRequired();
        builder.Property(c => c.ExtractedText).IsRequired();
        builder.Ignore(c => c.HasText);

        builder.HasOne<Candidate>()
            .WithMany()
            .HasForeignKey(c => c.CandidateId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index thuong (khong ten) cho danh sach CV; index CO TEN ben duoi la index thu hai,
        // khong ghi de len index nay.
        builder.HasIndex(c => c.CandidateId);

        // Rang buoc 4: moi ung vien chi MOT CV mac dinh (partial unique index).
        builder.HasIndex(c => c.CandidateId, "ix_cvs_default")
            .IsUnique()
            .HasFilter("is_default");
    }
}

internal sealed class JobConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("jobs", AtsDbContext.RecruitmentSchema);
        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).ValueGeneratedNever();
        builder.Property(j => j.Title).HasMaxLength(Job.TitleMaxLength).IsRequired();
        builder.Property(j => j.Description).IsRequired();
        builder.Property(j => j.Requirements).IsRequired();
        builder.Property(j => j.Location).HasMaxLength(Job.LocationMaxLength).IsRequired();
        builder.Property(j => j.EmploymentType).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Ignore(j => j.IsOpen);

        builder.HasIndex(j => j.HrOwnerId);
        builder.HasIndex(j => j.Status);
    }
}

internal sealed class ApplicationConfiguration : IEntityTypeConfiguration<JobApplication>
{
    public void Configure(EntityTypeBuilder<JobApplication> builder)
    {
        builder.ToTable("applications", AtsDbContext.RecruitmentSchema);
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(a => a.HrNote).HasMaxLength(JobApplication.HrNoteMaxLength);
        builder.Ignore(a => a.IsFinal);

        // Don khong bao gio bi xoa vat ly (rang buoc 5) → Restrict tren moi FK.
        builder.HasOne<Job>().WithMany().HasForeignKey(a => a.JobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Candidate>().WithMany().HasForeignKey(a => a.CandidateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Cv>().WithMany().HasForeignKey(a => a.CvId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.JobId, a.Status });
        builder.HasIndex(a => a.CandidateId);
        builder.HasIndex(a => a.CvId);

        // Moi ung vien nop MOT don cho moi tin.
        builder.HasIndex(a => new { a.JobId, a.CandidateId }).IsUnique();
    }
}

internal sealed class InterviewConfiguration : IEntityTypeConfiguration<Interview>
{
    public void Configure(EntityTypeBuilder<Interview> builder)
    {
        builder.ToTable("interviews", AtsDbContext.RecruitmentSchema);
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();
        builder.Property(i => i.Mode).HasMaxLength(10).IsRequired();
        builder.Property(i => i.LocationOrLink).HasMaxLength(500).IsRequired();
        builder.Property(i => i.InterviewerName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne<JobApplication>().WithMany().HasForeignKey(i => i.ApplicationId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(i => i.ApplicationId);
        builder.HasIndex(i => new { i.Status, i.ScheduledAt });
    }
}

internal sealed class EvaluationConfiguration : IEntityTypeConfiguration<Evaluation>
{
    public void Configure(EntityTypeBuilder<Evaluation> builder)
    {
        builder.ToTable("evaluations", AtsDbContext.RecruitmentSchema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Comment).HasMaxLength(4000);
        builder.Property(e => e.Recommendation).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne<Interview>().WithMany().HasForeignKey(e => e.InterviewId).OnDelete(DeleteBehavior.Cascade);

        // Moi buoi phong van toi da MOT danh gia.
        builder.HasIndex(e => e.InterviewId).IsUnique();
    }
}
