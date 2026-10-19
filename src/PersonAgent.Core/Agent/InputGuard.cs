namespace PersonAgent.Agent;

public static class InputGuard
{
    public const int MinLength = 3;

    public const int MaxLength = 500;

    public const string LengthMessage = "Укажите текст от 3 до 500 символов";

    public static string? RejectReason(string? input)
    {
        if (input is null || input.Trim().Length < MinLength)
        {
            return LengthMessage;
        }

        if (input.Length > MaxLength)
        {
            return $"длина текста превышает {MaxLength} символов";
        }

        return null;
    }
}
