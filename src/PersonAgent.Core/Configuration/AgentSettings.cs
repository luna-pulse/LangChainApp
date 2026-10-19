namespace PersonAgent.Configuration;

public sealed class PersonApiSettings
{
    public const string SectionName = "PersonApi";

    public string BaseUrl { get; set; } = "http://localhost:5080";

    public int TimeoutSeconds { get; set; } = 30;
}

public sealed class OllamaSettings
{
    public const string SectionName = "Ollama";

    public string BaseUrl { get; set; } = "http://localhost:11434";

    public string Model { get; set; } = "llama3.1";

    public int TimeoutSeconds { get; set; } = 120;
}

public sealed class LlmSettings
{
    public const string SectionName = "Llm";

    public string Provider { get; set; } = "Ollama";

    public string BaseUrl { get; set; } = "";

    public string Model { get; set; } = "";

    public string ApiKey { get; set; } = "";

    public int TimeoutSeconds { get; set; } = 120;
}

public sealed class AgentSettings
{
    public const string SectionName = "Agent";

    public int MaxActions { get; set; } = 6;
}

public static class SettingsValidator
{
    public static string? Validate(PersonApiSettings api, OllamaSettings ollama, LlmSettings llm, AgentSettings agent)
    {
        if (!IsHttpUrl(api.BaseUrl))
        {
            return "PersonApi:BaseUrl должен быть абсолютным http(s)-адресом";
        }

        if (api.TimeoutSeconds is < 1 or > 600)
        {
            return "PersonApi:TimeoutSeconds должен быть от 1 до 600";
        }

        if (agent.MaxActions is < 1 or > 12)
        {
            return "Agent:MaxActions должен быть от 1 до 12";
        }

        if (IsOpenAiCompatible(llm.Provider))
        {
            if (!IsHttpUrl(llm.BaseUrl))
            {
                return "Для провайдера OpenAICompatible укажите Llm:BaseUrl";
            }

            if (string.IsNullOrWhiteSpace(llm.Model))
            {
                return "Для провайдера OpenAICompatible укажите Llm:Model";
            }

            if (llm.TimeoutSeconds is < 1 or > 600)
            {
                return "Llm:TimeoutSeconds должен быть от 1 до 600";
            }

            return null;
        }

        if (!string.Equals(llm.Provider, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            return "Llm:Provider может быть Ollama или OpenAICompatible";
        }

        if (!IsHttpUrl(ollama.BaseUrl))
        {
            return "Ollama:BaseUrl должен быть абсолютным http(s)-адресом";
        }

        if (string.IsNullOrWhiteSpace(ollama.Model))
        {
            return "Ollama:Model не задан";
        }

        if (ollama.TimeoutSeconds is < 1 or > 600)
        {
            return "Ollama:TimeoutSeconds должен быть от 1 до 600";
        }

        return null;
    }

    public static bool IsOpenAiCompatible(string? provider) =>
        string.Equals(provider, "OpenAICompatible", StringComparison.OrdinalIgnoreCase);

    private static bool IsHttpUrl(string? value) =>
        Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
