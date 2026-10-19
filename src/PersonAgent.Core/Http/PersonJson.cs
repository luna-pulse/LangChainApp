using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PersonAgent.Http;

public static class PersonJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static bool TryReadPerson(string? body, out PersonBody person)
    {
        person = default!;
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<PersonBody>(body, Options);
            if (parsed is null || parsed.Id <= 0 || string.IsNullOrWhiteSpace(parsed.LastName) || string.IsNullOrWhiteSpace(parsed.FirstName))
            {
                return false;
            }

            person = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryReadList(string? body, out PersonListBody list)
    {
        list = default!;
        if (string.IsNullOrWhiteSpace(body))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<PersonListBody>(body, Options);
            if (parsed?.Items is null || parsed.Count < 0)
            {
                return false;
            }

            list = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed record PersonBody(int Id, string LastName, string FirstName, string? MiddleName, DateOnly? BirthDate);

public sealed record PersonListBody(IReadOnlyList<PersonBody> Items, int Count);
