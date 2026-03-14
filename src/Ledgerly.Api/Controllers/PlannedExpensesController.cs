using Ledgerly.Application.Budget;
using Ledgerly.Contracts.Budget;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("planned-expenses")]
[Produces("application/json")]
[Authorize]
public class PlannedExpensesController : ControllerBase
{
    private readonly PlannedExpenseService _svc;

    public PlannedExpensesController(PlannedExpenseService svc)
    {
        _svc = svc;
    }

    // GET /planned-expenses
    [HttpGet]
    public async Task<ActionResult<List<PlannedExpenseDto>>> Get(CancellationToken ct)
        => await _svc.GetAllAsync(ct);

    // POST /planned-expenses
    [HttpPost]
    public async Task<ActionResult<PlannedExpenseDto>> Create([FromBody] CreatePlannedExpenseRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _svc.CreateAsync(req, ct);
            return Created($"/planned-expenses/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // PUT /planned-expenses/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PlannedExpenseDto>> Update(Guid id, [FromBody] UpdatePlannedExpenseRequest req, CancellationToken ct)
    {
        try
        {
            var updated = await _svc.UpdateAsync(id, req, ct);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // POST /planned-expenses/{id}/mark-paid
    [HttpPost("{id:guid}/mark-paid")]
    public async Task<ActionResult<PlannedExpenseDto>> MarkPaid(Guid id, [FromBody] MarkPaidRequest req, CancellationToken ct)
    {
        try
        {
            var updated = await _svc.MarkPaidAsync(id, req, ct);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    // POST /planned-expenses/{id}/mark-unpaid
    [HttpPost("{id:guid}/mark-unpaid")]
    public async Task<IActionResult> MarkUnpaid(Guid id, CancellationToken ct)
    {
        try
        {
            await _svc.MarkUnpaidAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    // DELETE /planned-expenses/{id}
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _svc.DeleteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }
}
