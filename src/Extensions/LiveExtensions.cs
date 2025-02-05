using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

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

    public static CancellationTokenSource Live(this Directory directory, Action action, bool allDirectories = false)
    {
        return Live(directory.Path, null, action, allDirectories);
    }
    public static CancellationTokenSource Live(this Directory directory, Func<Task> action, bool allDirectories = false)
    {
        return Live(directory.Path, null, action, allDirectories);
    }

    public static CancellationTokenSource Live(this IEnumerable<Directory> directories, Action action, bool allDirectories = false)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(directories.Distinct().Select(directory => Live(directory.Path, null, action, allDirectories)).Select(cts => cts.Token).ToArray());
    }
    public static CancellationTokenSource Live(this IEnumerable<Directory> directories, Func<Task> action, bool allDirectories = false)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(directories.Distinct().Select(directory => Live(directory.Path, null, action, allDirectories)).Select(cts => cts.Token).ToArray());
    }

    public static CancellationTokenSource Live(this File file, Action action, bool allDirectories = false)
    {
        return Live(file.Parent.Path, file.Name, action, allDirectories);
    }
    public static CancellationTokenSource Live(this File file, Func<Task> action, bool allDirectories = false)
    {
        return Live(file.Parent.Path, file.Name, action, allDirectories);
    }

    public static CancellationTokenSource Live(this IEnumerable<File> files, Action action, bool allDirectories = false)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(files.Distinct().Select(file => Live(file.Parent.Path, file.Name, action, allDirectories)).Select(cts => cts.Token).ToArray());
    }
    public static CancellationTokenSource Live(this IEnumerable<File> files, Func<Task> action, bool allDirectories = false)
    {
        return CancellationTokenSource.CreateLinkedTokenSource(files.Distinct().Select(file => Live(file.Parent.Path, file.Name, action, allDirectories)).Select(cts => cts.Token).ToArray());
    }

    private static CancellationTokenSource Live(string path, string? filter, Action action, bool allDirectories)
    {
        var cts = new CancellationTokenSource();

        var fw = new IO.FileSystemWatcher(path, filter ?? "*")
        {
            EnableRaisingEvents = true,
            NotifyFilter = IO.NotifyFilters.Attributes | IO.NotifyFilters.LastWrite | IO.NotifyFilters.FileName | IO.NotifyFilters.DirectoryName,
            IncludeSubdirectories = allDirectories,
        };

        void NotifyOfChange(object? _ = null, IO.FileSystemEventArgs? e = null) => Safe.Try(action);

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

    private static CancellationTokenSource Live(string path, string? filter, Func<Task> action, bool allDirectories)
    {
        var cts = new CancellationTokenSource();

        var fw = new IO.FileSystemWatcher(path, filter ?? "*")
        {
            EnableRaisingEvents = true,
            NotifyFilter = IO.NotifyFilters.Attributes | IO.NotifyFilters.LastWrite | IO.NotifyFilters.FileName | IO.NotifyFilters.DirectoryName,
            IncludeSubdirectories = allDirectories,
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