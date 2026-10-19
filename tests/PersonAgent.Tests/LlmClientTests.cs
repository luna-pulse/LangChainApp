using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using PersonAgent.Agent;
using PersonAgent.Configuration;
using PersonAgent.Http;
using PersonAgent.Llm;

namespace PersonAgent.Tests;

public sealed class LlmClientTests
{
    [Fact]
    public async Task OllamaClient_ParsesNativeChatResponse_AndStopsBeforeObservation()
    {
        var handler = new RecordingHandler
        {
            Responder = request =>
            {
                Assert.Equal("http://localhost:11434/api/chat", request.RequestUri!.ToString());
                return Json("""
                    {"message":{"role":"assistant","content":"Action: ListPersons\nAction Input: {}\nObservation: скрыто"}}
                    """);
            },
        };
        using var http = new HttpClient(handler);
        var client = new OllamaChatClient(http, "http://localhost:11434", "llama3.1");

        var response = await client.GetResponseAsync(
            [new(ChatRole.User, "покажи список")],
            new ChatOptions { StopSequences = ["Observation"], Temperature = 0 });

        Assert.Contains("Action: ListPersons", response.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("скрыто", response.Text, StringComparison.Ordinal);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task OllamaClient_WhenUnreachable_ReportsUnavailable()
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var client = new OllamaChatClient(http, "http://127.0.0.1:9", "llama3.1");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.GetResponseAsync([new(ChatRole.User, "ping")]));

        Assert.Contains("Ollama недоступен", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenAiCompatibleClient_ParsesChatCompletion()
    {
        var handler = new RecordingHandler
        {
            Responder = request =>
            {
                Assert.Equal("http://models.local/v1/chat/completions", request.RequestUri!.ToString());
                Assert.Equal("Bearer", request.Headers.Authorization?.Scheme);
                return Json("""
                    {"choices":[{"message":{"role":"assistant","content":"Final Answer: уточните имя"}}]}
                    """);
            },
        };
        using var http = new HttpClient(handler);
        var client = new OpenAiCompatibleChatClient(http, "http://models.local/v1", "composer", "test-key");

        var response = await client.GetResponseAsync([new(ChatRole.User, "создай")]);

        Assert.Contains("уточните имя", response.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Factory_SelectsOllamaByDefault_AndOpenAiWhenConfigured()
    {
        using var ollama = Assert.IsType<OllamaChatClient>(ChatClientFactory.Create(new OllamaSettings(), new LlmSettings()));
        Assert.Equal("http://localhost:11434/api/chat", ollama.Endpoint.ToString());

        using var compatible = Assert.IsType<OpenAiCompatibleChatClient>(ChatClientFactory.Create(
            new OllamaSettings(),
            new LlmSettings
            {
                Provider = "OpenAICompatible",
                BaseUrl = "http://models.local/v1",
                Model = "composer",
            }));
        Assert.Equal("http://models.local/v1/chat/completions", compatible.Endpoint.ToString());
    }

    [SkippableFact]
    public async Task LiveModel_CreatePerson_WhenConfigured()
    {
        var baseUrl = Environment.GetEnvironmentVariable("LLM_INTEGRATION_BASE_URL");
        var model = Environment.GetEnvironmentVariable("LLM_INTEGRATION_MODEL");
        Skip.If(string.IsNullOrWhiteSpace(baseUrl), "LLM_INTEGRATION_BASE_URL не задан");
        Skip.If(string.IsNullOrWhiteSpace(model), "LLM_INTEGRATION_MODEL не задан");

        await using var factory = new WebApplicationFactory<global::Program>();
        using var api = factory.CreateClient();
        using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        var chat = new OpenAiCompatibleChatClient(
            http,
            baseUrl!,
            model!,
            Environment.GetEnvironmentVariable("LLM_INTEGRATION_API_KEY"));
        using var logs = LoggerFactory.Create(builder => builder.AddProvider(new CollectingLoggerProvider()));
        var runner = new PersonAgentRunner(
            chat,
            new PersonApiClient(api, logs.CreateLogger<PersonApiClient>()),
            logs.CreateLogger<PersonAgentRunner>(),
            TestHost.Prompt,
            maxActions: 6);

        var result = await runner.RunAsync("Создай физлицо: фамилия Тестов, имя Пётр.");

        Assert.True(result.Success, result.Text);
        Assert.Contains("CreatePerson", result.Text, StringComparison.Ordinal);
        Assert.Contains("Готово. Физлицо создано.", result.Text, StringComparison.Ordinal);
        var list = await api.GetFromJsonAsync<JsonElement>("/persons");
        Assert.Contains(
            list.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("lastName").GetString() == "Тестов" && item.GetProperty("firstName").GetString() == "Пётр");
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json"),
    };
}
