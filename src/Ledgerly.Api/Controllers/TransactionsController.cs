using Ledgerly.Application.Budget;
using Ledgerly.Contracts.Budget;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("transactions")]
[Produces("application/json")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly TransactionService _svc;

    public TransactionsController(TransactionService svc)
    {
        _svc = svc;
    }

    // GET /transactions?from=yyyy-MM-dd&to=yyyy-MM-dd
    [HttpGet]
    public async Task<ActionResult<List<TransactionDto>>> Get(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
        => await _svc.GetAllAsync(from, to, ct);

    // GET /transactions/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> GetById(Guid id, CancellationToken ct)
    {
        var transaction = await _svc.GetByIdAsync(id, ct);
        return transaction is null ? NotFound() : Ok(transaction);
    }

    // POST /transactions
    [HttpPost]
    public async Task<ActionResult<TransactionDto>> Create([FromBody] CreateTransactionRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _svc.CreateAsync(req, ct);
            return Created($"/transactions/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // PUT /transactions/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TransactionDto>> Update(Guid id, [FromBody] UpdateTransactionRequest req, CancellationToken ct)
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

    // DELETE /transactions/{id}
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
