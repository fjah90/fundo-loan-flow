using LoanFlow.Application;
using LoanFlow.Domain;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LoanFlow.Infrastructure;

public sealed class LoanFlowDbContext(DbContextOptions<LoanFlowDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public async Task CommitAsync(CancellationToken ct = default)
    {
        try
        {
            await SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new DuplicateCustomerException("Customer already exists.", ex);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>(b =>
        {
            b.ToTable("Customers");
            b.HasKey(c => c.Id);
            b.Property(c => c.Id).ValueGeneratedNever();
            b.Property(c => c.Ssn).HasConversion(s => s.Value, v => Ssn.Parse(v)).HasMaxLength(9);
            b.HasIndex(c => c.Ssn).IsUnique();
            b.ComplexProperty(c => c.Address, a =>
            {
                // Get-only record properties are not mapped by convention.
                a.Property(x => x.Street);
                a.Property(x => x.City);
                a.Property(x => x.ZipCode);
                a.Property(x => x.State).HasConversion(s => s.Code, v => UsState.Parse(v)).HasMaxLength(2);
            });
            b.HasOne(c => c.Application).WithOne().HasForeignKey<LoanApplication>(a => a.CustomerId);
        });

        modelBuilder.Entity<LoanApplication>(b =>
        {
            b.ToTable("Applications");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).ValueGeneratedNever();
            b.Property(a => a.RequestedAmount).HasPrecision(18, 2);
            b.HasIndex(a => a.CustomerId).IsUnique();
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.ToTable("OutboxMessages");
            b.HasKey(m => m.Id);
            b.Property(m => m.Payload).HasColumnType("jsonb");
        });
    }
}
