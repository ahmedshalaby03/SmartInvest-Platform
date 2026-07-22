using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartInvest.Infrastructure.Identity;

namespace SmartInvest.Infrastructure.Data.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        // حساب دخول واحد بحد أقصى لكل جهة/مقاول
        builder.HasIndex(x => x.ExecutiveAgencyId)
               .IsUnique()
               .HasFilter("[ExecutiveAgencyId] IS NOT NULL");

        builder.HasIndex(x => x.ContractorId)
               .IsUnique()
               .HasFilter("[ContractorId] IS NOT NULL");

        builder.HasOne(x => x.ExecutiveAgency)
               .WithMany()
               .HasForeignKey(x => x.ExecutiveAgencyId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Contractor)
               .WithMany()
               .HasForeignKey(x => x.ContractorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
