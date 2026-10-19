using System.Net;
using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using PersonAgent.Agent;
using PersonAgent.Http;

namespace PersonAgent.Tests;

internal sealed class ScriptedChatClient : IChatClient
{
    private readonly Queue<string> _responses;

    public ScriptedChatClient(params string[] responses)
    {
        _responses = new Queue<string>(responses);
    }

    public List<string> Prompts { get; } = [];

    public int Calls => Prompts.Count;

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Prompts.Add(string.Join("\n", messages.Select(message => message.Text)));
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException("Скрипт модели закончился.");
        }

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, _responses.Dequeue())));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

internal sealed class ThrowingChatClient : IChatClient
{
    private readonly Exception _exception;

    public ThrowingChatClient(Exception exception)
    {
        _exception = exception;
    }

    public int Calls { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        Calls++;
        throw _exception;
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}

internal sealed class CollectingLoggerProvider : ILoggerProvider
{
    public List<string> Messages { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CollectingLogger(Messages);

    public void Dispose()
    {
    }

    private sealed class CollectingLogger(List<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            messages.Add(formatter(state, exception));
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();

        public void Dispose()
        {
        }
    }
}

internal sealed class RecordingHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = [];

    public Func<HttpRequestMessage, HttpResponseMessage>? Responder { get; set; }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var response = Responder?.Invoke(request) ?? new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}

internal static class TestHost
{
    public static string Prompt => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Prompts", "system.prompt"));

    public static (PersonAgentRunner Runner, ScriptedChatClient Chat, CollectingLoggerProvider Logs, HttpClient Http) Create(
        HttpClient http,
        params string[] responses)
    {
        var logs = new CollectingLoggerProvider();
        var factory = LoggerFactory.Create(builder => builder.AddProvider(logs));
        var chat = new ScriptedChatClient(responses);
        var runner = new PersonAgentRunner(
            chat,
            new PersonApiClient(http, factory.CreateLogger<PersonApiClient>()),
            factory.CreateLogger<PersonAgentRunner>(),
            Prompt,
            maxActions: 6);
        return (runner, chat, logs, http);
    }
}
