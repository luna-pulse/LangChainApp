using PersonAgent.Http;

namespace PersonAgent.Tools;

public sealed class UpdatePersonTool : PersonApiTool
{
    public UpdatePersonTool(PersonApiClient client, ILogger logger, List<ToolExecution> trace)
        : base(
            ToolNames.UpdatePerson,
            "Изменяет физлицо через PUT /persons/<id>. JSON: id и хотя бы одно из firstName, lastName, middleName, birthDate.",
            client,
            logger,
            trace)
    {
    }

    public override async Task<string> ToolTask(string input, CancellationToken token = default)
    {
        if (!ToolArguments.TryRead(input, ToolArguments.UpdateFields, out var args, out var error) ||
            !ToolArguments.TryUpdatePayload(args, out var id, out var payload, out error))
        {
            return Reject(error);
        }

        LogContract(args);
        var result = await Client.UpdateAsync(id, payload, token).ConfigureAwait(false);
        if (!result.IsHttpSuccess)
        {
            return HttpFailure(result, args);
        }

        if (!PersonJson.TryReadPerson(result.Body, out _))
        {
            return Fail(result, "API вернуло некорректный JSON", args);
        }

        return Succeed(result, "Готово. Данные физлица изменены", args);
    }
}
