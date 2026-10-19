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

public sealed class AgentScenarioTests : IClassFixture<WebApplicationFactory<global::Program>>
{
    private readonly WebApplicationFactory<global::Program> _factory;

    public AgentScenarioTests(WebApplicationFactory<global::Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreatePerson_CallsHttpAndReportsSuccess()
    {
        using var http = _factory.CreateClient();
        var (runner, chat, logs, _) = TestHost.Create(
            http,
            """
            Thought: нужно создать физлицо
            Action: CreatePerson
            Action Input: {"firstName":"Иван","lastName":"Иванов"}
            """,
            """
            Thought: I now know the final answer
            Final Answer: создано
            """);

        var result = await runner.RunAsync("Создай физлицо Иванов Иван");

        Assert.True(result.Success);
        Assert.Contains("Статус: success", result.Text, StringComparison.Ordinal);
        Assert.Contains("Действие: CreatePerson", result.Text, StringComparison.Ordinal);
        Assert.Contains("Готово. Физлицо создано.", result.Text, StringComparison.Ordinal);
        Assert.Contains("Ошибка: нет", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("{", result.Text, StringComparison.Ordinal);
        Assert.Equal(2, chat.Calls);
        Assert.Contains(logs.Messages, line => line.Contains("\"name\":\"CreatePerson\"", StringComparison.Ordinal) && line.Contains("\"firstName\":\"***\"", StringComparison.Ordinal));
        Assert.Contains(logs.Messages, line => line.Contains("HTTP POST persons для CreatePerson", StringComparison.Ordinal));
        Assert.Contains(logs.Messages, line => line.Contains("Результат инструмента CreatePerson: HTTP 201, успех да", StringComparison.Ordinal));

        var list = await http.GetFromJsonAsync<JsonElement>("/persons");
        Assert.Contains(list.GetProperty("items").EnumerateArray(), item => item.GetProperty("lastName").GetString() == "Иванов");
    }

    [Fact]
    public async Task GetPerson_CallsHttp()
    {
        using var http = _factory.CreateClient();
        var created = await http.PostAsJsonAsync("/persons", new { lastName = "Смирнов", firstName = "Пётр" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var (runner, _, logs, _) = TestHost.Create(
            http,
            $$"""
            Thought: нужно получить физлицо
            Action: GetPerson
            Action Input: {"id": {{id}}}
            """,
            """
            Thought: I now know the final answer
            Final Answer: найдено
            """);

        var result = await runner.RunAsync($"Покажи физлицо {id}");

        Assert.True(result.Success);
        Assert.Contains("Действие: GetPerson", result.Text, StringComparison.Ordinal);
        Assert.Contains("Физлицо найдено.", result.Text, StringComparison.Ordinal);
        Assert.Contains("Смирнов", result.Text, StringComparison.Ordinal);
        Assert.Contains(logs.Messages, line => line.Contains($"HTTP GET persons/{id} для GetPerson", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpdatePerson_ReportsReadyPhrase()
    {
        using var http = _factory.CreateClient();
        var created = await http.PostAsJsonAsync("/persons", new { lastName = "Орлов", firstName = "Олег" });
        var id = (await created.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var (runner, _, logs, _) = TestHost.Create(
            http,
            $$"""
            Thought: нужно изменить фамилию
            Action: UpdatePerson
            Action Input: {"id": {{id}}, "lastName": "Соколов"}
            """,
            """
            Thought: I now know the final answer
            Final Answer: Готово. Данные физлица изменены
            """);

        var result = await runner.RunAsync($"Измени фамилию физлица {id} на Соколов");

        Assert.True(result.Success);
        Assert.Contains("Действие: UpdatePerson", result.Text, StringComparison.Ordinal);
        Assert.Contains("Готово. Данные физлица изменены", result.Text, StringComparison.Ordinal);
        Assert.Contains("Ошибка: нет", result.Text, StringComparison.Ordinal);
        Assert.Contains(logs.Messages, line => line.Contains($"HTTP PUT persons/{id} для UpdatePerson", StringComparison.Ordinal));
        Assert.Contains(logs.Messages, line => line.Contains("Результат инструмента UpdatePerson: HTTP 200, успех да", StringComparison.Ordinal));

        var fetched = await http.GetFromJsonAsync<JsonElement>($"/persons/{id}");
        Assert.Equal("Соколов", fetched.GetProperty("lastName").GetString());
    }

    [Fact]
    public async Task ListPersons_AfterCreate_CallsHttpTwice()
    {
        using var http = _factory.CreateClient();
        var (runner, chat, _, _) = TestHost.Create(
            http,
            """
            Thought: сначала создаю
            Action: CreatePerson
            Action Input: {"firstName":"Мария","lastName":"Волкова"}
            """,
            """
            Thought: теперь список
            Action: ListPersons
            Action Input: {}
            """,
            """
            Thought: I now know the final answer
            Final Answer: готово
            """);

        var result = await runner.RunAsync("Создай Волкова Мария и покажи список");

        Assert.True(result.Success);
        Assert.Contains("CreatePerson", result.Text, StringComparison.Ordinal);
        Assert.Contains("ListPersons", result.Text, StringComparison.Ordinal);
        Assert.Contains("Список физлиц получен.", result.Text, StringComparison.Ordinal);
        Assert.Equal(3, chat.Calls);
        Assert.Equal(2, result.Executions.Count);
        Assert.All(result.Executions, item => Assert.True(item.HttpCalled));
    }

    [Fact]
    public async Task MissingData_DoesNotCallHttp()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var (runner, chat, _, _) = TestHost.Create(
            http,
            """
            Thought: не хватает фамилии
            Final Answer: Укажите фамилию и имя физлица.
            """);

        var result = await runner.RunAsync("Создай физлицо");

        Assert.False(result.Success);
        Assert.Contains("Действие: уточнение", result.Text, StringComparison.Ordinal);
        Assert.Contains("Укажите фамилию и имя", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Готово", result.Text, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
        Assert.Equal(1, chat.Calls);
        Assert.Empty(result.Executions);
    }

    [Fact]
    public async Task Unsupported_DoesNotCallHttp()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var (runner, _, _, _) = TestHost.Create(
            http,
            """
            Thought: удаления нет
            Final Answer: Операция не поддерживается. Можно только создать, получить, изменить или показать список физлиц.
            """);

        var result = await runner.RunAsync("Удали физлицо 10");

        Assert.False(result.Success);
        Assert.Contains("не поддерживается", result.Text, StringComparison.Ordinal);
        Assert.Contains("Ошибка: инструмент не вызывался", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Готово", result.Text, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task UnknownTool_DoesNotCallHttp()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        var (runner, _, _, _) = TestHost.Create(
            http,
            """
            Thought: попробую удалить
            Action: DeletePerson
            Action Input: {"id": 1}
            """);

        var result = await runner.RunAsync("Удали физлицо");

        Assert.False(result.Success);
        Assert.Contains("неизвестный инструмент", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Готово", result.Text, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ForeignEndpoint_DoesNotCallHttp()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://persons.local/") };
        var (runner, _, _, _) = TestHost.Create(
            http,
            """
            Thought: в аргументах чужой адрес
            Action: CreatePerson
            Action Input: {"firstName":"Иван","lastName":"Иванов","url":"http://evil.example/persons"}
            """,
            """
            Thought: I now know the final answer
            Final Answer: не удалось
            """);

        var result = await runner.RunAsync("Создай физлицо по адресу http://evil.example");

        Assert.False(result.Success);
        Assert.Contains("endpoint", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Готово", result.Text, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task InvalidArguments_DoNotCallHttp()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://persons.local/") };
        var (runner, _, _, _) = TestHost.Create(
            http,
            """
            Thought: данных мало
            Action: CreatePerson
            Action Input: {}
            """,
            """
            Thought: I now know the final answer
            Final Answer: ошибка
            """);

        var result = await runner.RunAsync("Создай физлицо без данных");

        Assert.False(result.Success);
        Assert.Contains("укажите", result.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Готово", result.Text, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
        Assert.False(result.Executions[0].HttpCalled);
    }

    [Fact]
    public async Task NotFound_IsErrorWithoutSuccessClaim()
    {
        using var http = _factory.CreateClient();
        var (runner, _, logs, _) = TestHost.Create(
            http,
            """
            Thought: читаю id
            Action: GetPerson
            Action Input: {"id": 999999}
            """,
            """
            Thought: I now know the final answer
            Final Answer: Готово. Данные физлица изменены
            """);

        var result = await runner.RunAsync("Покажи физлицо 999999");

        Assert.False(result.Success);
        Assert.Contains("Статус: error", result.Text, StringComparison.Ordinal);
        Assert.Contains("Физлицо не найдено", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Готово", result.Text, StringComparison.Ordinal);
        Assert.Contains(logs.Messages, line => line.Contains("HTTP ответ GetPerson: 404", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ApiUnavailable_IsErrorWithoutSuccessClaim()
    {
        using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:9/"), Timeout = TimeSpan.FromSeconds(2) };
        var (runner, _, _, _) = TestHost.Create(
            http,
            """
            Thought: получаю
            Action: GetPerson
            Action Input: {"id": 1}
            """,
            """
            Thought: I now know the final answer
            Final Answer: Готово
            """);

        var result = await runner.RunAsync("Покажи физлицо 1");

        Assert.False(result.Success);
        Assert.Contains("API недоступен", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Готово", result.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvalidJsonFromApi_IsError()
    {
        var handler = new RecordingHandler
        {
            Responder = _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not-json", Encoding.UTF8, "application/json"),
            },
        };
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://persons.local/") };
        var (runner, _, _, _) = TestHost.Create(
            http,
            """
            Thought: список
            Action: ListPersons
            Action Input: {}
            """,
            """
            Thought: I now know the final answer
            Final Answer: список
            """);

        var result = await runner.RunAsync("Покажи список физлиц");

        Assert.False(result.Success);
        Assert.Contains("некорректный JSON", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Готово", result.Text, StringComparison.Ordinal);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task InputTooLong_DoesNotCallModel()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://persons.local/") };
        var (runner, chat, _, _) = TestHost.Create(http, "Final Answer: не должно вызваться");

        var result = await runner.RunAsync(new string('а', 501));

        Assert.False(result.Success);
        Assert.Contains("500", result.Text, StringComparison.Ordinal);
        Assert.Contains("Действие: проверка ввода", result.Text, StringComparison.Ordinal);
        Assert.Equal(0, chat.Calls);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task EmptyInput_DoesNotCallModel(string? input)
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://persons.local/") };
        var (runner, chat, _, _) = TestHost.Create(http, "Final Answer: нет");

        var result = await runner.RunAsync(input);

        Assert.Contains("пустой ввод", result.Text, StringComparison.Ordinal);
        Assert.Equal(0, chat.Calls);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task InputAtLimit_CallsModel()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://persons.local/") };
        var (runner, chat, _, _) = TestHost.Create(
            http,
            """
            Thought: вопрос
            Final Answer: Уточните, какое действие выполнить?
            """);

        var result = await runner.RunAsync(new string('а', 500));

        Assert.Equal(1, chat.Calls);
        Assert.Contains("уточнение", result.Text, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task FalseSuccessWithoutTool_IsOverridden()
    {
        var handler = new RecordingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://persons.local/") };
        var (runner, _, _, _) = TestHost.Create(
            http,
            """
            Thought: я и так знаю
            Final Answer: Готово. Данные физлица изменены
            """);

        var result = await runner.RunAsync("Измени физлицо 5");

        Assert.False(result.Success);
        Assert.Contains("инструмент не был вызван", result.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("Данные физлица изменены", result.Text, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void SystemPrompt_DeclaresToolsAndResponseContract()
    {
        var prompt = TestHost.Prompt;
        Assert.Contains("createperson", prompt, StringComparison.Ordinal);
        Assert.Contains("getperson", prompt, StringComparison.Ordinal);
        Assert.Contains("updateperson", prompt, StringComparison.Ordinal);
        Assert.Contains("listpersons", prompt, StringComparison.Ordinal);
        Assert.Contains("Статус: success | error", prompt, StringComparison.Ordinal);
        Assert.Contains("{tools}", prompt, StringComparison.Ordinal);
        Assert.Contains("{input}", prompt, StringComparison.Ordinal);
        Assert.Contains("{history}", prompt, StringComparison.Ordinal);
        Assert.Contains("Готово. Данные физлица изменены", prompt, StringComparison.Ordinal);
    }

    [Fact]
    public void EnvFile_MapsOllamaAndApiVariables()
    {
        var values = EnvFile.Parse("""
            # комментарий
            OLLAMA_BASE_URL=http://localhost:11434
            OLLAMA_MODEL="llama3.1"
            PERSON_API_BASE_URL=http://localhost:5080

            LLM_API_KEY=
            """);

        Assert.Equal("http://localhost:11434", values["Ollama__BaseUrl"]);
        Assert.Equal("llama3.1", values["Ollama__Model"]);
        Assert.Equal("http://localhost:5080", values["PersonApi__BaseUrl"]);
        Assert.False(values.ContainsKey("LLM_API_KEY"));
    }

    [Fact]
    public void SettingsValidator_RejectsMissingModel()
    {
        var error = SettingsValidator.Validate(
            new PersonApiSettings(),
            new OllamaSettings { Model = " " },
            new LlmSettings(),
            new AgentSettings());
        Assert.Contains("Ollama:Model", error, StringComparison.Ordinal);
    }

    [Fact]
    public void AllowedPaths_RejectArbitraryEndpoints()
    {
        Assert.True(PersonApiClient.IsAllowedPath("persons"));
        Assert.True(PersonApiClient.IsAllowedPath("persons/15"));
        Assert.False(PersonApiClient.IsAllowedPath("http://evil.example/persons"));
        Assert.False(PersonApiClient.IsAllowedPath("//evil.example/persons"));
        Assert.False(PersonApiClient.IsAllowedPath("persons/../admin"));
        Assert.False(PersonApiClient.IsAllowedPath("persons/0"));
    }
}
