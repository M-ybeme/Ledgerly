namespace Ledgerly.Contracts.Scenarios;

/// <summary>
/// Side-by-side comparison of two scenario projections.
/// MonthsSaved = A.TotalMonths - B.TotalMonths (positive = B is faster).
/// InterestSaved = A.TotalInterestPaid - B.TotalInterestPaid (positive = B saves more interest).
/// WinnerLabel = name of the scenario with lower total interest, or "Equal".
/// </summary>
public sealed record ScenarioComparisonDto(
    ScenarioSummaryDto ScenarioA,
    ScenarioSummaryDto ScenarioB,
    int MonthsSaved,
    decimal InterestSaved,
    string WinnerLabel);
