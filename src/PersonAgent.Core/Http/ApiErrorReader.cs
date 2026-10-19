using System.Text.Json;

namespace PersonAgent.Http;

public static class ApiErrorReader
{
    public static string Read(string? body, int? statusCode)
    {
        if (!string.IsNullOrWhiteSpace(body))
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;
                var title = root.TryGetProperty("title", out var titleProperty) && titleProperty.ValueKind == JsonValueKind.String
                    ? titleProperty.GetString()
                    : null;
                var details = new List<string>();
                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var property in errors.EnumerateObject())
                    {
                        if (property.Value.ValueKind != JsonValueKind.Array)
                        {
                            continue;
                        }

                        foreach (var item in property.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String && item.GetString() is { Length: > 0 } message)
                            {
                                details.Add(message);
                            }
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(title) && details.Count > 0)
                {
                    return $"{title}: {string.Join("; ", details)}";
                }

                if (details.Count > 0)
                {
                    return string.Join("; ", details);
                }

                if (!string.IsNullOrWhiteSpace(title))
                {
                    return title;
                }
            }
            catch (JsonException)
            {
                return statusCode is null
                    ? "API вернуло некорректный JSON"
                    : $"API вернуло статус {statusCode} и некорректный JSON";
            }
        }

        return statusCode is null ? "ошибка API" : $"API вернуло статус {statusCode}";
    }
}
