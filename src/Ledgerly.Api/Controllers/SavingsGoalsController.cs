using Ledgerly.Application.Budget;
using Ledgerly.Contracts.Budget;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("savings-goals")]
[Produces("application/json")]
[Authorize]
public class SavingsGoalsController : ControllerBase
{
    private readonly SavingsGoalService _svc;

    public SavingsGoalsController(SavingsGoalService svc)
    {
        _svc = svc;
    }

    // GET /savings-goals
    [HttpGet]
    public async Task<ActionResult<List<SavingsGoalDto>>> Get(CancellationToken ct)
        => await _svc.GetAllAsync(ct);

    // POST /savings-goals
    [HttpPost]
    public async Task<ActionResult<SavingsGoalDto>> Create([FromBody] CreateSavingsGoalRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _svc.CreateAsync(req, ct);
            return Created($"/savings-goals/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // PUT /savings-goals/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SavingsGoalDto>> Update(Guid id, [FromBody] UpdateSavingsGoalRequest req, CancellationToken ct)
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

    // DELETE /savings-goals/{id}
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
