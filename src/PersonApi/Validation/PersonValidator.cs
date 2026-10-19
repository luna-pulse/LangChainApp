using PersonApi.Models;

namespace PersonApi.Validation;

public static class PersonValidator
{
    public const int MaxNameLength = 100;

    public static Dictionary<string, string[]> ValidateCreate(CreatePersonRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        AddName(errors, "lastName", request.LastName, required: true);
        AddName(errors, "firstName", request.FirstName, required: true);
        AddName(errors, "middleName", request.MiddleName, required: false);
        AddBirthDate(errors, request.BirthDate);
        return errors;
    }

    public static Dictionary<string, string[]> ValidateUpdate(UpdatePersonRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (!request.HasAnyField)
        {
            errors["body"] = ["Укажите хотя бы одно поле: lastName, firstName, middleName или birthDate"];
            return errors;
        }

        AddName(errors, "lastName", request.LastName, required: false);
        AddName(errors, "firstName", request.FirstName, required: false);
        AddName(errors, "middleName", request.MiddleName, required: false);
        AddBirthDate(errors, request.BirthDate);
        return errors;
    }

    public static Dictionary<string, string[]> InvalidId() =>
        new(StringComparer.Ordinal)
        {
            ["id"] = ["Идентификатор должен быть положительным числом"],
        };

    private static void AddName(Dictionary<string, string[]> errors, string field, string? value, bool required)
    {
        if (value is null)
        {
            if (required)
            {
                errors[field] = [RequiredMessage(field)];
            }

            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            errors[field] = [RequiredMessage(field)];
            return;
        }

        if (trimmed.Length > MaxNameLength)
        {
            errors[field] = [$"Поле не длиннее {MaxNameLength} символов"];
            return;
        }

        if (!IsPersonName(trimmed))
        {
            errors[field] = ["Допустимы буквы, пробел, дефис и апостроф"];
        }
    }

    private static void AddBirthDate(Dictionary<string, string[]> errors, DateOnly? birthDate)
    {
        if (birthDate is null)
        {
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (birthDate < new DateOnly(1900, 1, 1) || birthDate > today)
        {
            errors["birthDate"] = ["Дата рождения должна быть в диапазоне 1900-01-01..сегодня"];
        }
    }

    private static string RequiredMessage(string field) => field switch
    {
        "lastName" => "Укажите фамилию",
        "firstName" => "Укажите имя",
        "middleName" => "Отчество не может быть пустым",
        _ => "Поле обязательно",
    };

    private static bool IsPersonName(string value)
    {
        if (!char.IsLetter(value[0]))
        {
            return false;
        }

        foreach (var ch in value)
        {
            if (char.IsLetter(ch) || ch is ' ' or '-' or '\'')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
