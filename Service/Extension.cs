namespace Dark;

public static class Extension
{
    public static bool IsFilled(this string? value) => !string.IsNullOrWhiteSpace(value);

    public static bool IsNotFilled(this string? value) => string.IsNullOrWhiteSpace(value);

    public static string Join(this IEnumerable<string> source, char separator) => string.Join(separator, source);

    public static async Task<string[]> AsStringArray(this Stream body, CancellationToken ct)
        => (await new StreamReader(body).ReadToEndAsync(ct)).Split('\n');
}
