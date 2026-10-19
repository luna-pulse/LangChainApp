namespace PersonAgent.Agent;

public sealed record AgentRunResult(string Status, string Text, IReadOnlyList<ToolExecution> Executions)
{
    public bool Success => string.Equals(Status, "success", StringComparison.Ordinal);
}
