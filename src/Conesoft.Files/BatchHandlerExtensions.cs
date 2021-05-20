using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Conesoft.Files
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class BatchHandlerExtensions
    {
        public static async Task<FileIncludingContent<T>[]> ReadFromJson<T>(this IEnumerable<File> files, JsonSerializerOptions? options = null)
            => await Task.WhenAll(files.Select(async file => new FileIncludingContent<T>(file, await file.ReadFromJson<T>(options))));

        public static async Task<FileIncludingContent<string>[]> ReadText(this IEnumerable<File> files)
            => await Task.WhenAll(files.Select(async file => new FileIncludingContent<string>(file, await file.ReadText())));

        public static async Task<FileIncludingContent<string[]>[]> ReadLines(this IEnumerable<File> files)
            => await Task.WhenAll(files.Select(async file => new FileIncludingContent<string[]>(file, await file.ReadLines())));

        public static async Task<FileIncludingContent<byte[]>[]> ReadBytes(this IEnumerable<File> files)
            => await Task.WhenAll(files.Select(async file => new FileIncludingContent<byte[]>(file, await file.ReadBytes())));

        public static async IAsyncEnumerable<(File[] All, File[] Changed, File[] Added, File[] Deleted)> Changes(this IAsyncEnumerable<File[]> liveFiles)
        {
            Dictionary<File, DateTime> lastModified = new();

            await foreach (var files in liveFiles)
            {
                var added = files.Except(lastModified.Keys).ToArray();
                var deleted = lastModified.Keys.Except(files).ToArray();

                var changed = files.Except(added).Where(f => lastModified[f] < f.Info.LastWriteTime).ToArray();

                foreach (var file in changed.Concat(added))
                {
                    lastModified[file] = file.Info.LastWriteTime;
                }

                if ((changed.Length | added.Length | deleted.Length) > 0)
                {
                    yield return (
                        All: files,
                        Changed: changed.ToArray(),
                        Added: added.ToArray(),
                        Deleted: deleted
                    );
                }
            }
        }
    }
}
