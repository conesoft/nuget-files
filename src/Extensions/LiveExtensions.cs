using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using IO = System.IO;

namespace Conesoft.Files;

[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class LiveExtensions
{
    private static readonly BoundedChannelOptions onlyLastMessage = new(1)
    {
        AllowSynchronousContinuations = true,
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = true
    };

    private static IAsyncEnumerable<bool> Live(string path, string filter, bool allDirectories = false, CancellationToken cancellation = default)
    {
        using var fw = new IO.FileSystemWatcher(path, filter)
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = allDirectories
        };
        var channel = Channel.CreateBounded<bool>(onlyLastMessage);

        async void NotifyOfChange(object? _ = null, IO.FileSystemEventArgs? e = null) => await channel.Writer.WriteAsync(true, cancellation);

        fw.Changed += NotifyOfChange;
        fw.Created += NotifyOfChange;
        fw.Deleted += NotifyOfChange;

        NotifyOfChange();

        return channel.Reader.ReadAllAsync(cancellation).EndOnCancel(cancellation, whenDone: () =>
        {
            fw.Changed -= NotifyOfChange;
            fw.Created -= NotifyOfChange;
            fw.Deleted -= NotifyOfChange;
        });
    }

    public static IAsyncEnumerable<bool> Live(this Directory directory, bool allDirectories = false, CancellationToken cancellation = default) => Live(directory.Path, "", allDirectories, cancellation);
    public static IAsyncEnumerable<bool> Live(this File file, CancellationToken cancellation = default) => Live(file.Parent.Path, file.Name, allDirectories: false, cancellation);

    public record EntryChanges(Entry[] All, Entry[]? Changed, Entry[]? Added, Entry[]? Deleted);

    public static async IAsyncEnumerable<EntryChanges> Changes(this Directory directory, bool allDirectories = false, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        Dictionary<Entry, DateTime> lastModified = [];

        await foreach (var _ in directory.Live(allDirectories, cancellation).EndOnCancel(cancellation))
        {
            var all = directory.FilteredArray("*", allDirectories);
            var added = all.Except(lastModified.Keys).ToArray();
            var deleted = lastModified.Keys.Except(all).ToArray();
            var changed = all.Except(added).Where(e => e.Info?.LastWriteTime switch
            {
                DateTime last => lastModified[e] < last,
                null => lastModified.ContainsKey(e)
            }).ToArray();

            lastModified = all.ToValidDictionaryValues(entry => entry.Info?.LastWriteTime);

            if (cancellation.IsCancellationRequested)
            {
                yield break;
            }

            yield return new(
                All: all,
                Changed: changed,
                Added: added,
                Deleted: deleted
            );
        }
    }
}