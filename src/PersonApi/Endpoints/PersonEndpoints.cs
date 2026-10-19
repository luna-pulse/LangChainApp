using PersonApi.Models;
using PersonApi.Store;
using PersonApi.Validation;

namespace PersonApi.Endpoints;

public static class PersonEndpoints
{
    public static IEndpointRouteBuilder MapPersonEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/persons", Create)
            .WithName("CreatePerson")
            .WithTags("Persons")
            .Produces<Person>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest);

        app.MapGet("/persons/{id:int}", Get)
            .WithName("GetPerson")
            .WithTags("Persons")
            .Produces<Person>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        app.MapPut("/persons/{id:int}", Update)
            .WithName("UpdatePerson")
            .WithTags("Persons")
            .Produces<Person>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        app.MapGet("/persons", List)
            .WithName("ListPersons")
            .WithTags("Persons")
            .Produces<PersonListResponse>(StatusCodes.Status200OK);

        return app;
    }

    private static IResult Create(CreatePersonRequest? request, IPersonStore store, ILogger<Program> logger)
    {
        if (request is null)
        {
            return PersonResults.BadRequest(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["body"] = ["Тело запроса обязательно"],
            });
        }

        var errors = PersonValidator.ValidateCreate(request);
        if (errors.Count > 0)
        {
            return PersonResults.BadRequest(errors);
        }

        var person = store.Create(
            request.LastName!.Trim(),
            request.FirstName!.Trim(),
            string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            request.BirthDate);
        logger.LogInformation("CreatePerson: создано физлицо {Id}", person.Id);
        return Results.Created($"/persons/{person.Id}", person);
    }

    private static IResult Get(int id, IPersonStore store, ILogger<Program> logger)
    {
        if (id <= 0)
        {
            return PersonResults.BadRequest(PersonValidator.InvalidId());
        }

        var person = store.Get(id);
        if (person is null)
        {
            logger.LogInformation("GetPerson: физлицо {Id} не найдено", id);
            return PersonResults.NotFound();
        }

        logger.LogInformation("GetPerson: найдено физлицо {Id}", id);
        return Results.Ok(person);
    }

    private static IResult Update(int id, UpdatePersonRequest? request, IPersonStore store, ILogger<Program> logger)
    {
        if (id <= 0)
        {
            return PersonResults.BadRequest(PersonValidator.InvalidId());
        }

        if (request is null)
        {
            return PersonResults.BadRequest(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["body"] = ["Тело запроса обязательно"],
            });
        }

        var errors = PersonValidator.ValidateUpdate(request);
        if (errors.Count > 0)
        {
            return PersonResults.BadRequest(errors);
        }

        var updated = store.Update(
            id,
            string.IsNullOrWhiteSpace(request.LastName) ? null : request.LastName.Trim(),
            string.IsNullOrWhiteSpace(request.FirstName) ? null : request.FirstName.Trim(),
            string.IsNullOrWhiteSpace(request.MiddleName) ? null : request.MiddleName.Trim(),
            request.BirthDate,
            request.BirthDate is not null);
        if (updated is null)
        {
            logger.LogInformation("UpdatePerson: физлицо {Id} не найдено", id);
            return PersonResults.NotFound();
        }

        logger.LogInformation("UpdatePerson: изменено физлицо {Id}", id);
        return Results.Ok(updated);
    }

    private static IResult List(IPersonStore store, ILogger<Program> logger)
    {
        var items = store.List();
        logger.LogInformation("ListPersons: возвращено {Count}", items.Count);
        return Results.Ok(new PersonListResponse(items, items.Count));
    }
}

internal static class PersonResults
{
    public static IResult BadRequest(IReadOnlyDictionary<string, string[]> errors) =>
        Results.Json(new ErrorResponse(StatusCodes.Status400BadRequest, "Некорректные данные", errors), statusCode: StatusCodes.Status400BadRequest);

    public static IResult NotFound() =>
        Results.Json(
            new ErrorResponse(StatusCodes.Status404NotFound, "Физлицо не найдено", new Dictionary<string, string[]>()),
            statusCode: StatusCodes.Status404NotFound);
}
