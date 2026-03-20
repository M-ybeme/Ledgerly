using Ledgerly.Application.Dashboard;
using Ledgerly.Application.Debts;
using Ledgerly.Contracts.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("dashboard")]
[Authorize]
public sealed class DashboardController : ControllerBase
{
    private readonly FinancialSummaryService _summary;
    private readonly DebtAccountService _debts;

    public DashboardController(FinancialSummaryService summary, DebtAccountService debts)
    {
        _summary = summary;
        _debts = debts;
    }

    // GET /dashboard/financial-summary
    [HttpGet("financial-summary")]
    public async Task<ActionResult<FinancialSummaryDto>> GetFinancialSummary()
    {
        var debts = await _debts.GetAllAsync();
        var totalDebt = debts.Sum(d => d.Balance);

        var (months, cumulative) = await _summary.GetMonthlySavingsAsync();

        return Ok(new FinancialSummaryDto(totalDebt, cumulative, months));
    }
}
