using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Infrastructure.Data.Configurations;

public class PlanProjectConfiguration : IEntityTypeConfiguration<PlanProject>
{
    public void Configure(EntityTypeBuilder<PlanProject> builder)
    {
        builder.HasOne(x => x.SubProject)
               .WithMany(s => s.PlanProjects)
               .HasForeignKey(x => x.SubProjectId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}