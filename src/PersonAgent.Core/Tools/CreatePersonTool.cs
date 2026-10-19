using PersonAgent.Http;

namespace PersonAgent.Tools;

public sealed class CreatePersonTool : PersonApiTool
{
    public CreatePersonTool(PersonApiClient client, ILogger logger, List<ToolExecution> trace)
        : base(
            ToolNames.CreatePerson,
            "Создаёт физлицо через POST /persons. JSON: firstName, lastName, необязательно middleName и birthDate (ГГГГ-ММ-ДД).",
            client,
            logger,
            trace)
    {
    }

    public override async Task<string> ToolTask(string input, CancellationToken token = default)
    {
        if (!ToolArguments.TryRead(input, ToolArguments.CreateFields, out var args, out var error))
        {
            return Reject(error);
        }

        if (!ToolArguments.TryCreatePayload(args, out var payload, out error))
        {
            LogContract(args);
            return Reject(error);
        }

        LogContract(args);
        var result = await Client.CreateAsync(payload, token).ConfigureAwait(false);
        if (!result.IsHttpSuccess)
        {
            return HttpFailure(result, args);
        }

        if (!PersonJson.TryReadPerson(result.Body, out var person))
        {
            return Fail(result, "API вернуло некорректный JSON", args);
        }

        return Succeed(result, $"Готово. Физлицо создано. Идентификатор: {person.Id}.", args);
    }
}
