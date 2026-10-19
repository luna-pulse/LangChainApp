using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace PersonAgent.Llm;

public sealed class OllamaChatClient : ChatClientBase
{
    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;

    public OllamaChatClient(HttpClient httpClient, string baseUrl, string model)
        : base("ollama", Normalize(baseUrl), model)
    {
        _httpClient = httpClient;
        _endpoint = new Uri(Normalize(baseUrl), "api/chat");
    }

    public Uri Endpoint => _endpoint;

    protected override async Task<string> CompleteAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            model = string.IsNullOrWhiteSpace(options?.ModelId) ? Model : options.ModelId,
            messages = ToWireMessages(messages),
            stream = false,
            options = new
            {
                temperature = options?.Temperature ?? 0,
                stop = options?.StopSequences ?? Array.Empty<string>(),
            },
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(_endpoint, payload, cancellationToken).ConfigureAwait(false);
            var body = await ReadRequiredAsync(response, cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(body);
            var content = Property(Property(document.RootElement, "message"), "content").GetString();
            return RequireContent(content);
        }
        catch (HttpRequestException exception)
        {
            throw new InvalidOperationException("Ollama недоступен.", exception);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Превышено время ожидания Ollama.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Ollama вернуло некорректный JSON.", exception);
        }
    }

    private static Uri Normalize(string baseUrl)
    {
        var text = baseUrl.TrimEnd('/');
        if (text.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^4];
        }

        if (!text.EndsWith('/'))
        {
            text += "/";
        }

        if (!Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Адрес Ollama задан неверно.");
        }

        return uri;
    }
}
