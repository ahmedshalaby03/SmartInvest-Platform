using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Infrastructure.Data.Configurations;

public class ProjectAssignmentConfiguration : IEntityTypeConfiguration<ProjectAssignment>
{
    public void Configure(EntityTypeBuilder<ProjectAssignment> builder)
    {
        builder.HasOne(x => x.SubProject)
               .WithMany(s => s.ProjectAssignments)
               .HasForeignKey(x => x.SubProjectId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Contractor)
               .WithMany(c => c.ProjectAssignments)
               .HasForeignKey(x => x.ContractorId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ContractType)
               .WithMany(c => c.ProjectAssignments)
               .HasForeignKey(x => x.ContractTypeId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
