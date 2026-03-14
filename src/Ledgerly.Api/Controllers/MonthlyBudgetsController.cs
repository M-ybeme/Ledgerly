using Ledgerly.Application.Budget;
using Ledgerly.Contracts.Budget;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("monthly-budgets")]
[Produces("application/json")]
[Authorize]
public class MonthlyBudgetsController : ControllerBase
{
    private readonly MonthlyBudgetService _svc;

    public MonthlyBudgetsController(MonthlyBudgetService svc)
    {
        _svc = svc;
    }

    // GET /monthly-budgets?year=2025&month=3
    [HttpGet]
    public async Task<ActionResult<List<MonthlyBudgetDto>>> Get([FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        if (year < 2000 || month < 1 || month > 12)
            return Problem(detail: "Invalid year or month.", statusCode: StatusCodes.Status400BadRequest);

        return await _svc.GetForMonthAsync(new DateOnly(year, month, 1), ct);
    }

    // PUT /monthly-budgets?year=2025&month=3
    [HttpPut]
    public async Task<ActionResult<MonthlyBudgetDto>> Set([FromQuery] int year, [FromQuery] int month, [FromBody] SetMonthlyBudgetRequest req, CancellationToken ct)
    {
        if (year < 2000 || month < 1 || month > 12)
            return Problem(detail: "Invalid year or month.", statusCode: StatusCodes.Status400BadRequest);

        try
        {
            var result = await _svc.SetAsync(new DateOnly(year, month, 1), req, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // DELETE /monthly-budgets?year=2025&month=3&categoryId={guid}
    [HttpDelete]
    public async Task<IActionResult> Delete([FromQuery] int year, [FromQuery] int month, [FromQuery] Guid categoryId, CancellationToken ct)
    {
        if (year < 2000 || month < 1 || month > 12)
            return Problem(detail: "Invalid year or month.", statusCode: StatusCodes.Status400BadRequest);

        try
        {
            await _svc.DeleteAsync(new DateOnly(year, month, 1), categoryId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }
}
