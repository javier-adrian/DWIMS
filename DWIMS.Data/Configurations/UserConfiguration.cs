using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DWIMS.Data.Configurations;

internal sealed class UserConfiguration: IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Email).IsUnique();
        
        builder.HasQueryFilter(x => !x.IsDeleted);
        
        builder.HasOne(x => x.Signature)
            .WithOne(y => y.User)
            .HasForeignKey<Signature>(y => y.UserId);

    }
}