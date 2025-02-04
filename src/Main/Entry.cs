namespace Conesoft.Files;

public record Entry
{
    protected readonly string path;

    internal Entry(string path)
    {
        this.path = path;
    }

    public static Entry? From(string path) => Safe.Try<Entry, IO.DirectoryNotFoundException, IO.FileNotFoundException>(() => IsDir(path) ? Directory.From(path) : File.From(path));
    public static Entry? From(IO.FileSystemInfo info) => info.Attributes.HasFlag(IO.FileAttributes.Directory) ? Directory.From(info.FullName) : File.From(info.FullName);

    public string Name => IO.Path.GetFileName(path);
    public string NameWithoutExtension => IO.Path.GetFileNameWithoutExtension(path);
    public string? Extension => IO.Path.GetExtension(path)?[1..];
    public string Path => path;

    public Directory Parent => IO.Path.GetDirectoryName(path) != null ? Directory.From(IO.Path.GetDirectoryName(path)!) : Directory.Invalid;


    public virtual IO.FileSystemInfo? Info => Safe.Try<IO.FileSystemInfo, NullReferenceException>(() => IsDirectory ? new IO.DirectoryInfo(path) : new IO.FileInfo(path));
    public virtual bool Exists => Safe.Try(() => IsDir(path) == false ? AsFile!.Exists : AsDirectory!.Exists);
    public virtual void Delete()
    {
        AsFile?.Delete();
        AsDirectory?.Delete();
    }

    public Entry? Rename(Filename newName)
    {
        var newPath = IO.Path.GetDirectoryName(path) is string parent ? IO.Path.Combine(parent, newName.FilenameWithExtension) : newName.FilenameWithExtension;
        if (IsDirectory)
        {
            IO.Directory.Move(path, newPath);
        }
        else
        {
            IO.File.Move(path, newPath);
        }
        return From(newPath);
    }

    public Entry? MoveTo(Directory directory)
    {
        if (IsDirectory)
        {
            var newPath = directory / Name;
            IO.Directory.Move(path, newPath.Path);
            return newPath;
        }
        else
        {
            var newPath = directory / Filename.FromExtended(Name);
            IO.File.Move(path, newPath.Path);
            return newPath;
        }
    }

    public bool IsFile => IsDir(path) == false;
    public bool IsDirectory => IsDir(path);

    public File? AsFile => IsDir(path) == false ? File.From(Path) : null;
    public Directory? AsDirectory => IsDir(path) ? Directory.From(Path) : null;

    public bool Visible
    {
        get => IO.File.GetAttributes(path).HasFlag(IO.FileAttributes.Hidden) == false;
        set
        {
            if (value)
            {
                IO.File.SetAttributes(path, IO.File.GetAttributes(path) | IO.FileAttributes.Hidden);
            }
            else
            {
                IO.File.SetAttributes(path, IO.File.GetAttributes(path) & ~IO.FileAttributes.Hidden);
            }
        }
    }


    #region Internal / Helpers
    public sealed override string ToString() => $"\"{Name}\" in \"{Parent?.Path ?? Path}\"";

    public virtual bool Equals(Entry? other)
    {
        if (other == null) return false;

        var a = IO.Path.GetFullPath(path);
        var b = IO.Path.GetFullPath(other.path);

        return a.Equals(b, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode() => path.GetHashCode();

    static private bool IsDir(string path) => IO.File.GetAttributes(path).HasFlag(IO.FileAttributes.Directory);
    #endregion
}
