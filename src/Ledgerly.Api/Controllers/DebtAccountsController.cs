using Ledgerly.Application.Debts;
using Ledgerly.Contracts.Debts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("debt-accounts")]
[Produces("application/json")]
[Authorize]
public class DebtAccountsController : ControllerBase
{
    private readonly DebtAccountService _svc;

    public DebtAccountsController(DebtAccountService svc)
    {
        _svc = svc;
    }

    // GET /debt-accounts
    [HttpGet]
    public async Task<ActionResult<List<DebtAccountDto>>> Get(CancellationToken ct)
        => await _svc.GetAllAsync(ct);

    // GET /debt-accounts/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DebtAccountDto>> GetById(Guid id, CancellationToken ct)
    {
        var debt = await _svc.GetByIdAsync(id, ct);
        return debt is null ? NotFound() : Ok(debt);
    }

    // POST /debt-accounts
    [HttpPost]
    public async Task<ActionResult<DebtAccountDto>> Create([FromBody] CreateDebtAccountRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _svc.CreateAsync(req, ct);
            return Created($"/debt-accounts/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // PUT /debt-accounts/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<DebtAccountDto>> Update(Guid id, [FromBody] UpdateDebtAccountRequest req, CancellationToken ct)
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

    // DELETE /debt-accounts/{id}
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
}
