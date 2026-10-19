namespace PersonAgent.Agent;

public static class ResponseFormatter
{
    public static AgentRunResult FromGuard(string error) =>
        Create("error", "проверка ввода", "запрос не отправлен агенту", error, []);

    public static AgentRunResult FromConfiguration(string error) =>
        Create("error", "проверка конфигурации", "агент не запущен", error, []);

    public static AgentRunResult FromExecutions(IReadOnlyList<ToolExecution> executions, string? modelAnswer)
    {
        if (executions.Count == 0)
        {
            return FromModelOnly(modelAnswer);
        }

        var success = executions.All(item => item.Success);
        var action = string.Join("; ", executions.Select(item => item.Name));
        var result = string.Join(" ", executions.Select(item => item.Summary));
        var error = success
            ? "нет"
            : string.Join(" ", executions.Where(item => !item.Success).Select(item => item.Error ?? item.Summary));
        return Create(success ? "success" : "error", action, result, error, executions);
    }

    public static AgentRunResult FromFailure(string action, string error, IReadOnlyList<ToolExecution> executions)
    {
        if (executions.Count > 0)
        {
            return FromExecutions(executions, null);
        }

        return Create("error", action, "действие не выполнено", error, executions);
    }

    private static AgentRunResult FromModelOnly(string? modelAnswer)
    {
        var text = (modelAnswer ?? string.Empty).Trim();
        if (text.Length == 0)
        {
            return Create(
                "error",
                "без вызова API",
                "действие не выполнено",
                "модель не вызвала инструмент и не дала пояснения",
                []);
        }

        if (text.StartsWith('{') || text.StartsWith('['))
        {
            return Create(
                "error",
                "разбор ответа модели",
                "действие не выполнено",
                "вместо вызова инструмента получен сырой JSON",
                []);
        }

        if (ClaimsSuccess(text))
        {
            return Create(
                "error",
                "без вызова API",
                "действие не выполнено",
                "модель сообщила об успехе, но инструмент не был вызван",
                []);
        }

        if (IsClarification(text))
        {
            return Create("error", "уточнение", text, "недостаточно данных для вызова инструмента", []);
        }

        return Create("error", "без вызова API", text, "инструмент не вызывался", []);
    }

    private static bool ClaimsSuccess(string text)
    {
        var lower = text.ToLowerInvariant();
        return lower.Contains("готово", StringComparison.Ordinal)
            || lower.Contains("физлицо создано", StringComparison.Ordinal)
            || lower.Contains("данные физлица изменены", StringComparison.Ordinal)
            || lower.Contains("успешно", StringComparison.Ordinal)
            || lower.Contains("статус: success", StringComparison.Ordinal)
            || lower.Contains("status: success", StringComparison.Ordinal);
    }

    private static bool IsClarification(string text)
    {
        var lower = text.ToLowerInvariant();
        return text.Contains('?')
            || lower.Contains("укажите", StringComparison.Ordinal)
            || lower.Contains("уточн", StringComparison.Ordinal)
            || lower.Contains("не хвата", StringComparison.Ordinal)
            || lower.Contains("какой", StringComparison.Ordinal)
            || lower.Contains("какая", StringComparison.Ordinal)
            || lower.Contains("какое", StringComparison.Ordinal)
            || lower.Contains("какие", StringComparison.Ordinal);
    }

    private static AgentRunResult Create(
        string status,
        string action,
        string result,
        string error,
        IReadOnlyList<ToolExecution> executions)
    {
        var text = string.Join(
            Environment.NewLine,
            [
                $"Статус: {status}",
                $"Действие: {OneLine(action)}",
                $"Результат: {OneLine(result)}",
                $"Ошибка: {OneLine(error)}",
            ]);
        return new AgentRunResult(status, text, executions);
    }

    private static string OneLine(string value)
    {
        var flat = value.Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal).Trim();
        return flat.Length == 0 ? "нет данных" : flat;
    }
}
