using Conesoft.Tools;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
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

        public static IEnumerable<File> Files(this IEnumerable<Entry> entries) => entries.Select(e => e.AsFile).NotNull();
        public static IEnumerable<Directory> Directories(this IEnumerable<Entry> entries) => entries.Select(e => e.AsDirectory).NotNull();

        public static IAsyncEnumerable<Entry[]> Live(this Directory directory, bool allDirectories = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var fw = new IO.FileSystemWatcher(directory.Path)
            {
                IncludeSubdirectories = allDirectories
            };
            var channel = Channel.CreateBounded<Entry[]>(new BoundedChannelOptions(1)
            {
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true,
                SingleWriter = true
            });
            Task.Run(async () =>
            {
                while (cancellationToken.IsCancellationRequested == false)
                {
                    while (cancellationToken.IsCancellationRequested == false)
                    {
                        await channel.Writer.WriteAsync(directory.Filtered("*", allDirectories).ToArray(), cancellationToken);
                        var result = fw.WaitForChanged(IO.WatcherChangeTypes.All, 10000);
                        if (result.TimedOut == false)
                        {
                            break;
                        }
                    }
                }
                channel.Writer.Complete();
            }, cancellationToken);
            return channel.Reader.ReadAllAsync(cancellationToken);
        }


        public record EntryChanges(Entry[] All, Entry[]? Changed, Entry[]? Added, Entry[]? Deleted, bool ThereAreChanges, bool FirstRun);

        public static async IAsyncEnumerable<EntryChanges> Changes(this IAsyncEnumerable<Entry[]> liveEntries, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Dictionary<Entry, DateTime> lastModified = [];

            var iterations = 0;

            await foreach (var entries in liveEntries.WithCancellation(cancellationToken))
            {
                iterations++;

                var all = entries;
                var added = all.Except(lastModified.Keys).ToArray();
                var deleted = lastModified.Keys.Except(all).ToArray();
                var changed = all.Except(added).Where(e => e.Info?.LastWriteTime switch
                {
                    DateTime last => lastModified[e] < last,
                    null => lastModified.ContainsKey(e)
                }).ToArray();

                lastModified = all.ToValidDictionaryValues(entry => entry.Info?.LastWriteTime);

                if (cancellationToken.IsCancellationRequested)
                {
                    yield break;
                }

                yield return new(
                    All: all,
                    Changed: changed,
                    Added: added,
                    Deleted: deleted,
                    ThereAreChanges: (changed.Length | added.Length | deleted.Length) > 0,
                    FirstRun: iterations == 1
                );
            }
        }

        public static Dictionary<TKey, TValue> ToDictionaryValues<TKey, TValue>(this IEnumerable<TKey> keys, Func<TKey, TValue> valueGenerator) where TKey : notnull => keys.ToDictionary(key => key, valueGenerator);
        public static Dictionary<TKey, TValue> ToValidDictionaryValues<TKey, TValue>(this IEnumerable<TKey> keys, Func<TKey, TValue?> valueGenerator) where TKey : notnull where TValue : struct => keys.ToValidDictionary(key => key, valueGenerator);

        //public static async IAsyncEnumerable<Dictionary<string, T>> FromJson<T>(this IAsyncEnumerable<FileChanges> liveFiles)
        //{
        //    await foreach (var files in liveFiles)
        //    {
        //        if (files.ThereAreChanges || files.FirstRun)
        //        {
        //            yield return await files.All.ToValidDictionary(f => f.NameWithoutExtension, async f => await f.ReadFromJson<T>());
        //        }
        //    }
        //}

        public static Dictionary<TKey, TValue> ToValidDictionary<T0, TKey, TValue>(this IEnumerable<T0> enumerable, Func<T0, TKey> keySelector, Func<T0, TValue?> valueSelector) where TKey : notnull where TValue : struct
        {
            Dictionary<TKey, TValue> d = [];
            foreach (var item in enumerable)
            {
                try
                {
                    var key = keySelector(item);
                    var value = valueSelector(item);
                    if (value != null)
                    {
                        d.Add(key, value.Value);
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