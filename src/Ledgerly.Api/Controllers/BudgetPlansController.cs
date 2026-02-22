using Ledgerly.Application.Budget;
using Ledgerly.Contracts.Budget;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("budget-plans")]
[Produces("application/json")]
[Authorize]
public class BudgetPlansController : ControllerBase
{
    private readonly BudgetPlanService _svc;
    private readonly TransactionService _transactionSvc;
    private readonly BudgetSummaryService _summarySvc;

    public BudgetPlansController(
        BudgetPlanService svc,
        TransactionService transactionSvc,
        BudgetSummaryService summarySvc)
    {
        _svc = svc;
        _transactionSvc = transactionSvc;
        _summarySvc = summarySvc;
    }

    // GET /budget-plans
    [HttpGet]
    public async Task<ActionResult<List<BudgetPlanDto>>> Get(CancellationToken ct)
        => await _svc.GetAllAsync(ct);

    // GET /budget-plans/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BudgetPlanDto>> GetById(Guid id, CancellationToken ct)
    {
        var plan = await _svc.GetByIdAsync(id, ct);
        return plan is null ? NotFound() : Ok(plan);
    }

    // POST /budget-plans
    [HttpPost]
    public async Task<ActionResult<BudgetPlanDto>> Create([FromBody] CreateBudgetPlanRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _svc.CreateAsync(req, ct);
            return Created($"/budget-plans/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // PUT /budget-plans/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BudgetPlanDto>> Update(Guid id, [FromBody] UpdateBudgetPlanRequest req, CancellationToken ct)
    {
        try
        {
            var updated = await _svc.UpdateAsync(id, req, ct);
            return Ok(updated);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // DELETE /budget-plans/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // GET /budget-plans/{id}/summary
    [HttpGet("{id:guid}/summary")]
    public async Task<ActionResult<BudgetSummaryDto>> GetSummary(Guid id, CancellationToken ct)
    {
        var plan = await _svc.GetPlanEntityAsync(id, ct);
        if (plan is null) return NotFound();

        var transactions = await _transactionSvc.GetEntitiesByDateRangeAsync(plan.StartDate, plan.EndDate, ct);
        var summary = _summarySvc.Compute(plan, transactions);

        return Ok(summary);
    }
}
