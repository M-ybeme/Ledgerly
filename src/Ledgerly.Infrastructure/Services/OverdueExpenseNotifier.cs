using Ledgerly.Application.Auth;
using Ledgerly.Domain.Budget;
using Ledgerly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ledgerly.Infrastructure.Services;

public sealed class OverdueExpenseNotifier : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OverdueExpenseNotifier> _logger;

    public OverdueExpenseNotifier(IServiceScopeFactory scopeFactory, ILogger<OverdueExpenseNotifier> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await NotifyAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error running overdue expense notifications");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task NotifyAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LedgerlyDbContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var webBase = config["Web:BaseUrl"]?.TrimEnd('/') ?? "https://ledgerly.app";

        var today = DateOnly.FromDateTime(DateTime.Today);
        var cutoff = today.AddDays(-14); // Only notify about expenses overdue within the past 2 weeks

        var overdue = await db.PlannedExpenses
            .IgnoreQueryFilters()
            .Where(e => e.PaidDate == null && e.DueDate >= cutoff && e.DueDate < today)
            .ToListAsync(ct);

        if (overdue.Count == 0) return;

        var byUser = overdue.GroupBy(e => e.UserId);

        foreach (var group in byUser)
        {
            var user = await db.Users.FindAsync([group.Key], cancellationToken: ct);
            if (user?.Email is null) continue;

            var items = group.OrderBy(e => e.DueDate).ToList();
            _logger.LogInformation("Sending overdue reminder to {Email} ({Count} items)", user.Email, items.Count);

            await emailService.SendAsync(
                user.Email,
                $"You have {items.Count} overdue expense{(items.Count == 1 ? "" : "s")} — Ledgerly",
                BuildEmailBody(items, webBase));
        }
    }

    private static string BuildEmailBody(List<PlannedExpense> items, string webBase)
    {
        var rows = string.Join("", items.Select(e =>
            $"<tr><td style='padding:6px 12px;'>{System.Web.HttpUtility.HtmlEncode(e.Description)}</td>" +
            $"<td style='padding:6px 12px;'>{e.DueDate:MMM d, yyyy}</td>" +
            $"<td style='padding:6px 12px;'>${e.PlannedAmount:N2}</td></tr>"));

        return $"""
            <div style="font-family:sans-serif; max-width:560px;">
              <h2 style="color:#054849;">Overdue Expenses Reminder</h2>
              <p>You have <strong>{items.Count}</strong> overdue planned expense{(items.Count == 1 ? "" : "s")} in Ledgerly:</p>
              <table style="border-collapse:collapse; width:100%;">
                <thead>
                  <tr style="background:#f4f4f4;">
                    <th style="padding:6px 12px; text-align:left;">Description</th>
                    <th style="padding:6px 12px; text-align:left;">Due Date</th>
                    <th style="padding:6px 12px; text-align:left;">Amount</th>
                  </tr>
                </thead>
                <tbody>{rows}</tbody>
              </table>
              <p style="margin-top:1.5rem;">
                <a href="{webBase}/planned-expenses"
                   style="background:#b5d542; color:#021e1e; padding:8px 20px; border-radius:4px; text-decoration:none; font-weight:bold;">
                  View in Ledgerly
                </a>
              </p>
              <p style="color:#888; font-size:.85em;">You're receiving this because you have an account on Ledgerly.</p>
            </div>
            """;
    }
}
