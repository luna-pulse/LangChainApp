namespace PersonAgent.Tools;

public static class ToolNames
{
    public const string CreatePerson = "CreatePerson";
    public const string GetPerson = "GetPerson";
    public const string UpdatePerson = "UpdatePerson";
    public const string ListPersons = "ListPersons";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        CreatePerson,
        GetPerson,
        UpdatePerson,
        ListPersons,
    };

    public static string ToReActName(string canonicalName) => canonicalName.ToLowerInvariant();
}
