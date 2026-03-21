using Ledgerly.Contracts.Accounts;
using Ledgerly.Contracts.Budget;
using Ledgerly.Contracts.Debts;
using Ledgerly.Contracts.Income;
using Ledgerly.Contracts.Insights;
using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Insights;

public sealed class InsightService
{
    public InsightsDto ComputeAll(
        List<DebtAccountDto> debts,
        List<IncomeSourceDto> incomeSources,
        List<PlannedExpenseDto> thisMonthExpenses,
        List<AccountDto> accounts,
        DateOnly today)
    {
        return new InsightsDto(
            ComputeDebtInsights(debts, incomeSources),
            ComputeBudgetInsights(thisMonthExpenses, today),
            ComputeCashFlowInsights(accounts, thisMonthExpenses, today));
    }

    // ── Debt insights ──────────────────────────────────────────────────────

    private static List<InsightDto> ComputeDebtInsights(
        List<DebtAccountDto> debts,
        List<IncomeSourceDto> incomeSources)
    {
        var insights = new List<InsightDto>();

        if (debts.Count == 0)
        {
            insights.Add(new("Add your debts to start modeling a payoff strategy.", InsightSeverity.Info));
            return insights;
        }

        // Highest-APR debt
        var highestApr = debts.MaxBy(d => d.AnnualInterestRate);
        if (highestApr is not null && highestApr.AnnualInterestRate > 0)
        {
            var aprPct = highestApr.AnnualInterestRate * 100m;
            var severity = aprPct >= 24m ? InsightSeverity.Danger
                         : aprPct >= 18m ? InsightSeverity.Warning
                         : InsightSeverity.Info;
            insights.Add(new(
                $"{highestApr.Name} has the highest interest rate at {aprPct:F1}% APR — the avalanche strategy targets this first.",
                severity));
        }

        // Monthly minimums vs income
        var totalMinimums = debts.Sum(d => d.MinimumPayment);
        var monthlyIncome = incomeSources.Sum(s => s.MonthlyAmount);
        if (totalMinimums > 0 && monthlyIncome > 0)
        {
            var ratio = totalMinimums / monthlyIncome * 100m;
            var severity = ratio >= 25m ? InsightSeverity.Danger
                         : ratio >= 15m ? InsightSeverity.Warning
                         : InsightSeverity.Info;
            insights.Add(new(
                $"Monthly debt minimums (${totalMinimums:N0}/mo) are {ratio:F1}% of expected income — " +
                (ratio >= 25m ? "this is a high debt burden."
                 : ratio >= 15m ? "approaching a high debt burden."
                 : "within a manageable range."),
                severity));
        }

        return insights;
    }

    // ── Budget insights ────────────────────────────────────────────────────

    private static List<InsightDto> ComputeBudgetInsights(
        List<PlannedExpenseDto> thisMonthExpenses,
        DateOnly today)
    {
        var insights = new List<InsightDto>();

        if (thisMonthExpenses.Count == 0)
            return insights;

        // Overdue bills
        var overdue = thisMonthExpenses.Where(e => !e.IsPaid && e.DueDate < today).ToList();
        if (overdue.Count > 0)
        {
            var total = overdue.Sum(e => e.PlannedAmount);
            insights.Add(new(
                $"{overdue.Count} bill{(overdue.Count == 1 ? "" : "s")} overdue totaling ${total:N0} — action needed.",
                InsightSeverity.Danger));
        }

        // Unpaid must-pay bills remaining this month
        var unpaidMust = thisMonthExpenses
            .Where(e => !e.IsPaid && e.Priority == ExpensePriority.MustPay && e.DueDate >= today)
            .ToList();
        if (unpaidMust.Count > 0)
        {
            var total = unpaidMust.Sum(e => e.PlannedAmount);
            insights.Add(new(
                $"${total:N0} in must-pay bills still due this month ({unpaidMust.Count} remaining).",
                InsightSeverity.Warning));
        }

        // All paid — good news
        var allPaid = thisMonthExpenses.All(e => e.IsPaid);
        if (allPaid)
        {
            insights.Add(new(
                "All bills for this month are paid. Great job!",
                InsightSeverity.Info));
        }

        return insights;
    }

    // ── Cash flow insights ──────────────────────────────────────────────────

    private static List<InsightDto> ComputeCashFlowInsights(
        List<AccountDto> accounts,
        List<PlannedExpenseDto> thisMonthExpenses,
        DateOnly today)
    {
        var insights = new List<InsightDto>();

        var cash = accounts
            .Where(a => a.Type != Domain.Accounts.AccountType.CreditCard)
            .Sum(a => a.Balance);

        var unpaidBills = thisMonthExpenses
            .Where(e => !e.IsPaid && e.DueDate >= today)
            .Sum(e => e.PlannedAmount);

        var totalMonthlyBills = thisMonthExpenses.Sum(e => e.PlannedAmount);

        if (accounts.Count == 0)
        {
            insights.Add(new("Add your bank accounts to see your cash position.", InsightSeverity.Info));
            return insights;
        }

        // Cash vs unpaid bills
        if (unpaidBills > 0 && cash < unpaidBills)
        {
            var shortfall = unpaidBills - cash;
            insights.Add(new(
                $"Unpaid bills (${unpaidBills:N0}) exceed available cash (${cash:N0}) — you may be short ${shortfall:N0}.",
                InsightSeverity.Danger));
        }
        else if (cash < 500m)
        {
            insights.Add(new(
                $"Cash is low (${cash:N0}) — consider building an emergency buffer before your next bills.",
                InsightSeverity.Warning));
        }
        else if (totalMonthlyBills > 0)
        {
            var monthsCovered = cash / totalMonthlyBills;
            insights.Add(new(
                $"Cash (${cash:N0}) covers approximately {monthsCovered:F1} month{(monthsCovered < 1.95m ? "" : "s")} of bills — healthy buffer.",
                InsightSeverity.Info));
        }
        else
        {
            insights.Add(new(
                $"Available cash: ${cash:N0} across {accounts.Count} account{(accounts.Count == 1 ? "" : "s")}.",
                InsightSeverity.Info));
        }

        return insights;
    }
}
