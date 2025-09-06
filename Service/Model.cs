namespace Dark;

public readonly record struct Data(string Aggregate, string Id);

public readonly record struct Condition(string? Query = null, int? Version = 0);
