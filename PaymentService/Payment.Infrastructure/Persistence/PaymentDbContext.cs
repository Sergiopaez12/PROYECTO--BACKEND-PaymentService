using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Persistence;

public class PaymentDbContext : DbContext
{
    public DbSet<PaymentTransaction> Transactions => Set<PaymentTransaction>();

    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("Transactions");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.OrderId).IsUnique();

            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(e => e.PaymentMethod)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(e => e.TransactionReference)
                .HasMaxLength(100);

            entity.Property(e => e.FailureReason)
                .HasMaxLength(255);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.ProcessedAt);
        });
    }
}
