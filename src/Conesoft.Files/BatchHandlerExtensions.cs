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
    }
}
