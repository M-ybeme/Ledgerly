using Ledgerly.Contracts.Accounts;
using Ledgerly.Contracts.Budget;
using Ledgerly.Contracts.Debts;
using Ledgerly.Contracts.Goals;
using Ledgerly.Contracts.Income;

namespace Ledgerly.Application.Goals;

public sealed class GoalPlannerService
{
    public GoalPlanResultDto Compute(
        GoalPlanRequest request,
        List<DebtAccountDto> debts,
        List<IncomeSourceDto> incomeSources,
        List<PlannedExpenseDto> thisMonthExpenses,
        DateOnly today)
    {
        var monthlyIncome = incomeSources.Sum(s => s.MonthlyAmount);
        var monthlyExpenses = thisMonthExpenses.Sum(e => e.PlannedAmount);
        var totalMinimums = debts.Sum(d => d.MinimumPayment);

        return request.GoalType switch
        {
            GoalType.DebtFree   => ComputeDebtFree(request, debts, totalMinimums, monthlyIncome, monthlyExpenses, today),
            GoalType.SaveAmount => ComputeSaveAmount(request, totalMinimums, monthlyIncome, monthlyExpenses),
            GoalType.SpendingCap => ComputeSpendingCap(request, monthlyExpenses),
            _ => throw new ArgumentException("Unknown goal type.")
        };
    }

    // ── Debt-Free by Date ───────────────────────────────────────────────────

    private static GoalPlanResultDto ComputeDebtFree(
        GoalPlanRequest request,
        List<DebtAccountDto> debts,
        decimal totalMinimums,
        decimal monthlyIncome,
        decimal monthlyExpenses,
        DateOnly today)
    {
        var totalDebt = debts.Sum(d => d.Balance);

        if (totalDebt <= 0)
            return new(GoalFeasibility.OnTrack,
                "You have no outstanding debt — this goal is already achieved!",
                null, null, 0m, null, 0m);

        if (request.TargetDate is not { } targetDate)
            return new(GoalFeasibility.NotFeasible, "Target date is required.", null, null, null, null, null);

        var targetMonths = ((targetDate.Year - today.Year) * 12) + (targetDate.Month - today.Month);
        if (targetMonths <= 0)
            return new(GoalFeasibility.NotFeasible,
                "Target date must be in the future.",
                $"Total debt remaining: ${totalDebt:N0}.", null, null, null, null);

        // Weighted average monthly interest rate
        var weightedMonthlyRate = totalDebt > 0
            ? debts.Sum(d => d.Balance * d.AnnualInterestRate / 12m) / totalDebt
            : 0m;

        // PMT formula: P * r(1+r)^n / ((1+r)^n - 1)
        decimal requiredPmt;
        if (weightedMonthlyRate <= 0)
        {
            requiredPmt = totalDebt / targetMonths;
        }
        else
        {
            double r = (double)weightedMonthlyRate;
            double n = targetMonths;
            double factor = Math.Pow(1 + r, n);
            requiredPmt = (decimal)((double)totalDebt * r * factor / (factor - 1));
        }

        var requiredExtra = Math.Max(0m, requiredPmt - totalMinimums);
        var netAfterExpenses = monthlyIncome - monthlyExpenses;
        var availableForExtra = Math.Max(0m, netAfterExpenses - totalMinimums);
        var shortfall = Math.Max(0m, requiredExtra - availableForExtra);

        var feasibility = requiredPmt <= totalMinimums || shortfall == 0m ? GoalFeasibility.OnTrack
                        : shortfall < requiredPmt * 0.25m ? GoalFeasibility.AtRisk
                        : GoalFeasibility.NotFeasible;

        var summary = $"To be debt-free by {targetDate:MMM yyyy}, you need ${requiredPmt:N0}/month in total debt payments.";

        var statusDetail = requiredExtra > 0
            ? $"Your current minimums are ${totalMinimums:N0}/month — you need ${requiredExtra:N0}/month extra on top of that."
            : $"Your current minimums (${totalMinimums:N0}/month) are already sufficient for this timeline.";

        var recommendation = feasibility != GoalFeasibility.OnTrack
            ? $"Free up ${shortfall:N0}/month by reducing monthly spending or increasing income."
            : null;

        return new(feasibility, summary, statusDetail, recommendation,
            Math.Round(requiredPmt, 0), Math.Round(availableForExtra, 0), Math.Round(shortfall, 0));
    }

    // ── Save a Target Amount ────────────────────────────────────────────────

    private static GoalPlanResultDto ComputeSaveAmount(
        GoalPlanRequest request,
        decimal totalMinimums,
        decimal monthlyIncome,
        decimal monthlyExpenses)
    {
        if (request.TargetSavingsAmount is not { } targetAmount || targetAmount <= 0)
            return new(GoalFeasibility.NotFeasible, "Target savings amount is required.", null, null, null, null, null);

        if (request.MonthsToSave is not { } months || months <= 0)
            return new(GoalFeasibility.NotFeasible, "Number of months is required.", null, null, null, null, null);

        var requiredMonthly = targetAmount / months;
        // Capacity = income − expenses − debt minimums (what's truly left to save)
        var currentCapacity = Math.Max(0m, monthlyIncome - monthlyExpenses - totalMinimums);
        var shortfall = Math.Max(0m, requiredMonthly - currentCapacity);

        var feasibility = shortfall == 0m ? GoalFeasibility.OnTrack
                        : shortfall < requiredMonthly * 0.3m ? GoalFeasibility.AtRisk
                        : GoalFeasibility.NotFeasible;

        var summary = $"Saving ${targetAmount:N0} in {months} month{(months == 1 ? "" : "s")} requires ${requiredMonthly:N0}/month.";

        var statusDetail = $"Estimated monthly surplus after all expenses and debt minimums: ${currentCapacity:N0}.";

        var recommendation = shortfall > 0
            ? $"Reduce spending or increase income by ${shortfall:N0}/month to hit this goal."
            : null;

        return new(feasibility, summary, statusDetail, recommendation,
            Math.Round(requiredMonthly, 0), Math.Round(currentCapacity, 0), Math.Round(shortfall, 0));
    }

    // ── Spending Cap ────────────────────────────────────────────────────────

    private static GoalPlanResultDto ComputeSpendingCap(
        GoalPlanRequest request,
        decimal monthlyExpenses)
    {
        if (request.MonthlySpendingCap is not { } cap || cap <= 0)
            return new(GoalFeasibility.NotFeasible, "Monthly spending cap is required.", null, null, null, null, null);

        var overage = monthlyExpenses - cap;
        var shortfall = Math.Max(0m, overage);

        var feasibility = overage <= 0m ? GoalFeasibility.OnTrack
                        : overage <= cap * 0.1m ? GoalFeasibility.AtRisk
                        : GoalFeasibility.NotFeasible;

        var summary = overage <= 0
            ? $"Your planned spending (${monthlyExpenses:N0}/month) is within the ${cap:N0} cap — surplus of ${-overage:N0}."
            : $"Your planned spending (${monthlyExpenses:N0}/month) exceeds the ${cap:N0} cap by ${overage:N0}.";

        var statusDetail = $"Based on {(overage <= 0 ? "current" : "this month's")} planned expenses: ${monthlyExpenses:N0}/month.";

        var recommendation = overage > 0
            ? $"Reduce planned expenses by ${overage:N0} to meet your spending target."
            : null;

        return new(feasibility, summary, statusDetail, recommendation,
            Math.Round(cap, 0), Math.Round(monthlyExpenses, 0), Math.Round(shortfall, 0));
    }
}
