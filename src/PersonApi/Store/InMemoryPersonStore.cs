using PersonApi.Models;

namespace PersonApi.Store;

public sealed class InMemoryPersonStore : IPersonStore
{
    private readonly Lock _lock = new();
    private readonly Dictionary<int, Person> _people = [];
    private int _nextId = 1;

    public Person Create(string lastName, string firstName, string? middleName, DateOnly? birthDate)
    {
        lock (_lock)
        {
            var person = new Person(_nextId++, lastName, firstName, middleName, birthDate);
            _people.Add(person.Id, person);
            return person;
        }
    }

    public Person? Get(int id)
    {
        lock (_lock)
        {
            return _people.TryGetValue(id, out var person) ? person : null;
        }
    }

    public Person? Update(
        int id,
        string? lastName,
        string? firstName,
        string? middleName,
        DateOnly? birthDate,
        bool hasBirthDate)
    {
        lock (_lock)
        {
            if (!_people.TryGetValue(id, out var current))
            {
                return null;
            }

            var updated = current with
            {
                LastName = lastName ?? current.LastName,
                FirstName = firstName ?? current.FirstName,
                MiddleName = middleName ?? current.MiddleName,
                BirthDate = hasBirthDate ? birthDate : current.BirthDate,
            };
            _people[id] = updated;
            return updated;
        }
    }

    public IReadOnlyList<Person> List()
    {
        lock (_lock)
        {
            return _people.Values.OrderBy(person => person.Id).ToArray();
        }
    }
}
