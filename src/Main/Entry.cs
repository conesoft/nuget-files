namespace Conesoft.Files;

public record Entry
{
    protected readonly string path;

    internal Entry(string path)
    {
        this.path = path;
    }

    public static Entry? From(string path) => Safe.Try<Entry, IO.DirectoryNotFoundException, IO.FileNotFoundException>(() => ExistsAs(path) switch
    {
        EntryType.Directory => Directory.From(path),
        EntryType.File => File.From(path),
        _ => null
    });

    public static Entry? From(IO.FileSystemInfo info) => info.Attributes.HasFlag(IO.FileAttributes.Directory) ? Directory.From(info.FullName) : File.From(info.FullName);

    public virtual string Name => IO.Path.GetFileName(path);
    public string NameWithoutExtension => IO.Path.GetFileNameWithoutExtension(path);
    public string Extension => IO.Path.GetExtension(path).Replace(".", "");
    public string Path => path;

    public Directory Parent => IO.Path.GetDirectoryName(path) != null ? Directory.From(IO.Path.GetDirectoryName(path)!) : Directory.Invalid;


    public virtual IO.FileSystemInfo? Info => Safe.Try<IO.FileSystemInfo, NullReferenceException>(() => ExistsAs(path) switch
    {
        EntryType.Directory => new IO.DirectoryInfo(path),
        EntryType.File => new IO.FileInfo(path),
        _ => null
    });

    public virtual bool Exists => ExistsAs(path).HasValue;
    public virtual Task Delete()
    {
        if(AsFile is File file)
        {
            return file.Delete();
        }
        if(AsDirectory is Directory directory)
        {
            return directory.Delete();
        }
        return Task.CompletedTask;
    }

    public Entry? Rename(Filename newName)
    {
        var newPath = IO.Path.GetDirectoryName(path) is string parent ? IO.Path.Combine(parent, newName.FilenameWithExtension) : newName.FilenameWithExtension;
        switch (ExistsAs(path))
        {
            case EntryType.Directory:
                IO.Directory.Move(path, newPath);
                break;

            case EntryType.File:
                IO.File.Move(path, newPath);
                break;
        }
        return From(newPath);
    }

    public Entry? MoveTo(Directory directory)
    {
        switch (ExistsAs(path))
        {
            case EntryType.Directory:
                {
                    var newPath = directory / Name;
                    IO.Directory.Move(path, newPath.Path);
                    return newPath;
                }

            case EntryType.File:
                {
                    var newPath = directory / Filename.FromExtended(Name);
                    IO.File.Move(path, newPath.Path);
                    return newPath;
                }

            default:
                return null;
        }
    }

    public bool IsFile => ExistsAs(path) == EntryType.File;
    public bool IsDirectory => ExistsAs(path) == EntryType.Directory;

    public virtual File? AsFile => ExistsAs(path) == EntryType.File ? File.From(Path) : null;
    public virtual Directory? AsDirectory => ExistsAs(path) == EntryType.Directory ? Directory.From(Path) : null;

    public bool Visible
    {
        get => IO.File.GetAttributes(path).HasFlag(IO.FileAttributes.Hidden) == false;
        set
        {
            if (value)
            {
                IO.File.SetAttributes(path, IO.File.GetAttributes(path) & ~IO.FileAttributes.Hidden);
            }
            else
            {
                IO.File.SetAttributes(path, IO.File.GetAttributes(path) | IO.FileAttributes.Hidden);
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

    private enum EntryType { File, Directory }
    static private EntryType? ExistsAs(string path) => IO.Directory.Exists(path) ? EntryType.Directory : (IO.File.Exists(path) ? EntryType.File : null);
    #endregion
}
