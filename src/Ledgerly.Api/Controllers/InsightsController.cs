using Ledgerly.Application.Accounts;
using Ledgerly.Application.Budget;
using Ledgerly.Application.Debts;
using Ledgerly.Application.Income;
using Ledgerly.Application.Insights;
using Ledgerly.Contracts.Insights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("insights")]
[Authorize]
public sealed class InsightsController : ControllerBase
{
    private readonly InsightService _insights;
    private readonly DebtAccountService _debts;
    private readonly AccountService _accounts;
    private readonly IncomeSourceService _income;
    private readonly PlannedExpenseService _expenses;

    public InsightsController(
        InsightService insights,
        DebtAccountService debts,
        AccountService accounts,
        IncomeSourceService income,
        PlannedExpenseService expenses)
    {
        _insights = insights;
        _debts = debts;
        _accounts = accounts;
        _income = income;
        _expenses = expenses;
    }

    // GET /insights
    [HttpGet]
    public async Task<ActionResult<InsightsDto>> GetInsights()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var debts = await _debts.GetAllAsync();
        var accounts = await _accounts.GetAllAsync();
        var income = await _income.GetAllAsync();
        var allExpenses = await _expenses.GetAllAsync();

        // Filter to current-month expenses (due this month or recurring unpaid)
        var thisMonthExpenses = allExpenses
            .Where(e => (e.DueDate.Year == today.Year && e.DueDate.Month == today.Month)
                     || (e.IsRecurring && !e.IsPaid))
            .ToList();

        return Ok(_insights.ComputeAll(debts, income, thisMonthExpenses, accounts, today));
    }
}
