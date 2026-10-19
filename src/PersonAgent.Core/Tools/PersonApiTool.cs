using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using PersonAgent.Agent;
using PersonAgent.Http;
using LangChain.Chains.StackableChains.Agents.Tools;

namespace PersonAgent.Tools;

public abstract class PersonApiTool : AgentTool
{
    private readonly ILogger _logger;
    private readonly List<ToolExecution> _trace;

    protected PersonApiTool(
        string canonicalName,
        string description,
        PersonApiClient client,
        ILogger logger,
        List<ToolExecution> trace)
        : base(ToolNames.ToReActName(canonicalName), description)
    {
        CanonicalName = canonicalName;
        Client = client;
        _logger = logger;
        _trace = trace;
    }

    protected string CanonicalName { get; }

    protected PersonApiClient Client { get; }

    protected string Reject(string error)
    {
        var contract = ToolArguments.ContractJson(CanonicalName, null);
        _trace.Add(new ToolExecution(CanonicalName, contract, false, null, false, error, error));
        _logger.LogInformation("Результат инструмента {Tool}: вызов API не выполнен, ошибка: {Error}", CanonicalName, error);
        return "Ошибка: " + error;
    }

    protected void LogContract(JsonObject args)
    {
        var contract = ToolArguments.ContractJson(CanonicalName, args);
        _logger.LogInformation("Контракт инструмента: {Contract}", contract);
    }

    protected string Succeed(ApiResult result, string summary, JsonObject args)
    {
        Remember(result, args, true, summary, null);
        _logger.LogInformation(
            "Результат инструмента {Tool}: HTTP {Status}, успех {Success}",
            CanonicalName,
            result.StatusCode,
            "да");
        return summary;
    }

    protected string HttpFailure(ApiResult result, JsonObject args)
    {
        var error = result.TransportError ?? ApiErrorReader.Read(result.Body, result.StatusCode);
        return Fail(result, error, args);
    }

    protected string Fail(ApiResult result, string error, JsonObject? args)
    {
        Remember(result, args, false, error, error);
        _logger.LogInformation(
            "Результат инструмента {Tool}: HTTP {Status}, успех {Success}",
            CanonicalName,
            result.StatusCode?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "нет",
            "нет");
        return "Ошибка: " + error;
    }

    private void Remember(ApiResult result, JsonObject? args, bool success, string summary, string? error)
    {
        _trace.Add(new ToolExecution(
            CanonicalName,
            ToolArguments.ContractJson(CanonicalName, args),
            result.HttpCalled,
            result.StatusCode,
            success,
            summary,
            error));
    }
}
