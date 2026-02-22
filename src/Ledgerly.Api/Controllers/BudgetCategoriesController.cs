using Ledgerly.Application.Budget;
using Ledgerly.Contracts.Budget;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("budget-categories")]
[Produces("application/json")]
[Authorize]
public class BudgetCategoriesController : ControllerBase
{
    private readonly BudgetCategoryService _svc;

    public BudgetCategoriesController(BudgetCategoryService svc)
    {
        _svc = svc;
    }

    // GET /budget-categories
    [HttpGet]
    public async Task<ActionResult<List<BudgetCategoryDto>>> Get(CancellationToken ct)
        => await _svc.GetAllAsync(ct);

    // GET /budget-categories/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BudgetCategoryDto>> GetById(Guid id, CancellationToken ct)
    {
        var category = await _svc.GetByIdAsync(id, ct);
        return category is null ? NotFound() : Ok(category);
    }

    // POST /budget-categories
    [HttpPost]
    public async Task<ActionResult<BudgetCategoryDto>> Create([FromBody] CreateBudgetCategoryRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _svc.CreateAsync(req, ct);
            return Created($"/budget-categories/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    // PUT /budget-categories/{id}
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<BudgetCategoryDto>> Update(Guid id, [FromBody] UpdateBudgetCategoryRequest req, CancellationToken ct)
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

    // DELETE /budget-categories/{id}
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
        catch (InvalidOperationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }
}
