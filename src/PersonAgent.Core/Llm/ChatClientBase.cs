using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace PersonAgent.Llm;

public abstract class ChatClientBase : IChatClient, IDisposable
{
    private readonly ChatClientMetadata _metadata;

    protected ChatClientBase(string provider, Uri endpoint, string model)
    {
        _metadata = new ChatClientMetadata(provider, endpoint, model);
        Model = model;
    }

    protected string Model { get; }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var text = await CompleteAsync(messages, options, cancellationToken).ConfigureAwait(false);
        return new ChatResponse(new ChatMessage(ChatRole.Assistant, ApplyStops(text, options?.StopSequences)));
    }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        yield return new ChatResponseUpdate(ChatRole.Assistant, response.Text);
    }

    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        if (serviceKey is null && serviceType == typeof(ChatClientMetadata))
        {
            return _metadata;
        }

        return null;
    }

    public void Dispose()
    {
    }

    protected abstract Task<string> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken);

    protected static IReadOnlyList<object> ToWireMessages(IEnumerable<ChatMessage> messages) =>
        messages.Select(message => (object)new
        {
            role = message.Role == ChatRole.Assistant ? "assistant"
                : message.Role == ChatRole.System ? "system"
                : "user",
            content = message.Text ?? string.Empty,
        }).ToArray();

    protected static string ApplyStops(string text, IList<string>? stops)
    {
        if (stops is null || stops.Count == 0)
        {
            return text.Trim();
        }

        var cut = text.Length;
        foreach (var stop in stops)
        {
            if (string.IsNullOrEmpty(stop))
            {
                continue;
            }

            var index = text.IndexOf(stop, StringComparison.Ordinal);
            if (index >= 0 && index < cut)
            {
                cut = index;
            }
        }

        return text[..cut].Trim();
    }

    protected static async Task<string> ReadRequiredAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Модель вернула статус {(int)response.StatusCode}.");
        }

        return body;
    }

    protected static string RequireContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("Модель не вернула текст.");
        }

        return content;
    }

    protected static JsonElement Property(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
        {
            throw new InvalidOperationException("Ответ модели имеет неожиданный формат.");
        }

        return value;
    }
}
