using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class SpecialityConfiguration : IEntityTypeConfiguration<Speciality>
{
    public void Configure(EntityTypeBuilder<Speciality> builder)
    {
        builder.ToTable("Specialities");

        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Name)
            .IsRequired()
            .HasColumnType("varchar(100)");
        
        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasFilter("[Deleted] = 0");
        
        builder.Property(s => s.Description)
            .IsRequired()
            .HasColumnType("varchar(100)");
        
        builder.Property(s => s.Deleted)
            .HasColumnType("bit")
            .HasDefaultValue(false);
        
        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("datetime2");
        
        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("datetime2");
        
        builder.HasQueryFilter(s => !s.Deleted);
    }
}
