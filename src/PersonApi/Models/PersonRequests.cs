namespace PersonApi.Models;

public sealed class CreatePersonRequest
{
    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public DateOnly? BirthDate { get; set; }
}

public sealed class UpdatePersonRequest
{
    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public DateOnly? BirthDate { get; set; }

    public bool HasAnyField =>
        LastName is not null || FirstName is not null || MiddleName is not null || BirthDate is not null;
}
