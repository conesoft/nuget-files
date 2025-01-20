using Conesoft.Files.Try;
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

    private static IAsyncEnumerable<bool> Live(string path, string? filter = null, bool allDirectories = false, CancellationToken cancellation = default)
    {
        var fw = new IO.FileSystemWatcher(path, filter ?? "*")
        {
            EnableRaisingEvents = true,
            IncludeSubdirectories = allDirectories,
        };
        var channel = Channel.CreateBounded<bool>(onlyLastMessage);

        void NotifyOfChange(object? _ = null, IO.FileSystemEventArgs? e = null) => Safe.TryAsync(async () => await channel.Writer.WriteAsync(true, cancellation));

        fw.Changed += NotifyOfChange;
        fw.Created += NotifyOfChange;
        fw.Deleted += NotifyOfChange;

        NotifyOfChange();

        return channel.Reader.ReadAllAsync(cancellation).EndOnCancel(cancellation, whenDone: () =>
        {
            fw.Changed -= NotifyOfChange;
            fw.Created -= NotifyOfChange;
            fw.Deleted -= NotifyOfChange;
            fw.Dispose();
        });
    }

    public static IAsyncEnumerable<bool> Live(this Directory directory, bool allDirectories = false, CancellationToken cancellation = default) => Live(directory.Path, allDirectories: allDirectories, cancellation: cancellation);
    public static IAsyncEnumerable<bool> Live(this File file, CancellationToken cancellation = default) => Live(file.Parent.Path, file.Name, allDirectories: false, cancellation);

    public record FileChanges(File[] All, File[] Changed, File[] Added, File[] Deleted);

    public static async IAsyncEnumerable<FileChanges> Changes(this Directory directory, bool allDirectories = false, [EnumeratorCancellation] CancellationToken cancellation = default)
    {
        Dictionary<File, DateTime> lastModified = [];

        await foreach (var _ in directory.Live(allDirectories, cancellation).EndOnCancel(cancellation))
        {
            var all = directory.FilteredFiles("*", allDirectories).ToArray();
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