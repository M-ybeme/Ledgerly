using Ledgerly.Application.Auth;
using Ledgerly.Domain.Accounts;
using Ledgerly.Domain.Budget;
using Ledgerly.Domain.Debts;
using Ledgerly.Domain.Income;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ledgerly.Tests.Integration;

public class UserIsolationTests
{
    // ── Mutable current-user stub ─────────────────────────────────────────
    // The global query filters in LedgerlyDbContext capture this reference at
    // OnModelCreating time, then evaluate UserId on each query — so switching
    // the Guid here switches which user's rows are visible.
    private sealed class TestCurrentUser : ICurrentUserService
    {
        public Guid UserId { get; set; }
    }

    private static readonly Guid UserA = Guid.NewGuid();
    private static readonly Guid UserB = Guid.NewGuid();

    /// <summary>
    /// Builds a fresh in-memory DbContext + a mutable current-user stub.
    /// Each call gets its own isolated in-memory database.
    /// </summary>
    private static (LedgerlyDbContext db, TestCurrentUser currentUser) BuildDb()
    {
        var currentUser = new TestCurrentUser { UserId = UserA };
        var options = new DbContextOptionsBuilder<LedgerlyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return (new LedgerlyDbContext(options, currentUser), currentUser);
    }

    /// <summary>Seed two debt accounts — one per user — directly into the store.</summary>
    private static async Task SeedDebtsAsync(LedgerlyDbContext db)
    {
        db.DebtAccounts.AddRange(
            new DebtAccount { Id = Guid.NewGuid(), UserId = UserA, Name = "User-A Visa",   Balance = 1000m, AnnualInterestRate = 0.20m, MinimumPayment = 25m, CreatedUtc = DateTime.UtcNow },
            new DebtAccount { Id = Guid.NewGuid(), UserId = UserB, Name = "User-B MasterCard", Balance = 2000m, AnnualInterestRate = 0.22m, MinimumPayment = 40m, CreatedUtc = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedAccountsAsync(LedgerlyDbContext db)
    {
        db.Accounts.AddRange(
            new Account { Id = Guid.NewGuid(), UserId = UserA, Name = "User-A Checking", Type = AccountType.Checking, CreatedUtc = DateTime.UtcNow },
            new Account { Id = Guid.NewGuid(), UserId = UserB, Name = "User-B Savings",  Type = AccountType.Savings,  CreatedUtc = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedIncomeAsync(LedgerlyDbContext db)
    {
        db.IncomeSources.AddRange(
            new IncomeSource { Id = Guid.NewGuid(), UserId = UserA, Name = "User-A Job", Amount = 3000m, Frequency = PayFrequency.Monthly, CreatedUtc = DateTime.UtcNow },
            new IncomeSource { Id = Guid.NewGuid(), UserId = UserB, Name = "User-B Job", Amount = 4000m, Frequency = PayFrequency.Monthly, CreatedUtc = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();
    }

    private static async Task SeedExpensesAsync(LedgerlyDbContext db)
    {
        db.PlannedExpenses.AddRange(
            new PlannedExpense { Id = Guid.NewGuid(), UserId = UserA, Description = "User-A Rent",  PlannedAmount = 1200m, DueDate = new DateOnly(2026, 3, 1), Priority = ExpensePriority.MustPay },
            new PlannedExpense { Id = Guid.NewGuid(), UserId = UserB, Description = "User-B Rent",  PlannedAmount = 1500m, DueDate = new DateOnly(2026, 3, 1), Priority = ExpensePriority.MustPay }
        );
        await db.SaveChangesAsync();
    }

    // ── Debt Account Isolation ────────────────────────────────────────────

    [Fact]
    public async Task DebtAccounts_UserA_OnlySeesOwnDebts()
    {
        var (db, cu) = BuildDb();
        await SeedDebtsAsync(db);

        cu.UserId = UserA;
        var debts = await db.DebtAccounts.ToListAsync();

        Assert.Single(debts);
        Assert.Equal("User-A Visa", debts[0].Name);
    }

    [Fact]
    public async Task DebtAccounts_UserB_OnlySeesOwnDebts()
    {
        var (db, cu) = BuildDb();
        await SeedDebtsAsync(db);

        cu.UserId = UserB;
        var debts = await db.DebtAccounts.ToListAsync();

        Assert.Single(debts);
        Assert.Equal("User-B MasterCard", debts[0].Name);
    }

    [Fact]
    public async Task DebtAccounts_GetById_UserACannotSeeUserBRecord()
    {
        var (db, cu) = BuildDb();

        // Seed and capture User B's ID
        var userBDebtId = Guid.NewGuid();
        db.DebtAccounts.Add(new DebtAccount { Id = userBDebtId, UserId = UserB, Name = "User-B Card", Balance = 500m, AnnualInterestRate = 0.18m, MinimumPayment = 15m, CreatedUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();

        // Query as User A
        cu.UserId = UserA;
        var result = await db.DebtAccounts.FirstOrDefaultAsync(d => d.Id == userBDebtId);

        // Filter makes it invisible — returns null, not an exception
        Assert.Null(result);
    }

    // ── Account Isolation ─────────────────────────────────────────────────

    [Fact]
    public async Task Accounts_UserA_OnlySeesOwnAccounts()
    {
        var (db, cu) = BuildDb();
        await SeedAccountsAsync(db);

        cu.UserId = UserA;
        var accounts = await db.Accounts.ToListAsync();

        Assert.Single(accounts);
        Assert.Equal("User-A Checking", accounts[0].Name);
    }

    [Fact]
    public async Task Accounts_UserB_OnlySeesOwnAccounts()
    {
        var (db, cu) = BuildDb();
        await SeedAccountsAsync(db);

        cu.UserId = UserB;
        var accounts = await db.Accounts.ToListAsync();

        Assert.Single(accounts);
        Assert.Equal("User-B Savings", accounts[0].Name);
    }

    // ── Income Source Isolation ───────────────────────────────────────────

    [Fact]
    public async Task IncomeSources_UserA_OnlySeesOwnSources()
    {
        var (db, cu) = BuildDb();
        await SeedIncomeAsync(db);

        cu.UserId = UserA;
        var sources = await db.IncomeSources.ToListAsync();

        Assert.Single(sources);
        Assert.Equal("User-A Job", sources[0].Name);
    }

    [Fact]
    public async Task IncomeSources_UserB_OnlySeesOwnSources()
    {
        var (db, cu) = BuildDb();
        await SeedIncomeAsync(db);

        cu.UserId = UserB;
        var sources = await db.IncomeSources.ToListAsync();

        Assert.Single(sources);
        Assert.Equal("User-B Job", sources[0].Name);
    }

    // ── Planned Expense Isolation ─────────────────────────────────────────

    [Fact]
    public async Task PlannedExpenses_UserA_OnlySeesOwnExpenses()
    {
        var (db, cu) = BuildDb();
        await SeedExpensesAsync(db);

        cu.UserId = UserA;
        var expenses = await db.PlannedExpenses.ToListAsync();

        Assert.Single(expenses);
        Assert.Equal("User-A Rent", expenses[0].Description);
    }

    [Fact]
    public async Task PlannedExpenses_UserB_OnlySeesOwnExpenses()
    {
        var (db, cu) = BuildDb();
        await SeedExpensesAsync(db);

        cu.UserId = UserB;
        var expenses = await db.PlannedExpenses.ToListAsync();

        Assert.Single(expenses);
        Assert.Equal("User-B Rent", expenses[0].Description);
    }

    // ── Total count sanity check ──────────────────────────────────────────

    [Fact]
    public async Task AllEntities_TotalInStore_ExceedsFilteredCount()
    {
        // Verify that the data IS in the store but the filter hides it.
        // IgnoreQueryFilters() bypasses the global filter.
        var (db, cu) = BuildDb();
        await SeedDebtsAsync(db);

        cu.UserId = UserA;
        var filteredCount   = await db.DebtAccounts.CountAsync();
        var unfilteredCount = await db.DebtAccounts.IgnoreQueryFilters().CountAsync();

        Assert.Equal(1, filteredCount);   // only User A's
        Assert.Equal(2, unfilteredCount); // both users'
    }

    // ── Financial summary scoping ─────────────────────────────────────────

    [Fact]
    public async Task MixedEntities_SwitchingUsers_IsolatesAllEntityTypes()
    {
        var (db, cu) = BuildDb();
        await SeedDebtsAsync(db);
        await SeedAccountsAsync(db);
        await SeedIncomeAsync(db);
        await SeedExpensesAsync(db);

        // User A sees only their data across all entity types
        cu.UserId = UserA;
        Assert.Equal(1, await db.DebtAccounts.CountAsync());
        Assert.Equal(1, await db.Accounts.CountAsync());
        Assert.Equal(1, await db.IncomeSources.CountAsync());
        Assert.Equal(1, await db.PlannedExpenses.CountAsync());

        // Switching to User B immediately shows User B's data without rebuilding the context
        cu.UserId = UserB;
        Assert.Equal(1, await db.DebtAccounts.CountAsync());
        Assert.Equal(1, await db.Accounts.CountAsync());
        Assert.Equal(1, await db.IncomeSources.CountAsync());
        Assert.Equal(1, await db.PlannedExpenses.CountAsync());
    }
}
