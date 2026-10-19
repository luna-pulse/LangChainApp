using PersonApi.Models;

namespace PersonApi.Store;

public interface IPersonStore
{
    Person Create(string lastName, string firstName, string? middleName, DateOnly? birthDate);

    Person? Get(int id);

    Person? Update(int id, string? lastName, string? firstName, string? middleName, DateOnly? birthDate, bool hasBirthDate);

    IReadOnlyList<Person> List();
}
