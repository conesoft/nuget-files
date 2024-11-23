using System;
using System.IO;
using IO = System.IO;

namespace Conesoft.Files
{
    public record Entry
    {
        protected readonly string path;

        internal Entry(string path)
        {
            this.path = path;
        }

        public static Entry? From(string path)
        {
            try
            {
                return IO.File.GetAttributes(path).HasFlag(IO.FileAttributes.Directory) ? Directory.From(path) : File.From(path);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
        public static Entry? From(IO.FileSystemInfo info) => info.Attributes.HasFlag(IO.FileAttributes.Directory) ? Directory.From(info.FullName) : File.From(info.FullName);

    public virtual IO.FileSystemInfo? Info
    {
        get
        {
            try
            {
                var info = new IO.FileInfo(path);
                return info.Attributes.HasFlag(IO.FileAttributes.Directory) ? new IO.DirectoryInfo(path) : info;
            }
            catch (NullReferenceException)
            {
                return null;
            }
        }
    }

    public virtual string Name => IO.Path.GetFileName(path);
    public string Path => path;
    public Directory Parent => IO.Path.GetDirectoryName(path) != null ? Directory.From(IO.Path.GetDirectoryName(path)!) : Directory.Invalid;
    public sealed override string ToString() => $"\"{Name}\" in \"{Parent.Path ?? Path}\"";

    public bool IsFile => From(Path) is File;
    public bool IsDirectory => From(Path) is Directory;

    public File? AsFile => From(Path) as File;
    public Directory? AsDirectory => From(Path) as Directory;
}
}
