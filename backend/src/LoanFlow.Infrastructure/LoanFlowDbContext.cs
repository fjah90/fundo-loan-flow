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
        // AR-67: touch the Customer root so xmin is checked on every commit, even if only the application row or nothing changed.
        foreach (var entry in ChangeTracker.Entries<Customer>().Where(e => e.State == EntityState.Unchanged))
            entry.State = EntityState.Modified;

        try
        {
            await SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new CustomerConflictException("Customer already exists.", ex);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new CustomerConflictException("Customer was changed concurrently.", ex);
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
            b.Property<uint>("Version").IsRowVersion(); // maps to PostgreSQL xmin (AR-67)
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
