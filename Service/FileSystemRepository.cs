namespace Dark;

public sealed class FileSystemRepository(string dataFolder, string lockFolder)
{
    const string Suffix = ".jsonl";

    public Task<string[]> GetIdsForAggregate(string aggregate)
    {
        var path = $"{dataFolder}{aggregate}/";
        var files = Directory.EnumerateFiles(path, '*' + Suffix);
        return Task.FromResult(files.Select(x => x[path.Length..^Suffix.Length]).ToArray());
    }

    public Task<bool> Has(Data data) => Task.FromResult(File.Exists(DataFilePath(data)));

    public async IAsyncEnumerable<string> Read(Data data, Condition cond, [EnumeratorCancellation] CancellationToken ct)
    {
        if (!await Has(data)) yield break;

        var groups = QueryParser.Parse(cond.Query ?? "");
        var version = 0;
        await foreach (var line in File.ReadLinesAsync(DataFilePath(data), ct))
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
        var dataFilePath = DataFilePath(data);
        var lockFilePath = LockFilePath(data);
        if (!File.Exists(AggregateLockFolderPath(data))) Directory.CreateDirectory(AggregateLockFolderPath(data));

        if (!File.Exists(AggregateDataFolderPath(data))) Directory.CreateDirectory(AggregateDataFolderPath(data));

        FileStream? fsLock = null;
        using FileStream fs = new(dataFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read, 4096, true);
        try
        {
            fsLock = new(lockFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None, 4096, true);
            System.Console.WriteLine("Acquire lock on " + lockFilePath);
            fsLock.Lock(0, 0);
            System.Console.WriteLine("Lock acquired on " + lockFilePath);
            await EnsureNoConcurrency(data, condition, ct);

            var currentVersion = fs.LastVersion() + 1;
            fs.Seek(0, SeekOrigin.End);

            var processedEntries = entries.Select(x => x.EndsWith('}') ? $"{x[..^1]},\"$ver\":{currentVersion++}}}" : x);

            await fs.WriteAsync(Encoding.UTF8.GetBytes(processedEntries.Join('\n') + '\n'), ct);
            await fs.FlushAsync(CancellationToken.None);
        }
        catch (IOException e) when (e.Message.Contains("is being used by another process"))
        {
            Console.WriteLine($"File {lockFilePath} is locked, retrying...");
            fs.Dispose();
            fsLock?.Unlock(0, 0);
            fsLock?.Dispose();
            await Task.Delay(100, ct);
            await Append(data, entries, condition, ct);
        }
        finally
        {
            fs.Dispose();
            fsLock?.Unlock(0, 0);
            fsLock?.Dispose();
        }
    }

    public async Task Overwrite(Data data, string[] entries, Condition condition, CancellationToken ct)
    {
        await EnsureNoConcurrency(data, condition, ct);

        var tempFile = Path.GetTempFileName();
        await File.WriteAllLinesAsync(tempFile, entries, ct);
        Directory.CreateDirectory(dataFolder + data.Aggregate);
        ReplaceFile(tempFile, DataFilePath(data));
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
        => await Has(data) ? (await File.ReadAllLinesAsync(DataFilePath(data), ct)).Length : 0;

    string AggregatePathSegment(Data data) => $"{data.Aggregate}/";
    string AggregateDataFolderPath(Data data) => $"{dataFolder}{AggregatePathSegment(data)}";
    string AggregateLockFolderPath(Data data) => $"{lockFolder}{AggregatePathSegment(data)}";
    string IdPathSegment(Data data) => $"{data.Id}{Suffix}";
    string DataFilePath(Data data) => $"{AggregateDataFolderPath(data)}{IdPathSegment(data)}";
    string LockFilePath(Data data) => $"{AggregateLockFolderPath(data)}{IdPathSegment(data)}.lock";

    static void ReplaceFile(string source, string dest)
    {
        try { File.Move(source, dest, overwrite: true); }
        catch (Exception) { throw; }
        finally { File.Delete(source); }
    }
}
