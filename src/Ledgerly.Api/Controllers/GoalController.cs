using Ledgerly.Application.Accounts;
using Ledgerly.Application.Budget;
using Ledgerly.Application.Debts;
using Ledgerly.Application.Goals;
using Ledgerly.Application.Income;
using Ledgerly.Contracts.Goals;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("goal")]
[Authorize]
public sealed class GoalController : ControllerBase
{
    private readonly GoalPlannerService _planner;
    private readonly DebtAccountService _debts;
    private readonly AccountService _accounts;
    private readonly IncomeSourceService _income;
    private readonly PlannedExpenseService _expenses;

    public GoalController(
        GoalPlannerService planner,
        DebtAccountService debts,
        AccountService accounts,
        IncomeSourceService income,
        PlannedExpenseService expenses)
    {
        _planner = planner;
        _debts = debts;
        _accounts = accounts;
        _income = income;
        _expenses = expenses;
    }

    // POST /goal/plan
    [HttpPost("plan")]
    public async Task<ActionResult<GoalPlanResultDto>> ComputePlan([FromBody] GoalPlanRequest request)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        var debts = await _debts.GetAllAsync();
        var income = await _income.GetAllAsync();
        var allExpenses = await _expenses.GetAllAsync();

        var thisMonthExpenses = allExpenses
            .Where(e => e.DueDate.Year == today.Year && e.DueDate.Month == today.Month)
            .ToList();

        return Ok(_planner.Compute(request, debts, income, thisMonthExpenses, today));
    }
}
