using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

using CTS = System.Threading.CancellationTokenSource;

namespace Conesoft.Files;

[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class LiveExtensions
{
    public static CTS Live(this IEnumerable<Entry> entries, Action action, bool all = false, CTS? previous = default) => entries.Live(WrapAction(action), all, previous);
    public static CTS Live(this Entry entry, Action action, bool all = false, CTS? previous = default) => entry.Live(WrapAction(action), all, previous);

    public static CTS Live(this IEnumerable<Entry> entries, Func<Task> action, bool all = false, CTS? previous = default)
    {
        var tokens = entries.Distinct().Select(entry => entry.Live(action, all, previous)).Select(cts => cts.Token).ToArray();
        return previous ?? CTS.CreateLinkedTokenSource(tokens);
    }

    public static CTS Live(this Entry entry, Func<Task> action, bool all = false, CTS? previous = default)
    {
        var cts = previous ?? new CTS();

        var path = entry.IsDirectory ? entry.Path : entry.Parent.Path;
        var filter = entry.IsDirectory ? "*" : entry.Name;

        var fw = new IO.FileSystemWatcher(path, filter)
        {
            EnableRaisingEvents = true,
            NotifyFilter = IO.NotifyFilters.Attributes | IO.NotifyFilters.LastWrite | IO.NotifyFilters.FileName | IO.NotifyFilters.DirectoryName,
            IncludeSubdirectories = all,
        };

        async void NotifyOfChange(object? _ = null, IO.FileSystemEventArgs? e = null) => await Safe.TryAsync(action);

        fw.Created += NotifyOfChange;
        fw.Renamed += NotifyOfChange;
        fw.Changed += NotifyOfChange;
        fw.Deleted += NotifyOfChange;

        cts.Token.Register(() =>
        {
            fw.Created -= NotifyOfChange;
            fw.Renamed -= NotifyOfChange;
            fw.Changed -= NotifyOfChange;
            fw.Deleted -= NotifyOfChange;
            fw.Dispose();
        });

        NotifyOfChange();

        return cts;
    }

    private static Func<Task> WrapAction(Action action) => () =>
    {
        action();
        return Task.CompletedTask;
    };

    #region Obsolete
    [Obsolete]
    private static readonly BoundedChannelOptions onlyLastMessage = new(1)
    {
        AllowSynchronousContinuations = true,
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = true
    };

    [Obsolete]
    private static IAsyncEnumerable<bool> Live(string path, string? filter = null, bool allDirectories = false, CancellationToken cancellation = default)
    {
        var fw = new IO.FileSystemWatcher(path, filter ?? "*")
        {
            EnableRaisingEvents = true,
            NotifyFilter = IO.NotifyFilters.Attributes | IO.NotifyFilters.LastWrite | IO.NotifyFilters.FileName | IO.NotifyFilters.DirectoryName,
            IncludeSubdirectories = allDirectories,
        };
        var channel = Channel.CreateBounded<bool>(onlyLastMessage);

        void NotifyOfChange(object? _ = null, IO.FileSystemEventArgs? e = null) => Safe.TryAsync(async () => await channel.Writer.WriteAsync(true, cancellation));

        fw.Created += NotifyOfChange;
        fw.Renamed += NotifyOfChange;
        fw.Changed += NotifyOfChange;
        fw.Deleted += NotifyOfChange;

        NotifyOfChange();

        return channel.Reader.ReadAllAsync(cancellation).EndOnCancel(cancellation, whenDone: () =>
        {
            fw.Created -= NotifyOfChange;
            fw.Renamed -= NotifyOfChange;
            fw.Changed -= NotifyOfChange;
            fw.Deleted -= NotifyOfChange;
            fw.Dispose();
        });
    }

    [Obsolete]
    public static IAsyncEnumerable<bool> Live(this Directory directory, bool allDirectories = false, CancellationToken cancellation = default) => Live(directory.Path, allDirectories: allDirectories, cancellation: cancellation);

    [Obsolete]
    public static IAsyncEnumerable<bool> Live(this File file, CancellationToken cancellation = default) => Live(file.Parent.Path, file.Name, allDirectories: false, cancellation);

    [Obsolete]
    public record FileChanges(File[] All, File[] Changed, File[] Added, File[] Deleted);

    [Obsolete]
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
    #endregion
}