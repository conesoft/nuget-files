namespace Conesoft.Files;

public record Directory : Entry
{
    private Directory(string path) : base(path)
    {
    }
    public static new Directory From(string path) => new(path);
    public static Directory From(IO.DirectoryInfo info) => new DirectoryIncludingInfo(info);

    public static Directory Invalid { get; } = new("");
    public static Directories Common { get; } = new();




    public new virtual IO.DirectoryInfo Info => new(path);
    public override bool Exists => IO.Directory.Exists(path);
    public override void Delete() => Safe.Try<IO.DirectoryNotFoundException>(() => IO.Directory.Delete(path, recursive: true));

    public void Create() => IO.Directory.CreateDirectory(path);

    protected virtual Directory SubDirectory(string subdirectory) => new(IO.Path.Combine(path, subdirectory));

    #region Filters
    public IEnumerable<File> FilteredFiles(string filter, bool allDirectories)
    {
        return new IO.DirectoryInfo(path)
            .EnumerateFiles(filter, allDirectories ? IO.SearchOption.AllDirectories : IO.SearchOption.TopDirectoryOnly)
            .Select(File.From)
            ;
    }

    public IEnumerable<Directory> FilteredDirectories(string filter, bool allDirectories)
    {
        return new IO.DirectoryInfo(path)
            .EnumerateDirectories(filter, allDirectories ? IO.SearchOption.AllDirectories : IO.SearchOption.TopDirectoryOnly)
            .Select(From)
            ;
    }

    public virtual IEnumerable<File> Files => FilteredFiles("*", false);
    public virtual IEnumerable<Directory> Directories => FilteredDirectories("*", false);
    public IEnumerable<Entry> Filtered(string filter, bool allDirectories) => FilteredDirectories(filter, allDirectories).Concat<Entry>(FilteredFiles(filter, allDirectories));

    public Entry[] FilteredArray(string filter, bool allDirectories) => Filtered(filter, allDirectories).ToArray();

    public IEnumerable<File> OfType(string extension, bool allDirectories) => FilteredFiles("*." + extension, allDirectories);
    public virtual IEnumerable<File> AllFiles => Exists ? FilteredFiles("*", true) : [];
    #endregion

    public static Directory operator /(Directory directory, string subdirectory) => directory.SubDirectory(subdirectory);
    public static File operator /(Directory directory, Filename filename) => File.From(IO.Path.Combine(directory.path, filename.FilenameWithExtension));
}
