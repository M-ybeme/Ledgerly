using Ledgerly.Application.Income;
using Ledgerly.Contracts.Income;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("income-sources")]
[Produces("application/json")]
[Authorize]
public class IncomeSourcesController : ControllerBase
{
    private readonly IncomeSourceService _svc;

    public IncomeSourcesController(IncomeSourceService svc)
    {
        _svc = svc;
    }

    // GET /income-sources
    [HttpGet]
    public async Task<ActionResult<List<IncomeSourceDto>>> Get(CancellationToken ct)
        => await _svc.GetAllAsync(ct);

    // POST /income-sources
    [HttpPost]
    public async Task<ActionResult<IncomeSourceDto>> Create([FromBody] CreateIncomeSourceRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _svc.CreateAsync(req, ct);
            return Created($"/income-sources/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // PUT /income-sources/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<IncomeSourceDto>> Update(Guid id, [FromBody] UpdateIncomeSourceRequest req, CancellationToken ct)
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

    // DELETE /income-sources/{id}
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
