namespace PersonAgent.Configuration;

public static class EnvFile
{
    public static IReadOnlyDictionary<string, string> Parse(string content)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var raw in content.Split(['\r', '\n'], StringSplitOptions.None))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            var separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"'))
            {
                value = value[1..^1];
            }

            if (key.Length == 0 || value.Length == 0)
            {
                continue;
            }

            values[key] = value;
            var mapped = Map(key);
            if (mapped is not null)
            {
                values[mapped] = value;
            }
        }

        return values;
    }

    public static void ApplyIfMissing(IReadOnlyDictionary<string, string> values)
    {
        foreach (var (key, value) in values)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            {
                Environment.SetEnvironmentVariable(key, value);
            }
        }
    }

    public static string? Map(string key) => key switch
    {
        "PERSON_API_BASE_URL" => "PersonApi__BaseUrl",
        "PERSON_API_TIMEOUT_SECONDS" => "PersonApi__TimeoutSeconds",
        "OLLAMA_BASE_URL" => "Ollama__BaseUrl",
        "OLLAMA_MODEL" => "Ollama__Model",
        "OLLAMA_TIMEOUT_SECONDS" => "Ollama__TimeoutSeconds",
        "AGENT_MAX_ACTIONS" => "Agent__MaxActions",
        "LLM_PROVIDER" => "Llm__Provider",
        "LLM_BASE_URL" => "Llm__BaseUrl",
        "LLM_MODEL" => "Llm__Model",
        "LLM_API_KEY" => "Llm__ApiKey",
        "LLM_TIMEOUT_SECONDS" => "Llm__TimeoutSeconds",
        _ => null,
    };
}
