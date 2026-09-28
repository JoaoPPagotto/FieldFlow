using FieldFlow.Domain.Catalog;
using FieldFlow.Domain.Technicians;
using FieldFlow.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FieldFlow.Infrastructure.Persistence.Configurations;

internal sealed class TechnicianConfiguration : IEntityTypeConfiguration<Technician>
{
    public void Configure(EntityTypeBuilder<Technician> builder)
    {
        builder.ToTable("technicians");
        builder.HasKey(t => t.TechnicianId).HasName("technicians_pkey");
        builder.Property(t => t.TechnicianId).ValueGeneratedNever();

        builder.Property(t => t.Name).HasMaxLength(Technician.NameMaxLength).IsRequired();

        builder.ComplexProperty(t => t.WorkingHours, wh =>
        {
            wh.Property(w => w.Start).HasColumnName("working_hours_start");
            wh.Property(w => w.End).HasColumnName("working_hours_end");
            wh.Property(w => w.Days).HasColumnName("working_days"); // [Flags] → int
        });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .HasConstraintName("fk_technicians_user")
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(t => t.UserId).IsUnique().HasFilter("user_id IS NOT NULL AND " + ConfigurationExtensions.NotDeleted)
            .HasDatabaseName("ux_technicians_user");

        builder.HasMany(t => t.Skills)
            .WithOne()
            .HasForeignKey(s => s.TechnicianId)
            .HasConstraintName("fk_technician_skills_technician")
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(t => t.Skills).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.ConfigureAudit();
    }
}

internal sealed class TechnicianSkillConfiguration : IEntityTypeConfiguration<TechnicianSkill>
{
    public void Configure(EntityTypeBuilder<TechnicianSkill> builder)
    {
        builder.ToTable("technician_skills");
        builder.HasKey(s => new { s.TechnicianId, s.ServiceTypeId }).HasName("technician_skills_pkey");

        builder.HasOne<ServiceType>()
            .WithMany()
            .HasForeignKey(s => s.ServiceTypeId)
            .HasConstraintName("fk_technician_skills_service_type")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
