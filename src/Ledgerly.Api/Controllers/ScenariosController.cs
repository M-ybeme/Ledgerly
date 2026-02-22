using Ledgerly.Application.Scenarios;
using Ledgerly.Contracts.Scenarios;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("scenarios")]
[Produces("application/json")]
[Authorize]
public class ScenariosController : ControllerBase
{
    private readonly ScenarioService _svc;
    private readonly DebtProjectionService _projection;
    private readonly ScenarioComparisonService _comparison;
    private readonly ActualPaymentService _payments;
    private readonly DriftService _drift;

    public ScenariosController(
        ScenarioService svc,
        DebtProjectionService projection,
        ScenarioComparisonService comparison,
        ActualPaymentService payments,
        DriftService drift)
    {
        _svc = svc;
        _projection = projection;
        _comparison = comparison;
        _payments = payments;
        _drift = drift;
    }

    // GET /scenarios
    [HttpGet]
    public async Task<ActionResult<List<ScenarioDto>>> Get(CancellationToken ct)
        => await _svc.GetAllAsync(ct);

    // GET /scenarios/compare?a={idA}&b={idB}
    [HttpGet("compare")]
    public async Task<ActionResult<ScenarioComparisonDto>> Compare(
        [FromQuery] Guid a,
        [FromQuery] Guid b,
        CancellationToken ct)
    {
        var scenarioA = await _svc.GetScenarioEntityAsync(a, ct);
        if (scenarioA is null) return NotFound(Problem(detail: $"Scenario {a} not found.", statusCode: 404));

        var scenarioB = await _svc.GetScenarioEntityAsync(b, ct);
        if (scenarioB is null) return NotFound(Problem(detail: $"Scenario {b} not found.", statusCode: 404));

        return Ok(_comparison.Compare(scenarioA, scenarioB));
    }

    // GET /scenarios/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ScenarioDto>> GetById(Guid id, CancellationToken ct)
    {
        var scenario = await _svc.GetByIdAsync(id, ct);
        return scenario is null ? NotFound() : Ok(scenario);
    }

    // POST /scenarios
    [HttpPost]
    public async Task<ActionResult<ScenarioDto>> Create([FromBody] CreateScenarioRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _svc.CreateAsync(req, ct);
            return Created($"/scenarios/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // POST /scenarios/{id}/duplicate
    [HttpPost("{id:guid}/duplicate")]
    public async Task<ActionResult<ScenarioDto>> Duplicate(Guid id, CancellationToken ct)
    {
        try
        {
            var created = await _svc.DuplicateAsync(id, ct);
            return Created($"/scenarios/{created.Id}", created);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // GET /scenarios/{id}/projection
    [HttpGet("{id:guid}/projection")]
    public async Task<ActionResult<ProjectionResultDto>> GetProjection(Guid id, CancellationToken ct)
    {
        var scenario = await _svc.GetScenarioEntityAsync(id, ct);
        if (scenario is null) return NotFound();

        var result = _projection.Project(scenario);
        return Ok(result);
    }

    // GET /scenarios/{id}/payments
    [HttpGet("{id:guid}/payments")]
    public async Task<ActionResult<List<ActualPaymentDto>>> GetPayments(Guid id, CancellationToken ct)
        => await _payments.GetByScenarioAsync(id, ct);

    // POST /scenarios/{id}/payments
    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<ActualPaymentDto>> LogPayment(
        Guid id,
        [FromBody] LogPaymentRequest req,
        CancellationToken ct)
    {
        try
        {
            var created = await _payments.LogPaymentAsync(id, req, ct);
            return Created($"/scenarios/{id}/payments/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    // DELETE /scenarios/{id}/payments/{paymentId}
    [HttpDelete("{id:guid}/payments/{paymentId:guid}")]
    public async Task<IActionResult> DeletePayment(Guid id, Guid paymentId, CancellationToken ct)
    {
        try
        {
            await _payments.DeleteAsync(id, paymentId, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // GET /scenarios/{id}/drift
    [HttpGet("{id:guid}/drift")]
    public async Task<ActionResult<DriftSummaryDto>> GetDrift(Guid id, CancellationToken ct)
    {
        var scenario = await _svc.GetScenarioEntityAsync(id, ct);
        if (scenario is null) return NotFound();

        var actualPayments = await _payments.GetEntitiesByScenarioAsync(id, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var result = _drift.Compute(scenario, actualPayments, today);
        return Ok(result);
    }
}
