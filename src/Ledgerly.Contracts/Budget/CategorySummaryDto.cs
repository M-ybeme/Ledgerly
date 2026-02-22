using Ledgerly.Domain.Budget;

namespace Ledgerly.Contracts.Budget;

public sealed record CategorySummaryDto(
    Guid CategoryId,
    string CategoryName,
    CategoryType Type,
    decimal Planned,
    decimal Actual,
    decimal Variance);
