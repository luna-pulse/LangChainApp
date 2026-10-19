namespace PersonApi.Models;

public sealed record ErrorResponse(
    int Status,
    string Title,
    IReadOnlyDictionary<string, string[]> Errors);
