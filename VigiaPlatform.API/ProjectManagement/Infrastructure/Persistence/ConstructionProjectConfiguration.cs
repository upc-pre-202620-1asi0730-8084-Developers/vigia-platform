namespace VigiaPlatform.API.ProjectManagement.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.ProjectManagement.Domain.Model;

public class ConstructionProjectConfiguration : IEntityTypeConfiguration<ConstructionProject>
{
    public void Configure(EntityTypeBuilder<ConstructionProject> builder)
    {
        builder.ToTable("projects");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Zone).HasMaxLength(40).IsRequired();
        builder.Property(p => p.City).HasMaxLength(100).IsRequired();
        builder.Property(p => p.ManagerId).HasMaxLength(50);
        builder.Property(p => p.ManagerName).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Progress).HasPrecision(5, 2);
        builder.Property(p => p.Status).HasMaxLength(30).IsRequired();
    }
}
