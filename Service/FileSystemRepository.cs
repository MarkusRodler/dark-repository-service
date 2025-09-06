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

    public Task<bool> Has(Data data) => Task.FromResult(File.Exists(GetFilePath(data)));

    public async IAsyncEnumerable<string> Read(Data data, Condition cond, [EnumeratorCancellation] CancellationToken ct)
    {
        if (!await Has(data)) yield break;

        var groups = QueryParser.Parse(cond.Query ?? "");
        var version = 0;
        await foreach (var line in File.ReadLinesAsync(GetFilePath(data), ct))
        {
            version++;
            if (version <= cond.Version) continue;

            if (cond.Query.IsNotFilled()
                || JsonSerializer.Deserialize(line, JsonContext.Default.Event) is { } e && e.Matches(groups))
            {
                yield return line;
            }
        }
    }

    public async Task Append(Data data, IEnumerable<string> entries, Condition condition, CancellationToken ct)
    {
        var destFile = GetFilePath(data);
        Directory.CreateDirectory(folder + data.Aggregate);

        using FileStream fs = new(destFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096, true);
        try
        {
            fs.Lock(0, 0);
            await EnsureNoConcurrency(data, condition, ct);

            var currentVersion = fs.LastVersion() + 1;
            fs.Seek(0, SeekOrigin.End);

            var processedEntries = entries.Select(x => x.EndsWith('}') ? $"{x[..^1]},\"$ver\":{currentVersion++}}}" : x);

            await fs.WriteAsync(Encoding.UTF8.GetBytes(processedEntries.Join('\n') + '\n'), ct);
            await fs.FlushAsync(CancellationToken.None);
        }
        catch (IOException e) when (e.Message.Contains("is being used by another process"))
        {
            fs.Unlock(0, 0);
            fs.Dispose();
            await Task.Delay(100, ct);
            await Append(data, entries, condition, ct);
        }
        finally
        {
            fs.Unlock(0, 0);
            fs.Dispose();
        }
    }

    public async Task Overwrite(Data data, string[] entries, Condition condition, CancellationToken ct)
    {
        await EnsureNoConcurrency(data, condition, ct);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tempFile, entries, ct);
        Directory.CreateDirectory(folder + data.Aggregate);
        ReplaceFile(tempFile, GetFilePath(data));
    }

    async Task EnsureNoConcurrency(Data data, Condition condition, CancellationToken ct)
    {
        if (condition.Query.IsFilled())
        {
            await foreach (var _ in Read(data, condition, ct)) throw new ConcurrencyException();
        }
        else if (condition.Version > -1 && await CurrentVersion(data, ct) != condition.Version)
        {
            throw new ConcurrencyException();
        }
    }

    async Task<int> CurrentVersion(Data data, CancellationToken ct)
        => await Has(data) ? (await File.ReadAllLinesAsync(GetFilePath(data), ct)).Length : 0;

    string GetFilePath(Data data) => $"{folder}{data.Aggregate}/{data.Id}{Suffix}";

    static void ReplaceFile(string source, string dest)
    {
        try { File.Move(source, dest, overwrite: true); }
        catch (Exception) { throw; }
        finally { File.Delete(source); }
    }
}
