using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DWIMS.Data.Configurations;

public class SubmissionConfiguration : IEntityTypeConfiguration<Data.Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.Property(s => s.VerificationCode).HasMaxLength(128);
        builder.HasIndex(s => s.VerificationCode).IsUnique();
    }
}