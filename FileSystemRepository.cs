using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Dark
{
    public class FileSystemRepository
    {
        private const string suffix = ".jsonl";
        private readonly string folder;

        public FileSystemRepository(string folder) => this.folder = folder;

        public IImmutableList<string> getIdsForAggregate(string aggregate)
        {
            var files = Directory.EnumerateFiles(folder + aggregate, "*" + suffix);
            return files.Select(x => x.Remove(x.Length - suffix.Length).Remove(0, folder.Length)).ToImmutableList();
        }

        public bool has(string aggregate, string id) => File.Exists(getFilePath(aggregate, id));

        public IImmutableList<string> read(string aggregate, string id)
            => File.ReadLines(getFilePath(aggregate, id)).ToImmutableList();

        public IImmutableList<string> readOrEmpty(string aggregate, string id)
            => has(aggregate, id) ? read(aggregate, id) : ImmutableList.Create<string>();

        public async Task append(string aggregate, string id, IImmutableList<string> entries, int expectedVersion)
        {
            if (currentVersion(aggregate, id) != expectedVersion) {
                throw new ConcurrencyException();
            }
            Directory.CreateDirectory(folder + aggregate);
            await File.AppendAllLinesAsync(getFilePath(aggregate, id), entries);
        }

        public async Task overwrite(string aggregate, string id, IImmutableList<string> entries, int expectedVersion)
        {
            if (currentVersion(aggregate, id) != expectedVersion) {
                throw new ConcurrencyException();
            }
            Directory.CreateDirectory(folder + aggregate);
            await File.WriteAllLinesAsync(getFilePath(aggregate, id), entries);
        }

        private int currentVersion(string aggregate, string id)
            => has(aggregate, id) ? File.ReadLines(getFilePath(aggregate, id)).Count() : 0;
        private string getFilePath(string aggregate, string id) => $"{folder}{aggregate}/{id}{suffix}";
    }
}
