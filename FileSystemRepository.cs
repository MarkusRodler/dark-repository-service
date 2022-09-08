namespace Dark;

public class FileSystemRepository
{
    const string Suffix = ".eventlog.jsonl";
    readonly string folder;

    public FileSystemRepository(string folder) => this.folder = folder;

    public Task<IImmutableList<string>> GetIdsForAggregate(string aggregate)
    {
        var path = $"{folder}{aggregate}/";
        var files = Directory.EnumerateFiles(path, "*" + Suffix);
        var list = files.Select(x => x[path.Length..^Suffix.Length]).ToImmutableList();
        return Task.FromResult<IImmutableList<string>>(list);
    }

    public Task<bool> Has(string aggregate, string id) => Task.FromResult(File.Exists(GetFilePath(aggregate, id)));

    public async Task<IImmutableList<string>> Read(string aggregate, string id)
        => (await File.ReadAllLinesAsync(GetFilePath(aggregate, id))).ToImmutableList();

    public async Task Append(string aggregate, string id, IImmutableList<string> entries, int expectedVersion)
    {
        await EnsureNoConcurrency(aggregate, id, expectedVersion);
        Directory.CreateDirectory(folder + aggregate);
        await File.AppendAllLinesAsync(GetFilePath(aggregate, id), entries);
    }

    public async Task Overwrite(string aggregate, string id, IImmutableList<string> entries, int expectedVersion)
    {
        await EnsureNoConcurrency(aggregate, id, expectedVersion);
        Directory.CreateDirectory(folder + aggregate);
        await File.WriteAllLinesAsync(GetFilePath(aggregate, id), entries);
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
}
