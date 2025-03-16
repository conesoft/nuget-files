using CT = System.Threading.CancellationToken;
using CTS = System.Threading.CancellationTokenSource;

namespace Conesoft.Files;

[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class LiveExtensions
{
    public static void Live(this IEnumerable<Entry> entries, Action action, CT cancellation, bool all = false) => entries.LiveImplementation(WrapAction(action), all, cancellation);
    public static CTS Live(this IEnumerable<Entry> entries, Action action, bool all = false) => entries.LiveImplementation(WrapAction(action), all)!;

    public static void Live(this IEnumerable<Entry> entries, Func<Task> action, CT cancellation, bool all = false) => entries.LiveImplementation(action, all, cancellation);
    public static CTS Live(this IEnumerable<Entry> entries, Func<Task> action, bool all = false) => entries.LiveImplementation(action, all)!;

    public static void Live(this Entry entry, Action action, CT cancellation, bool all = false) => entry.LiveImplementation(WrapAction(action), all, cancellation);
    public static CTS Live(this Entry entry, Action action, bool all = false) => entry.LiveImplementation(WrapAction(action), all)!;

    public static void Live(this Entry entry, Func<Task> action, CT cancellation, bool all = false) => entry.LiveImplementation(action, all, cancellation);
    public static CTS Live(this Entry entry, Func<Task> action, bool all = false) => entry.LiveImplementation(action, all)!;

    static CTS? LiveImplementation(this IEnumerable<Entry> entries, Func<Task> action, bool all = false, CT? previous = default)
    {
        var tokens = entries.Distinct().Select(entry => entry.LiveImplementation(action, all, previous)).NotNull().Select(cts => cts.Token).ToArray();
        return tokens.Length > 0 ? CTS.CreateLinkedTokenSource(tokens) : null;
    }

    static CTS? LiveImplementation(this Entry entry, Func<Task> action, bool all = false, CT? previous = default)
    {
        var cts = previous != null ? new CTS() : null;
        var ct = previous ?? cts!.Token;

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

        ct.Register(() =>
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

    public static void Changes(this Directory directory, Action<EntryChanges> action, CT cancellation, bool all = false) => directory.ChangesImplementation(WrapAction(action), all, cancellation);
    public static CTS Changes(this Directory directory, Action<EntryChanges> action, bool all = false) => directory.ChangesImplementation(WrapAction(action), all)!;

    public static void Changes(this Directory directory, Func<EntryChanges, Task> action, CT cancellation, bool all = false) => directory.ChangesImplementation(action, all, cancellation);
    public static CTS Changes(this Directory directory, Func<EntryChanges, Task> action, bool all = false) => directory.ChangesImplementation(action, all)!;


    public record EntryChanges(Entry[] All, Entry[] Changed, Entry[] Added, Entry[] Deleted);

    static CTS? ChangesImplementation(this Directory directory, Func<EntryChanges, Task> action, bool subDirectories = false, CT? previous = default)
    {
        Dictionary<Entry, DateTime> lastModified = [];

        return directory.LiveImplementation(async () =>
        {
            var all = directory.Filtered("*", subDirectories).ToArray();
            var added = all.Except(lastModified.Keys).ToArray();
            var deleted = lastModified.Keys.Except(all).ToArray();
            var changed = all.Except(added).Where(e => e.Info?.LastWriteTime switch
            {
                DateTime last => lastModified[e] < last,
                null => lastModified.ContainsKey(e)
            }).ToArray();

            lastModified = all.ToValidDictionaryValues(entry => entry.Info?.LastWriteTime);

            await action(new(
                All: all,
                Changed: changed,
                Added: added,
                Deleted: deleted
            ));
        }, subDirectories, previous);
    }

    private static Func<Task> WrapAction(Action action) => () =>
    {
        action();
        return Task.CompletedTask;
    };

    private static Func<T, Task> WrapAction<T>(Action<T> action) => value =>
    {
        action(value);
        return Task.CompletedTask;
    };
}