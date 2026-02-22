using Ledgerly.Contracts.Scenarios;
using Ledgerly.Domain.Scenarios;

namespace Ledgerly.Application.Scenarios;

/// <summary>
/// Pure service — no injected repositories. Projects both scenarios and computes a side-by-side comparison.
/// </summary>
public sealed class ScenarioComparisonService
{
    private readonly DebtProjectionService _projection;

    public ScenarioComparisonService(DebtProjectionService projection)
        => _projection = projection;

    public ScenarioComparisonDto Compare(Scenario scenarioA, Scenario scenarioB)
    {
        var projA = _projection.Project(scenarioA);
        var projB = _projection.Project(scenarioB);

        var summaryA = new ScenarioSummaryDto(
            scenarioA.Id, scenarioA.Name, scenarioA.Strategy,
            scenarioA.ExtraMonthlyPayment,
            projA.TotalMonths, projA.TotalInterestPaid, projA.TotalPaid);

        var summaryB = new ScenarioSummaryDto(
            scenarioB.Id, scenarioB.Name, scenarioB.Strategy,
            scenarioB.ExtraMonthlyPayment,
            projB.TotalMonths, projB.TotalInterestPaid, projB.TotalPaid);

        // Positive = B is faster / saves more interest relative to A
        int monthsSaved = projA.TotalMonths - projB.TotalMonths;
        decimal interestSaved = projA.TotalInterestPaid - projB.TotalInterestPaid;

        string winnerLabel;
        if (projA.TotalInterestPaid < projB.TotalInterestPaid)
            winnerLabel = scenarioA.Name;
        else if (projB.TotalInterestPaid < projA.TotalInterestPaid)
            winnerLabel = scenarioB.Name;
        else if (projA.TotalMonths < projB.TotalMonths)
            winnerLabel = scenarioA.Name;
        else if (projB.TotalMonths < projA.TotalMonths)
            winnerLabel = scenarioB.Name;
        else
            winnerLabel = "Equal";

        return new ScenarioComparisonDto(summaryA, summaryB, monthsSaved, interestSaved, winnerLabel);
    }
}
