using PersonAgent.Http;

namespace PersonAgent.Tools;

public sealed class ListPersonsTool : PersonApiTool
{
    public ListPersonsTool(PersonApiClient client, ILogger logger, List<ToolExecution> trace)
        : base(
            ToolNames.ListPersons,
            "Возвращает список физлиц через GET /persons. Action Input — пустой JSON-объект.",
            client,
            logger,
            trace)
    {
    }

    public override async Task<string> ToolTask(string input, CancellationToken token = default)
    {
        if (!ToolArguments.TryRead(input, ToolArguments.ListFields, out var args, out var error))
        {
            return Reject(error);
        }

        LogContract(args);
        var result = await Client.ListAsync(token).ConfigureAwait(false);
        if (!result.IsHttpSuccess)
        {
            return HttpFailure(result, args);
        }

        if (!PersonJson.TryReadList(result.Body, out var list))
        {
            return Fail(result, "API вернуло некорректный JSON", args);
        }

        var preview = string.Join(
            "; ",
            list.Items.Take(10).Select(person => $"{person.Id}: {person.LastName} {person.FirstName}"));
        var suffix = list.Items.Count > 10 ? $"; и ещё {list.Items.Count - 10}" : "";
        var names = preview.Length == 0 ? "" : " " + preview + suffix + ".";
        return Succeed(result, $"Список физлиц получен. Количество: {list.Count}.{names}", args);
    }
}
