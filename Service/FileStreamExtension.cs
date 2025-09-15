namespace Dark;

public static class FileStreamExtension
{
    const string VerField = "\"Version\":";

    public static int LastVersion(this FileStream fs)
    {
        var readSize = 32;
        if (fs.Length < readSize) return fs.LineCount();

        var buffer = new byte[readSize];

        fs.Seek(-readSize, SeekOrigin.End);
        fs.ReadExactly(buffer, 0, readSize);

        var input = Encoding.UTF8.GetString(buffer);

        var start = input.IndexOf(VerField);
        if (start < 0) return fs.LineCount();

        start += VerField.Length;
        var end = input.IndexOfAny([',', '}', ' '], start);
        if (end == -1) end = input.Length;
        return int.Parse(input[start..end]);
    }

    public static int LineCount(this FileStream fs)
    {
        fs.Seek(0, SeekOrigin.Begin);
        var count = 0;
        int b;
        while ((b = fs.ReadByte()) != -1) if (b == '\n') count++;

        return count;
    }
}
