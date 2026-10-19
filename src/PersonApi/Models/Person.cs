namespace PersonApi.Models;

public sealed record Person(
    int Id,
    string LastName,
    string FirstName,
    string? MiddleName,
    DateOnly? BirthDate);

public sealed record PersonListResponse(IReadOnlyList<Person> Items, int Count);
