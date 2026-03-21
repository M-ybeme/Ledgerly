using Ledgerly.Application.CashFlow;
using Ledgerly.Contracts.CashFlow;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("cash-flow")]
[Produces("application/json")]
[Authorize]
public class CashFlowController : ControllerBase
{
    private readonly CashFlowForecastService _svc;

    public CashFlowController(CashFlowForecastService svc)
    {
        _svc = svc;
    }

    // GET /cash-flow/forecast?startingBalance=5000&days=30&dailyBurnRate=50
    [HttpGet("forecast")]
    public async Task<ActionResult<CashFlowForecastDto>> GetForecast(
        [FromQuery] decimal startingBalance,
        [FromQuery] int days = 30,
        [FromQuery] decimal dailyBurnRate = 0m,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _svc.ComputeAsync(startingBalance, days, dailyBurnRate, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
