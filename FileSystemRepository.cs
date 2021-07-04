using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dark
{
    public class FileSystemRepository
    {
        const string Suffix = ".jsonl";
        readonly string folder;

        public FileSystemRepository(string folder) => this.folder = folder;

        public IImmutableList<string> GetIdsForAggregate(string aggregate)
        {
            var path = $"{folder}{aggregate}/";
            var files = Directory.EnumerateFiles(path, "*" + Suffix);
            return files.Select(x => x.Remove(x.Length - Suffix.Length).Remove(0, path.Length)).ToImmutableList();
        }

        public bool Has(string aggregate, string id) => File.Exists(GetFilePath(aggregate, id));

        public IImmutableList<string> Read(string aggregate, string id)
            => File.ReadLines(GetFilePath(aggregate, id)).ToImmutableList();

        public async Task Append(string aggregate, string id, IImmutableList<string> entries, int expectedVersion)
        {
            if (expectedVersion > -1 && CurrentVersion(aggregate, id) != expectedVersion)
            {
                throw new ConcurrencyException();
            }
            Directory.CreateDirectory(folder + aggregate);
            await File.AppendAllLinesAsync(GetFilePath(aggregate, id), entries);
        }

        public async Task Overwrite(string aggregate, string id, IImmutableList<string> entries, int expectedVersion)
        {
            if (expectedVersion > -1 && CurrentVersion(aggregate, id) != expectedVersion)
            {
                throw new ConcurrencyException();
            }
            Directory.CreateDirectory(folder + aggregate);
            await File.WriteAllLinesAsync(GetFilePath(aggregate, id), entries);
        }

        int CurrentVersion(string aggregate, string id)
            => Has(aggregate, id) ? File.ReadLines(GetFilePath(aggregate, id)).Count() : 0;
        string GetFilePath(string aggregate, string id) => $"{folder}{aggregate}/{id}{Suffix}";
    }
}
