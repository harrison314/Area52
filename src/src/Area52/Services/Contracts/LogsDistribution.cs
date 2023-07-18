namespace Area52.Services.Contracts;

public record LogsDistribution(IReadOnlyList<LogsDistributionItem> Items, TimeSpan SliceInterval);
