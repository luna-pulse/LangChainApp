namespace PersonAgent.Agent;

public sealed record ToolExecution(
    string Name,
    string ContractJson,
    bool HttpCalled,
    int? StatusCode,
    bool Success,
    string Summary,
    string? Error);
