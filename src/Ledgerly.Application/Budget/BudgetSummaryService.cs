using Ledgerly.Contracts.Budget;
using Ledgerly.Domain.Budget;

namespace Ledgerly.Application.Budget;

public sealed class BudgetSummaryService
{
    public BudgetSummaryDto Compute(BudgetPlan plan, List<Transaction> transactions)
    {
        // Build a lookup of actual spending by category
        var actualByCategory = transactions
            .GroupBy(t => t.CategoryId)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        // Build category summaries — start from plan lines
        var summaries = new List<CategorySummaryDto>();
        var coveredCategoryIds = new HashSet<Guid>();

        foreach (var line in plan.Lines)
        {
            actualByCategory.TryGetValue(line.CategoryId, out var actual);
            var variance = line.PlannedAmount - actual;
            summaries.Add(new CategorySummaryDto(
                line.CategoryId,
                line.Category.Name,
                line.Category.Type,
                line.PlannedAmount,
                actual,
                variance));
            coveredCategoryIds.Add(line.CategoryId);
        }

        // Add any categories with transactions but no plan line (Planned = 0)
        foreach (var (categoryId, actual) in actualByCategory)
        {
            if (coveredCategoryIds.Contains(categoryId)) continue;

            var category = transactions.First(t => t.CategoryId == categoryId).Category;
            summaries.Add(new CategorySummaryDto(
                categoryId,
                category.Name,
                category.Type,
                Planned: 0m,
                Actual: actual,
                Variance: -actual));
        }

        var totalPlannedIncome = summaries
            .Where(s => s.Type == CategoryType.Income).Sum(s => s.Planned);
        var totalActualIncome = summaries
            .Where(s => s.Type == CategoryType.Income).Sum(s => s.Actual);
        var totalPlannedExpenses = summaries
            .Where(s => s.Type == CategoryType.Expense).Sum(s => s.Planned);
        var totalActualExpenses = summaries
            .Where(s => s.Type == CategoryType.Expense).Sum(s => s.Actual);

        return new BudgetSummaryDto(
            plan.Id,
            plan.Name,
            plan.StartDate,
            plan.EndDate,
            summaries,
            totalPlannedIncome,
            totalActualIncome,
            totalPlannedExpenses,
            totalActualExpenses,
            NetPlanned: totalPlannedIncome - totalPlannedExpenses,
            NetActual: totalActualIncome - totalActualExpenses);
    }
}
