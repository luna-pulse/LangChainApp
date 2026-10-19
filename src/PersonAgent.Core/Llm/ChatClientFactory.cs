using PersonAgent.Configuration;

namespace PersonAgent.Llm;

public static class ChatClientFactory
{
    public static IChatClient Create(OllamaSettings ollama, LlmSettings llm, Func<TimeSpan, HttpClient>? httpClientFactory = null)
    {
        httpClientFactory ??= timeout => new HttpClient { Timeout = timeout };
        if (SettingsValidator.IsOpenAiCompatible(llm.Provider))
        {
            var client = httpClientFactory(TimeSpan.FromSeconds(llm.TimeoutSeconds));
            return new OpenAiCompatibleChatClient(client, llm.BaseUrl, llm.Model, llm.ApiKey);
        }

        var ollamaClient = httpClientFactory(TimeSpan.FromSeconds(ollama.TimeoutSeconds));
        return new OllamaChatClient(ollamaClient, ollama.BaseUrl, ollama.Model);
    }
}
