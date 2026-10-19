namespace PersonAgent.Cli;

public static class ConsoleInput
{
    public static string? Read(string[] args, Func<string?> readLine, Action<string> writeLine)
    {
        if (args.Length > 0)
        {
            return string.Join(' ', args);
        }

        writeLine("Введите запрос (от 3 до 500 символов):");
        return readLine();
    }
}
