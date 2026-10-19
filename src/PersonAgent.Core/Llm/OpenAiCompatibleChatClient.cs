using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.AI;

namespace PersonAgent.Llm;

public sealed class OpenAiCompatibleChatClient : ChatClientBase
{
    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;

    public OpenAiCompatibleChatClient(HttpClient httpClient, string baseUrl, string model, string? apiKey)
        : base("openai-compatible", Root(baseUrl), model)
    {
        _httpClient = httpClient;
        _endpoint = EndpointFor(baseUrl);
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
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
            temperature = options?.Temperature ?? 0,
            stop = options?.StopSequences,
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(_endpoint, payload, cancellationToken).ConfigureAwait(false);
            var body = await ReadRequiredAsync(response, cancellationToken).ConfigureAwait(false);
            using var document = JsonDocument.Parse(body);
            var message = Property(Property(document.RootElement, "choices")[0], "message");
            var content = Property(message, "content");
            if (content.ValueKind != JsonValueKind.String)
            {
                throw new InvalidOperationException("Модель не вернула текст.");
            }

            return RequireContent(content.GetString());
        }
        catch (HttpRequestException exception)
        {
            throw new InvalidOperationException("OpenAI-совместимый endpoint недоступен.", exception);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException("Превышено время ожидания модели.");
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException("Модель вернула некорректный JSON.", exception);
        }
    }

    private static Uri Root(string baseUrl)
    {
        var text = baseUrl.TrimEnd('/');
        if (text.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
        {
            text = text[..^3];
        }

        return new Uri(text);
    }

    private static Uri EndpointFor(string baseUrl)
    {
        var text = baseUrl.TrimEnd('/');
        var endpoint = text.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
            ? text + "/chat/completions"
            : text + "/v1/chat/completions";
        return new Uri(endpoint);
    }
}
