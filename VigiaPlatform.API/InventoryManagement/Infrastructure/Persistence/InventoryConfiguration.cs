namespace VigiaPlatform.API.InventoryManagement.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VigiaPlatform.API.InventoryManagement.Domain.Model;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> builder)
    {
        builder.ToTable("materials");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Name).HasMaxLength(150).IsRequired();
        builder.Property(m => m.Category).HasMaxLength(30).IsRequired();
        builder.Property(m => m.Unit).HasMaxLength(20).IsRequired();
        builder.Property(m => m.CurrentStock).HasPrecision(18, 3);
        builder.Property(m => m.MinimumStock).HasPrecision(18, 3);
        builder.Property(m => m.ProjectId).HasMaxLength(50);
        builder.Property(m => m.ProjectName).HasMaxLength(150).IsRequired();
    }
}

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.MaterialName).HasMaxLength(150).IsRequired();
        builder.Property(m => m.ProjectId).HasMaxLength(50);
        builder.Property(m => m.ProjectName).HasMaxLength(150).IsRequired();
        builder.Property(m => m.Type).HasMaxLength(5).IsRequired();
        builder.Property(m => m.Quantity).HasPrecision(18, 3);
        builder.Property(m => m.Unit).HasMaxLength(20).IsRequired();
        builder.Property(m => m.Date).HasColumnType("date");
        builder.HasOne<Material>().WithMany().HasForeignKey(m => m.MaterialId).OnDelete(DeleteBehavior.Restrict);
    }
}
