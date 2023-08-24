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
            => await Task.WhenAll(files.Select(async file => new FileIncludingContentMaybe<T>(file, await (await file.WhenReadyAsync()).ReadFromJson<T>(options)))).WithContent();

        public static async Task<FileIncludingContent<string>[]> ReadText(this IEnumerable<File> files)
            => await Task.WhenAll(files.Select(async file => new FileIncludingContentMaybe<string>(file, await (await file.WhenReadyAsync()).ReadText()))).WithContent();

        public static async Task<FileIncludingContent<string[]>[]> ReadLines(this IEnumerable<File> files)
            => await Task.WhenAll(files.Select(async file => new FileIncludingContentMaybe<string[]>(file, await (await file.WhenReadyAsync()).ReadLines()))).WithContent();

        public static async Task<FileIncludingContent<byte[]>[]> ReadBytes(this IEnumerable<File> files)
            => await Task.WhenAll(files.Select(async file => new FileIncludingContentMaybe<byte[]>(file, await (await file.WhenReadyAsync()).ReadBytes()))).WithContent();

        static async Task<FileIncludingContent<T>[]> WithContent<T>(this Task<FileIncludingContentMaybe<T>[]> files)
            => (await files).Where(f => f.ContentMaybe != null).Select(f => new FileIncludingContent<T>(f, f.ContentMaybe!)).ToArray();

        public static async IAsyncEnumerable<File[]> Live(this Directory directory, bool allDirectories = false, int timeout = 2500)
        {
            var fw = new IO.FileSystemWatcher(directory.Path)
            {
                IncludeSubdirectories = allDirectories
            };
            var tcs = new TaskCompletionSource<File[]>();
            int timedoutCounter = 0;

            _ = Task.Run(() =>
            {
                while (true)
                {
                    tcs.TrySetResult(directory.Filtered("*", allDirectories).ToArray());
                    var result = fw.WaitForChanged(IO.WatcherChangeTypes.All, timedoutCounter > 0 ? timeout / 10 : timeout);
                    timedoutCounter = result.TimedOut ? 5 : Math.Max(timedoutCounter - 1, 0);
                }
            });

            while (true)
            {
                yield return await tcs.Task;
                tcs = new();
            }
        }

        public record FileChanges(File[] All, File[]? Changed, File[]? Added, File[]? Deleted, bool ThereAreChanges, bool FirstRun);

        public static async IAsyncEnumerable<FileChanges> Changes(this IAsyncEnumerable<File[]> liveFiles)
        {
            Dictionary<File, DateTime> lastModified = new();

            var iterations = 0;

            await foreach (var files in liveFiles)
            {
                iterations++;

                var added = files.Except(lastModified.Keys).ToArray();
                var deleted = lastModified.Keys.Except(files).ToArray();
                var changed = files.Except(added).Where(f => lastModified[f] < f.Info.LastWriteTime).ToArray();

                lastModified = files.ToDictionaryValues(file => file.Info.LastWriteTime);

                yield return new(
                    All: files,
                    Changed: changed,
                    Added: added,
                    Deleted: deleted,
                    ThereAreChanges: (changed.Length | added.Length | deleted.Length) > 0,
                    FirstRun: iterations == 1
                );
            }
        }

        public static Dictionary<TKey, TValue> ToDictionaryValues<TKey, TValue>(this IEnumerable<TKey> keys, Func<TKey, TValue> valueGenerator) where TKey : notnull => keys.ToDictionary(key => key, valueGenerator);

        public static async IAsyncEnumerable<Dictionary<string, T>> FromJson<T>(this IAsyncEnumerable<FileChanges> liveFiles)
        {
            await foreach (var files in liveFiles)
            {
                if (files.ThereAreChanges || files.FirstRun)
                {
                    yield return await files.All.ToValidDictionary(f => f.NameWithoutExtension, async f => await f.ReadFromJson<T>());
                }
            }
        }

        public static Dictionary<TKey, TValue> ToValidDictionary<T0, TKey, TValue>(this IEnumerable<T0> enumerable, Func<T0, TKey> keySelector, Func<T0, TValue?> valueSelector) where TKey : notnull
        {
            Dictionary<TKey, TValue> d = new();
            foreach (var item in enumerable)
            {
                try
                {
                    var key = keySelector(item);
                    var value = valueSelector(item);
                    if (value != null)
                    {
                        d.Add(key, value);
                    }
                }
                catch (Exception)
                {
                }
            }
            return d;
        }

        public static async Task<Dictionary<TKey, TValue>> ToValidDictionary<T0, TKey, TValue>(this IEnumerable<T0> enumerable, Func<T0, TKey> keySelector, Func<T0, Task<TValue?>> valueSelector) where TKey : notnull
        {
            var tasks = enumerable.Select(async item => new { Key = item, Value = await valueSelector(item) }).ToArray();
            try
            {
                await Task.WhenAll(tasks);
            }
            catch
            {
            }
            return tasks
                .Where(t => t.Status == TaskStatus.RanToCompletion)
                .Select(t => t.Result)
                .Where(item => item.Value != null)
                .ToDictionary(item => keySelector(item.Key), item => item.Value!);
        }
    }
}