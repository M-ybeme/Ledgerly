namespace Ledgerly.Contracts.Goals;

public enum GoalFeasibility { OnTrack, AtRisk, NotFeasible }

public sealed record GoalPlanResultDto(
    GoalFeasibility Feasibility,
    string Summary,
    string? StatusDetail,
    string? Recommendation,
    decimal? RequiredMonthly,
    decimal? CurrentCapacity,
    decimal? Shortfall);
