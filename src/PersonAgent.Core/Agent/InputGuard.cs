namespace PersonAgent.Agent;

public static class InputGuard
{
    public const int MaxLength = 500;

    public static string? RejectReason(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "пустой ввод";
        }

        if (input.Length > MaxLength)
        {
            return $"длина текста превышает {MaxLength} символов";
        }

        return null;
    }
}
