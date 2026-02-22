using Ledgerly.Application.Auth;
using Ledgerly.Domain.Accounts;
using Ledgerly.Domain.Budget;
using Ledgerly.Domain.Credit;
using Ledgerly.Domain.Debts;
using Ledgerly.Domain.Scenarios;
using Ledgerly.Infrastructure.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Infrastructure.Data;

public class LedgerlyDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    private readonly ICurrentUserService _currentUser;

    public LedgerlyDbContext(DbContextOptions<LedgerlyDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<DebtAccount> DebtAccounts => Set<DebtAccount>();
    public DbSet<Scenario> Scenarios => Set<Scenario>();
    public DbSet<ActualPayment> ActualPayments => Set<ActualPayment>();
    public DbSet<BudgetCategory> BudgetCategories => Set<BudgetCategory>();
    public DbSet<BudgetPlan> BudgetPlans => Set<BudgetPlan>();
    public DbSet<BudgetPlanLine> BudgetPlanLines => Set<BudgetPlanLine>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<CreditProfile> CreditProfiles => Set<CreditProfile>();
    public DbSet<CreditAccountProfile> CreditAccountProfiles => Set<CreditAccountProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Global query filters for user isolation
        modelBuilder.Entity<Account>().HasQueryFilter(a => a.UserId == _currentUser.UserId);
        modelBuilder.Entity<DebtAccount>().HasQueryFilter(a => a.UserId == _currentUser.UserId);
        modelBuilder.Entity<Scenario>().HasQueryFilter(a => a.UserId == _currentUser.UserId);
        modelBuilder.Entity<BudgetCategory>().HasQueryFilter(a => a.UserId == _currentUser.UserId);
        modelBuilder.Entity<BudgetPlan>().HasQueryFilter(a => a.UserId == _currentUser.UserId);
        modelBuilder.Entity<Transaction>().HasQueryFilter(a => a.UserId == _currentUser.UserId);

        // Phase 1: Scenario ↔ DebtAccount many-to-many
        modelBuilder.Entity<Scenario>()
            .HasMany(s => s.DebtAccounts)
            .WithMany(d => d.Scenarios)
            .UsingEntity("ScenarioDebtAccounts");

        // Phase 2: BudgetPlan → BudgetPlanLine (cascade delete)
        modelBuilder.Entity<BudgetPlan>()
            .HasMany(p => p.Lines)
            .WithOne(l => l.BudgetPlan)
            .HasForeignKey(l => l.BudgetPlanId)
            .OnDelete(DeleteBehavior.Cascade);

        // BudgetPlanLine → BudgetCategory (restrict delete)
        modelBuilder.Entity<BudgetPlanLine>()
            .HasOne(l => l.Category)
            .WithMany()
            .HasForeignKey(l => l.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Transaction → BudgetCategory (restrict delete)
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Phase 4: ActualPayment → Scenario (cascade delete) + → DebtAccount (restrict delete)
        modelBuilder.Entity<ActualPayment>()
            .HasOne<Scenario>()
            .WithMany()
            .HasForeignKey(p => p.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActualPayment>()
            .HasOne<DebtAccount>()
            .WithMany()
            .HasForeignKey(p => p.DebtAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        // Phase 5: CreditProfile → Scenario (cascade delete)
        modelBuilder.Entity<CreditProfile>()
            .HasOne<Scenario>()
            .WithMany()
            .HasForeignKey(p => p.ScenarioId)
            .OnDelete(DeleteBehavior.Cascade);

        // CreditAccountProfile → CreditProfile (cascade delete)
        modelBuilder.Entity<CreditAccountProfile>()
            .HasOne<CreditProfile>()
            .WithMany(p => p.Accounts)
            .HasForeignKey(a => a.CreditProfileId)
            .OnDelete(DeleteBehavior.Cascade);

        // CreditAccountProfile → DebtAccount (restrict delete, optional FK)
        modelBuilder.Entity<CreditAccountProfile>()
            .HasOne<DebtAccount>()
            .WithMany()
            .HasForeignKey(a => a.DebtAccountId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
