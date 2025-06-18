namespace Dark;

public sealed class FileSystemRepository(string folder)
{
    const string Suffix = ".jsonl";
    readonly string folder = folder;

    public Task<string[]> GetIdsForAggregate(string aggregate)
    {
        var path = $"{folder}{aggregate}/";
        var files = Directory.EnumerateFiles(path, '*' + Suffix);
        return Task.FromResult(files.Select(x => x[path.Length..^Suffix.Length]).ToArray());
    }

    public Task<bool> Has(string aggregate, string id) => Task.FromResult(File.Exists(GetFilePath(aggregate, id)));

    public async Task<string[]> Read(string aggregate, string id)
        => await File.ReadAllLinesAsync(GetFilePath(aggregate, id));

    public async Task Append(string aggregate, string id, string[] entries, int expectedVersion)
    {
        await EnsureNoConcurrency(aggregate, id, expectedVersion);

        var tempFile = Path.GetTempFileName();
        var destFile = GetFilePath(aggregate, id);
        Directory.CreateDirectory(folder + aggregate);
        if (File.Exists(destFile)) File.Copy(destFile, tempFile, overwrite: true);

        await File.AppendAllLinesAsync(tempFile, entries);
        ReplaceFile(tempFile, destFile);
    }

    public async Task Overwrite(string aggregate, string id, string[] entries, int expectedVersion)
    {
        await EnsureNoConcurrency(aggregate, id, expectedVersion);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tempFile, entries);
        Directory.CreateDirectory(folder + aggregate);
        ReplaceFile(tempFile, GetFilePath(aggregate, id));
    }

    async Task EnsureNoConcurrency(string aggregate, string id, int expectedVersion)
    {
        if (expectedVersion > -1 && await CurrentVersion(aggregate, id) != expectedVersion)
        {
            throw new ConcurrencyException();
        }
    }

    async Task<int> CurrentVersion(string aggregate, string id)
        => await Has(aggregate, id) ? (await File.ReadAllLinesAsync(GetFilePath(aggregate, id))).Length : 0;

    string GetFilePath(string aggregate, string id) => $"{folder}{aggregate}/{id}{Suffix}";

    static void ReplaceFile(string source, string dest)
    {
        try { File.Move(source, dest, overwrite: true); }
        catch (Exception) { throw; }
        finally { File.Delete(source); }
    }
}
