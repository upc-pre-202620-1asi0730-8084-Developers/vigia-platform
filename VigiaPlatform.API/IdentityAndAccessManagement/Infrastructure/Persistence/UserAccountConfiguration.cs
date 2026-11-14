namespace VigiaPlatform.API.IdentityAndAccessManagement.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.IdentityAndAccessManagement.Domain.Model;

public class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("user_accounts");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).HasMaxLength(50).IsRequired();
        builder.Property(u => u.NormalizedUsername).HasMaxLength(50).IsRequired();
        builder.HasIndex(u => u.NormalizedUsername).IsUnique();
        builder.Property(u => u.CompanyId).HasMaxLength(64).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(254).IsRequired();
        builder.Property(u => u.Phone).HasMaxLength(40).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(32).IsRequired();
        builder.Property(u => u.Timezone).HasMaxLength(64).IsRequired();
    }
}
