using Ledgerly.Application.Credit;
using Ledgerly.Application.Scenarios;
using Ledgerly.Contracts.Credit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("scenarios/{scenarioId:guid}/credit")]
[Authorize]
public sealed class CreditProfilesController : ControllerBase
{
    private readonly ScenarioService _scenarioSvc;
    private readonly CreditProfileService _creditSvc;
    private readonly DebtProjectionService _projectionSvc;
    private readonly CreditScoreService _creditScoreSvc;

    public CreditProfilesController(
        ScenarioService scenarioSvc,
        CreditProfileService creditSvc,
        DebtProjectionService projectionSvc,
        CreditScoreService creditScoreSvc)
    {
        _scenarioSvc = scenarioSvc;
        _creditSvc = creditSvc;
        _projectionSvc = projectionSvc;
        _creditScoreSvc = creditScoreSvc;
    }

    [HttpGet]
    public async Task<IActionResult> Get(Guid scenarioId, CancellationToken ct)
    {
        var dto = await _creditSvc.GetByScenarioAsync(scenarioId, ct);
        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPut]
    public async Task<IActionResult> Upsert(
        Guid scenarioId,
        [FromBody] CreateCreditProfileRequest req,
        CancellationToken ct)
    {
        try
        {
            var dto = await _creditSvc.UpsertAsync(scenarioId, req, ct);
            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return Problem(ex.Message, statusCode: 400);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: 404);
        }
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(Guid scenarioId, CancellationToken ct)
    {
        try
        {
            await _creditSvc.DeleteAsync(scenarioId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(ex.Message, statusCode: 404);
        }
    }

    [HttpGet("projection")]
    public async Task<IActionResult> GetProjection(Guid scenarioId, CancellationToken ct)
    {
        var scenario = await _scenarioSvc.GetScenarioEntityAsync(scenarioId, ct);
        if (scenario is null) return NotFound();

        var profile = await _creditSvc.GetEntityByScenarioAsync(scenarioId, ct);
        if (profile is null) return NotFound();

        var projection = _projectionSvc.Project(scenario);
        var result = _creditScoreSvc.Project(profile, scenario.DebtAccounts, projection);
        return Ok(result);
    }
}
