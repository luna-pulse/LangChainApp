using Microsoft.Extensions.AI;

namespace PersonAgent.Tests;

internal sealed class FakeChatClient : IChatClient
{
    private readonly Queue<string> _responses;

    public FakeChatClient(params string[] responses)
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
            throw new InvalidOperationException("Заготовленные ответы FakeChatClient закончились.");
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
