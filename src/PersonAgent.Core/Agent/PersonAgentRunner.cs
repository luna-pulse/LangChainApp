using PersonAgent.Http;
using PersonAgent.Tools;
using static LangChain.Chains.Chain;

namespace PersonAgent.Agent;

public sealed class PersonAgentRunner
{
    private readonly IChatClient _chatClient;
    private readonly PersonApiClient _apiClient;
    private readonly ILogger<PersonAgentRunner> _logger;
    private readonly string _prompt;
    private readonly int _maxActions;

    public PersonAgentRunner(
        IChatClient chatClient,
        PersonApiClient apiClient,
        ILogger<PersonAgentRunner> logger,
        string prompt,
        int maxActions)
    {
        _chatClient = chatClient;
        _apiClient = apiClient;
        _logger = logger;
        _prompt = prompt;
        _maxActions = maxActions;
    }

    public async Task<AgentRunResult> RunAsync(string? input, CancellationToken cancellationToken = default)
    {
        var rejection = InputGuard.RejectReason(input);
        if (rejection is not null)
        {
            _logger.LogInformation("Ввод отклонён до обращения к модели: {Reason}", rejection);
            return ResponseFormatter.FromGuard(rejection);
        }

        var userText = input!.Trim();
        var trace = new List<ToolExecution>();
        _logger.LogInformation("Запрос передан агенту LangChain, длина {Length}", userText.Length);

        try
        {
            var executor = ReActAgentExecutor(_chatClient, _prompt, maxActions: _maxActions)
                .UseTool(new CreatePersonTool(_apiClient, _logger, trace))
                .UseTool(new GetPersonTool(_apiClient, _logger, trace))
                .UseTool(new UpdatePersonTool(_apiClient, _logger, trace))
                .UseTool(new ListPersonsTool(_apiClient, _logger, trace));

            var chain = Set(userText) | executor;
            var modelAnswer = await chain.RunAsync("text", cancellationToken: cancellationToken).ConfigureAwait(false);
            var result = ResponseFormatter.FromExecutions(trace, modelAnswer);
            LogOutcome(result);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogInformation(exception, "Сбой агента LangChain");
            var result = IsUnknownTool(exception)
                ? ResponseFormatter.FromFailure("неизвестный инструмент", "имя инструмента не входит в фиксированный набор", trace)
                : ResponseFormatter.FromFailure("вызов модели", Describe(exception), trace);
            LogOutcome(result);
            return result;
        }
    }

    private void LogOutcome(AgentRunResult result)
    {
        var tools = result.Executions.Count == 0
            ? "нет"
            : string.Join(",", result.Executions.Select(item => item.Name));
        _logger.LogInformation("Итог агента: статус {Status}, инструменты {Tools}", result.Status, tools);
    }

    private static bool IsUnknownTool(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is KeyNotFoundException)
            {
                return true;
            }
        }

        return false;
    }

    private static string Describe(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("Ollama", StringComparison.Ordinal) ||
                current.Message.Contains("OpenAI-совместимый", StringComparison.Ordinal) ||
                current.Message.Contains("время ожидания", StringComparison.OrdinalIgnoreCase))
            {
                return current.Message;
            }
        }

        if (exception.Message.Contains("Could not parse LLM output", StringComparison.Ordinal) ||
            exception.Message.Contains("Invalid Format", StringComparison.Ordinal))
        {
            return "модель вернула ответ не в формате ReAct, инструмент не подтверждён";
        }

        return "не удалось получить ответ модели";
    }
}
