using PersonAgent.Http;

namespace PersonAgent.Tools;

public sealed class GetPersonTool : PersonApiTool
{
    public GetPersonTool(PersonApiClient client, ILogger logger, List<ToolExecution> trace)
        : base(
            ToolNames.GetPerson,
            "Возвращает физлицо через GET /persons/<id>. JSON: id.",
            client,
            logger,
            trace)
    {
    }

    public override async Task<string> ToolTask(string input, CancellationToken token = default)
    {
        if (!ToolArguments.TryRead(input, ToolArguments.GetFields, out var args, out var error) ||
            !ToolArguments.TryGetId(args, out var id, out error))
        {
            return Reject(error);
        }

        LogContract(args);
        var result = await Client.GetAsync(id, token).ConfigureAwait(false);
        if (!result.IsHttpSuccess)
        {
            return HttpFailure(result, args);
        }

        if (!PersonJson.TryReadPerson(result.Body, out var person))
        {
            return Fail(result, "API вернуло некорректный JSON", args);
        }

        var middle = string.IsNullOrWhiteSpace(person.MiddleName) ? "" : " " + person.MiddleName;
        return Succeed(
            result,
            $"Физлицо найдено. Идентификатор: {person.Id}, фамилия: {person.LastName}, имя: {person.FirstName}{middle}.",
            args);
    }
}
