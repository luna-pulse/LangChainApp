namespace PersonAgent.Http;

public sealed record ApiResult(bool HttpCalled, int? StatusCode, string? Body, string? TransportError)
{
    public static ApiResult Transport(string error) => new(false, null, null, error);

    public bool IsHttpSuccess => HttpCalled && TransportError is null && StatusCode is >= 200 and < 300;
}
