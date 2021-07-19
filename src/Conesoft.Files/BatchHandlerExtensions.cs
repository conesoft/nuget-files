using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IO = System.IO;

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

        public static async IAsyncEnumerable<File[]> Live(this Directory directory, bool allDirectories = false, int timeout = 2500)
        {
            var fw = new IO.FileSystemWatcher(directory.Path)
            {
                IncludeSubdirectories = allDirectories
            };
            var tcs = new TaskCompletionSource<File[]>();
            bool timedout = false;

            _ = Task.Run(() =>
            {
                while (true)
                {
                    tcs.TrySetResult(directory.Filtered("*", allDirectories).ToArray());
                    var result = fw.WaitForChanged(IO.WatcherChangeTypes.All, timedout ? timeout : timeout / 10);
                    timedout = result.TimedOut;
                }
            });

            while (true)
            {
                yield return await tcs.Task;
                tcs = new();
            }
        }

        public static async IAsyncEnumerable<(File[] All, File[]? Changed, File[]? Added, File[]? Deleted, bool ThereAreChanges)> Changes(this IAsyncEnumerable<File[]> liveFiles)
        {
            Dictionary<File, DateTime> lastModified = new();

            await foreach (var files in liveFiles)
            {
                var added = files.Except(lastModified.Keys).ToArray();
                var deleted = lastModified.Keys.Except(files).ToArray();
                var changed = files.Except(added).Where(f => lastModified[f] < f.Info.LastWriteTime).ToArray();

                lastModified = files.ToDictionaryValues(file => file.Info.LastWriteTime);

                yield return (
                    All: files,
                    Changed: changed,
                    Added: added,
                    Deleted: deleted,
                    ThereAreChanges: (changed.Length | added.Length | deleted.Length) > 0
                );
            }
        }

        public static Dictionary<TKey, TValue> ToDictionaryValues<TKey, TValue>(this IEnumerable<TKey> keys, Func<TKey, TValue> valueGenerator) where TKey : notnull => keys.ToDictionary(key => key, valueGenerator);

        public static async IAsyncEnumerable<Dictionary<string, T>> FromJson<T>(this IAsyncEnumerable<(File[] All, File[]? Changed, File[]? Added, File[]? Deleted, bool ThereAreChanges)> liveFiles)
        {
            await foreach (var files in liveFiles)
            {
                if (files.ThereAreChanges)
                {
                    var dictionary = new Dictionary<string, T>();
                    foreach (var file in files.All)
                    {
                        try
                        {
                            var name = file.NameWithoutExtension;
                            if(await file.ReadFromJson<T>() is T value)
                            {
                                dictionary.Add(name, value);
                            }
                        }
                        catch (Exception)
                        {
                        }
                    }
                    yield return dictionary;
                }
            }
        }
    }
}