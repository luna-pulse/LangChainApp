using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using PersonAgent.Http;

namespace PersonAgent.Tools;

public static class ToolArguments
{
    public static readonly string[] CreateFields = ["lastName", "firstName", "middleName", "birthDate"];
    public static readonly string[] GetFields = ["id"];
    public static readonly string[] UpdateFields = ["id", "lastName", "firstName", "middleName", "birthDate"];
    public static readonly string[] ListFields = [];

    private static readonly string[] ForeignNames = ["url", "uri", "endpoint", "path", "href", "host"];

    public static bool TryRead(string input, IReadOnlyCollection<string> allowedFields, out JsonObject args, out string error)
    {
        args = [];
        var trimmed = input.Trim();
        if (trimmed.Length == 0)
        {
            trimmed = "{}";
        }

        if (trimmed.Length > 4000)
        {
            error = "аргументы слишком длинные";
            return false;
        }

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(trimmed);
        }
        catch (JsonException)
        {
            error = "аргументы должны быть JSON-объектом";
            return false;
        }

        if (node is not JsonObject obj)
        {
            error = "аргументы должны быть JSON-объектом";
            return false;
        }

        foreach (var property in obj)
        {
            if (ForeignNames.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
            {
                error = "нельзя передавать адрес или endpoint: инструмент вызывает только фиксированный метод API";
                return false;
            }

            if (property.Value is JsonValue value &&
                value.TryGetValue<string>(out var text) &&
                text.TrimStart().StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                error = "нельзя передавать адрес или endpoint: инструмент вызывает только фиксированный метод API";
                return false;
            }

            if (!allowedFields.Contains(property.Key, StringComparer.OrdinalIgnoreCase))
            {
                error = $"поле {property.Key} не входит в контракт инструмента";
                return false;
            }
        }

        args = obj;
        error = "";
        return true;
    }

    public static string ContractJson(string canonicalName, JsonObject? args)
    {
        var redacted = new JsonObject();
        if (args is not null)
        {
            foreach (var property in args)
            {
                if (property.Key.Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    redacted[property.Key] = property.Value?.DeepClone();
                }
                else
                {
                    redacted[property.Key] = "***";
                }
            }
        }

        var contract = new JsonObject
        {
            ["name"] = canonicalName,
            ["args"] = redacted,
        };
        return contract.ToJsonString(PersonJson.Options);
    }

    public static bool TryCreatePayload(JsonObject args, out CreatePersonPayload payload, out string error)
    {
        payload = new CreatePersonPayload();
        error = "";
        if (!TryName(args, "lastName", required: true, out var lastName, out error) ||
            !TryName(args, "firstName", required: true, out var firstName, out error) ||
            !TryName(args, "middleName", required: false, out var middleName, out error) ||
            !TryBirthDate(args, out var birthDate, out error))
        {
            return false;
        }

        payload.LastName = lastName;
        payload.FirstName = firstName;
        payload.MiddleName = middleName;
        payload.BirthDate = birthDate;
        return true;
    }

    public static bool TryGetId(JsonObject args, out int id, out string error)
    {
        id = 0;
        var node = Find(args, "id");
        if (!TryInt(node, out id) || id <= 0)
        {
            error = "укажите положительный id";
            return false;
        }

        error = "";
        return true;
    }

    public static bool TryUpdatePayload(JsonObject args, out int id, out UpdatePersonPayload payload, out string error)
    {
        payload = new UpdatePersonPayload();
        id = 0;
        if (!TryGetId(args, out id, out error))
        {
            return false;
        }

        var hasField = false;
        if (Find(args, "lastName") is not null)
        {
            hasField = true;
            if (!TryName(args, "lastName", required: true, out var lastName, out error))
            {
                return false;
            }

            payload.LastName = lastName;
        }

        if (Find(args, "firstName") is not null)
        {
            hasField = true;
            if (!TryName(args, "firstName", required: true, out var firstName, out error))
            {
                return false;
            }

            payload.FirstName = firstName;
        }

        if (Find(args, "middleName") is not null)
        {
            hasField = true;
            if (!TryName(args, "middleName", required: true, out var middleName, out error))
            {
                return false;
            }

            payload.MiddleName = middleName;
        }

        if (Find(args, "birthDate") is not null)
        {
            hasField = true;
            if (!TryBirthDate(args, out var birthDate, out error) || birthDate is null)
            {
                error = string.IsNullOrEmpty(error) ? "укажите дату рождения в формате ГГГГ-ММ-ДД" : error;
                return false;
            }

            payload.BirthDate = birthDate;
        }

        if (!hasField)
        {
            error = "укажите id и хотя бы одно поле для изменения";
            return false;
        }

        error = "";
        return true;
    }

    private static bool TryName(JsonObject args, string field, bool required, out string? value, out string error)
    {
        value = null;
        error = "";
        var node = Find(args, field);
        if (node is null)
        {
            if (required)
            {
                error = field switch
                {
                    "lastName" => "укажите фамилию",
                    "firstName" => "укажите имя",
                    _ => $"укажите {field}",
                };
                return false;
            }

            return true;
        }

        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<string>(out var text))
        {
            error = $"поле {field} должно быть строкой";
            return false;
        }

        var trimmed = text.Trim();
        if (trimmed.Length is 0 or > 100 || !IsPersonName(trimmed))
        {
            error = $"поле {field} должно содержать от 1 до 100 букв";
            return false;
        }

        value = trimmed;
        return true;
    }

    private static bool TryBirthDate(JsonObject args, out string? value, out string error)
    {
        value = null;
        error = "";
        var node = Find(args, "birthDate");
        if (node is null)
        {
            return true;
        }

        if (node is not JsonValue jsonValue || !jsonValue.TryGetValue<string>(out var text))
        {
            error = "дата рождения должна быть строкой ГГГГ-ММ-ДД";
            return false;
        }

        if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            error = "дата рождения должна быть в формате ГГГГ-ММ-ДД";
            return false;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (date < new DateOnly(1900, 1, 1) || date > today)
        {
            error = "дата рождения должна быть в диапазоне 1900-01-01..сегодня";
            return false;
        }

        value = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return true;
    }

    private static bool TryInt(JsonNode? node, out int value)
    {
        value = 0;
        if (node is not JsonValue jsonValue)
        {
            return false;
        }

        if (jsonValue.TryGetValue<int>(out value))
        {
            return true;
        }

        return jsonValue.TryGetValue<string>(out var text) &&
            int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out value);
    }

    private static JsonNode? Find(JsonObject args, string name)
    {
        foreach (var property in args)
        {
            if (property.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        return null;
    }

    private static bool IsPersonName(string value)
    {
        if (!char.IsLetter(value[0]))
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (char.IsLetter(ch) || ch is ' ' or '-' or '\'')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
