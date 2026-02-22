using Ledgerly.Application.Accounts;
using Ledgerly.Contracts.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("accounts")]
[Produces("application/json")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly AccountService _svc;

    public AccountsController(AccountService svc)
    {
        _svc = svc;
    }

    // GET /accounts
    [HttpGet]
    public async Task<ActionResult<List<AccountDto>>> Get(CancellationToken ct)
        => await _svc.GetAllAsync(ct);

    // GET /accounts/{id}
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountDto>> GetById(Guid id, CancellationToken ct)
    {
        var acct = await _svc.GetByIdAsync(id, ct);
        return acct is null ? NotFound() : Ok(acct);
    }

    // POST /accounts
    [HttpPost]
    public async Task<ActionResult<AccountDto>> Create([FromBody] CreateAccountRequest req, CancellationToken ct)
    {
        try
        {
            var created = await _svc.CreateAsync(req, ct);
            return Created($"/accounts/{created.Id}", created);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}