using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInvest.Domain.Entities;

namespace SmartInvest.Infrastructure.Data.Configurations;

public class SubProjectConfiguration : IEntityTypeConfiguration<SubProject>
{
    public void Configure(EntityTypeBuilder<SubProject> builder)
    {
        // كود المشروع الفرعي: فريد فقط على المشاريع اللي ليها كود (المعتمدة)،
        // والمقترحين (NULL) مش داخلين في قيد الـ uniqueness
        builder.HasIndex(x => x.SubProjectCode)
               .IsUnique()
               .HasFilter("[SubProjectCode] IS NOT NULL");

        // منع الـ cascade delete اللي ممكن يمسح بيانات التخطيط بالغلط (مطلب في الـ Master Prompt)
        builder.HasOne(x => x.MainProject)
               .WithMany(m => m.SubProjects)
               .HasForeignKey(x => x.MainProjectId)
               .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Status)
       .WithMany(s => s.SubProjects)
       .HasForeignKey(x => x.StatusId)
       .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Priority)
               .WithMany(p => p.SubProjects)
               .HasForeignKey(x => x.PriorityId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}