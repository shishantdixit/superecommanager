using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperEcomManager.Domain.Entities.Finance;

namespace SuperEcomManager.Infrastructure.Persistence.Configurations.Tenant;

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.ReferenceType)
            .HasMaxLength(50);

        builder.Property(e => e.Vendor)
            .HasMaxLength(200);

        builder.Property(e => e.InvoiceNumber)
            .HasMaxLength(100);

        builder.Property(e => e.Notes)
            .HasMaxLength(1000);

        // Configure Money value object
        builder.OwnsOne(e => e.Amount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("amount")
                .HasPrecision(18, 2);
            money.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3);
        });

        builder.HasIndex(e => e.ExpenseDate);
        builder.HasIndex(e => e.Category);
    }
}
