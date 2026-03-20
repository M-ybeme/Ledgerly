using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Ledgerly.Application.Accounts;
using Ledgerly.Application.Budget;
using Ledgerly.Application.Debts;
using Ledgerly.Application.Income;
using Ledgerly.Application.Scenarios;
using Ledgerly.Contracts.Export;
using Ledgerly.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ledgerly.Api.Controllers;

[ApiController]
[Route("export")]
[Authorize]
public sealed class ExportController : ControllerBase
{
    private readonly AccountService _accounts;
    private readonly DebtAccountService _debts;
    private readonly IncomeSourceService _income;
    private readonly PlannedExpenseService _expenses;
    private readonly BudgetCategoryService _categories;
    private readonly TransactionService _transactions;
    private readonly ScenarioService _scenarios;
    private readonly UserManager<ApplicationUser> _userManager;

    public ExportController(
        AccountService accounts,
        DebtAccountService debts,
        IncomeSourceService income,
        PlannedExpenseService expenses,
        BudgetCategoryService categories,
        TransactionService transactions,
        ScenarioService scenarios,
        UserManager<ApplicationUser> userManager)
    {
        _accounts = accounts;
        _debts = debts;
        _income = income;
        _expenses = expenses;
        _categories = categories;
        _transactions = transactions;
        _scenarios = scenarios;
        _userManager = userManager;
    }

    // GET /export/json
    [HttpGet("json")]
    public async Task<IActionResult> ExportJson()
    {
        var dto = await BuildExportDtoAsync();
        var json = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });
        var bytes = Encoding.UTF8.GetBytes(json);
        return File(bytes, "application/json", $"ledgerly-export-{DateTime.UtcNow:yyyyMMdd}.json");
    }

    // GET /export/csv
    [HttpGet("csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var dto = await BuildExportDtoAsync();

        using var ms = new MemoryStream();
        using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "accounts.csv", AccountsCsv(dto));
            WriteEntry(archive, "debts.csv", DebtsCsv(dto));
            WriteEntry(archive, "income_sources.csv", IncomeCsv(dto));
            WriteEntry(archive, "planned_expenses.csv", ExpensesCsv(dto));
            WriteEntry(archive, "budget_categories.csv", CategoriesCsv(dto));
            WriteEntry(archive, "transactions.csv", TransactionsCsv(dto));
            WriteEntry(archive, "scenarios.csv", ScenariosCsv(dto));
        }

        return File(ms.ToArray(), "application/zip", $"ledgerly-export-{DateTime.UtcNow:yyyyMMdd}.zip");
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private async Task<ExportDto> BuildExportDtoAsync()
    {
        var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var user = userId is not null ? await _userManager.FindByIdAsync(userId) : null;

        var accountsTask = _accounts.GetAllAsync();
        var debtsTask = _debts.GetAllAsync();
        var incomeTask = _income.GetAllAsync();
        var expensesTask = _expenses.GetAllAsync();
        var categoriesTask = _categories.GetAllAsync();
        var transactionsTask = _transactions.GetAllAsync();
        var scenariosTask = _scenarios.GetAllAsync();

        await Task.WhenAll(accountsTask, debtsTask, incomeTask, expensesTask, categoriesTask, transactionsTask, scenariosTask);

        return new ExportDto(
            ExportedAt: DateTime.UtcNow,
            Email: user?.Email ?? "",
            Accounts: accountsTask.Result,
            Debts: debtsTask.Result,
            IncomeSources: incomeTask.Result,
            PlannedExpenses: expensesTask.Result,
            BudgetCategories: categoriesTask.Result,
            Transactions: transactionsTask.Result,
            Scenarios: scenariosTask.Result);
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private static string AccountsCsv(ExportDto dto)
    {
        var sb = new StringBuilder("\"Name\",\"Type\",\"CreatedUtc\"\n");
        foreach (var a in dto.Accounts)
            sb.AppendLine($"{Csv(a.Name)},{Csv(a.Type.ToString())},{Csv(a.CreatedUtc.ToString("O"))}");
        return sb.ToString();
    }

    private static string DebtsCsv(ExportDto dto)
    {
        var sb = new StringBuilder("\"Name\",\"Balance\",\"AnnualInterestRate\",\"MinimumPayment\"\n");
        foreach (var d in dto.Debts)
            sb.AppendLine($"{Csv(d.Name)},{d.Balance},{d.AnnualInterestRate},{d.MinimumPayment}");
        return sb.ToString();
    }

    private static string IncomeCsv(ExportDto dto)
    {
        var sb = new StringBuilder("\"Name\",\"Amount\",\"Frequency\",\"MonthlyAmount\"\n");
        foreach (var s in dto.IncomeSources)
            sb.AppendLine($"{Csv(s.Name)},{s.Amount},{Csv(s.Frequency.ToString())},{s.MonthlyAmount}");
        return sb.ToString();
    }

    private static string ExpensesCsv(ExportDto dto)
    {
        var sb = new StringBuilder("\"Description\",\"PlannedAmount\",\"DueDate\",\"Priority\",\"IsPaid\",\"ActualAmount\",\"PaidDate\"\n");
        foreach (var e in dto.PlannedExpenses)
            sb.AppendLine($"{Csv(e.Description)},{e.PlannedAmount},{e.DueDate},{Csv(e.Priority.ToString())},{e.IsPaid},{e.ActualAmount?.ToString() ?? ""},{e.PaidDate?.ToString() ?? ""}");
        return sb.ToString();
    }

    private static string CategoriesCsv(ExportDto dto)
    {
        var sb = new StringBuilder("\"Name\",\"Type\"\n");
        foreach (var c in dto.BudgetCategories)
            sb.AppendLine($"{Csv(c.Name)},{Csv(c.Type.ToString())}");
        return sb.ToString();
    }

    private static string TransactionsCsv(ExportDto dto)
    {
        var sb = new StringBuilder("\"Description\",\"Amount\",\"Date\",\"Category\",\"Type\"\n");
        foreach (var t in dto.Transactions)
            sb.AppendLine($"{Csv(t.Description)},{t.Amount},{t.Date},{Csv(t.CategoryName)},{Csv(t.CategoryType.ToString())}");
        return sb.ToString();
    }

    private static string ScenariosCsv(ExportDto dto)
    {
        var sb = new StringBuilder("\"Name\",\"Strategy\",\"ExtraMonthlyPayment\"\n");
        foreach (var s in dto.Scenarios)
            sb.AppendLine($"{Csv(s.Name)},{Csv(s.Strategy.ToString())},{s.ExtraMonthlyPayment}");
        return sb.ToString();
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
}
