using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IO = System.IO;

namespace Conesoft.Files;

[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class BatchHandlerExtensions
{
    public static async Task<FileIncludingContent<T>[]> ReadFromJson<T>(this IEnumerable<File> files, JsonSerializerOptions? options = null)
        => await Task.WhenAll(files.Select(async file => new FileIncludingContentMaybe<T>(file, await (await file.WhenReadyAsync()).ReadFromJson<T>(options)))).WithContent();

    public static async Task<FileIncludingContent<string>[]> ReadText(this IEnumerable<File> files)
        => await Task.WhenAll(files.Select(async file => new FileIncludingContentMaybe<string>(file, await (await file.WhenReadyAsync()).ReadText()))).WithContent();

    public static async Task<FileIncludingContent<string[]>[]> ReadLines(this IEnumerable<File> files)
        => await Task.WhenAll(files.Select(async file => new FileIncludingContentMaybe<string[]>(file, await (await file.WhenReadyAsync()).ReadLines()))).WithContent();

    public static async Task<FileIncludingContent<byte[]>[]> ReadBytes(this IEnumerable<File> files)
        => await Task.WhenAll(files.Select(async file => new FileIncludingContentMaybe<byte[]>(file, await (await file.WhenReadyAsync()).ReadBytes()))).WithContent();

    static async Task<FileIncludingContent<T>[]> WithContent<T>(this Task<FileIncludingContentMaybe<T>[]> files)
        => (await files).Where(f => f.ContentMaybe != null).Select(f => new FileIncludingContent<T>(f, f.ContentMaybe!)).ToArray();

    public static IEnumerable<File> Files(this IEnumerable<Entry> entries) => entries.Select(e => e.AsFile).NotNull();
    public static IEnumerable<Directory> Directories(this IEnumerable<Entry> entries) => entries.Select(e => e.AsDirectory).NotNull();

    public static IEnumerable<File> Visible(this IEnumerable<File> files) => files.Where(f => f.Info.Attributes.HasFlag(IO.FileAttributes.Hidden) == false);
    public static IEnumerable<Directory> Visible(this IEnumerable<Directory> directories) => directories.Where(f => f.Info.Attributes.HasFlag(IO.FileAttributes.Hidden) == false);
}