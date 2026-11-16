using ATS.Identity.Domain;
using ATS.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ATS.Identity.Infrastructure.Persistence;

/// <summary>identity.users — xem docs/database-design.md (co them cot full_name).</summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", AtsDbContext.IdentitySchema);
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.Property(u => u.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(200).IsRequired();

        // Enum luu chuoi dung TEN enum ("Candidate" | "Hr" | "Admin"); chuoi "HR" chi xuat hien o API/JWT.
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.IsActive).IsRequired();
        builder.Property(u => u.CreatedAt).IsRequired();

        // Dong cu (truoc khi co cot nay) nhan gia tri ngau nhien tu default cua database, nen
        // migrate xong la token cu (khong co claim sstamp) bi tu choi va user dang nhap lai.
        builder.Property(u => u.SecurityStamp)
            .HasMaxLength(64)
            .IsRequired()
            .HasDefaultValueSql("replace(gen_random_uuid()::text, '-', '')");
    }
}

/// <summary>identity.password_reset_tokens — chi luu hash, unique theo token_hash.</summary>
internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens", AtsDbContext.IdentitySchema);
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique();
        builder.HasIndex(t => t.UserId);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
