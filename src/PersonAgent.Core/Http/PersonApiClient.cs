using System.Text;
using System.Text.Json;
using PersonAgent.Tools;

namespace PersonAgent.Http;

public sealed class PersonApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PersonApiClient> _logger;

    public PersonApiClient(HttpClient httpClient, ILogger<PersonApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public Task<ApiResult> CreateAsync(CreatePersonPayload payload, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Post, "persons", ToolNames.CreatePerson, payload, cancellationToken);

    public Task<ApiResult> GetAsync(int id, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, $"persons/{id}", ToolNames.GetPerson, null, cancellationToken);

    public Task<ApiResult> UpdateAsync(int id, UpdatePersonPayload payload, CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Put, $"persons/{id}", ToolNames.UpdatePerson, payload, cancellationToken);

    public Task<ApiResult> ListAsync(CancellationToken cancellationToken) =>
        SendAsync(HttpMethod.Get, "persons", ToolNames.ListPersons, null, cancellationToken);

    private async Task<ApiResult> SendAsync(
        HttpMethod method,
        string relativePath,
        string toolName,
        object? body,
        CancellationToken cancellationToken)
    {
        if (!IsAllowedPath(relativePath))
        {
            _logger.LogInformation("HTTP вызов {Tool} отклонён: путь вне контракта", toolName);
            return ApiResult.Transport("отклонён путь вне фиксированного контракта API");
        }

        try
        {
            using var request = new HttpRequestMessage(method, relativePath);
            request.Headers.Accept.ParseAdd("application/json");
            if (body is not null)
            {
                request.Content = new StringContent(
                    JsonSerializer.Serialize(body, body.GetType(), PersonJson.Options),
                    Encoding.UTF8,
                    "application/json");
            }

            _logger.LogInformation("HTTP {Method} {Path} для {Tool}", method.Method, relativePath, toolName);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("HTTP ответ {Tool}: {Status}", toolName, (int)response.StatusCode);
            return new ApiResult(true, (int)response.StatusCode, responseBody, null);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogInformation(exception, "HTTP {Tool}: API недоступен", toolName);
            return ApiResult.Transport("API недоступен");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("HTTP {Tool}: превышено время ожидания API", toolName);
            return ApiResult.Transport("Превышено время ожидания API");
        }
        catch (JsonException)
        {
            return ApiResult.Transport("не удалось сформировать JSON запроса");
        }
    }

    public static bool IsAllowedPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            relativePath.Contains("://", StringComparison.Ordinal) ||
            relativePath.StartsWith("//", StringComparison.Ordinal) ||
            relativePath.Contains("..", StringComparison.Ordinal) ||
            relativePath.Contains('\\'))
        {
            return false;
        }

        if (string.Equals(relativePath, "persons", StringComparison.Ordinal))
        {
            return true;
        }

        const string prefix = "persons/";
        if (!relativePath.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(relativePath[prefix.Length..], out var id) && id > 0;
    }
}
