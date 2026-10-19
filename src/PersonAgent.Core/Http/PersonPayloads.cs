namespace PersonAgent.Http;

public sealed class CreatePersonPayload
{
    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? BirthDate { get; set; }
}

public sealed class UpdatePersonPayload
{
    public string? LastName { get; set; }

    public string? FirstName { get; set; }

    public string? MiddleName { get; set; }

    public string? BirthDate { get; set; }
}
