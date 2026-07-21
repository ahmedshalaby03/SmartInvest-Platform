using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Infrastructure.Data.Configurations;

public class ProjectFollowUpConfiguration : IEntityTypeConfiguration<ProjectFollowUp>
{
    public void Configure(EntityTypeBuilder<ProjectFollowUp> builder)
    {
        builder.HasOne(x => x.Status)
               .WithMany(s => s.ProjectFollowUps)
               .HasForeignKey(x => x.StatusId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}