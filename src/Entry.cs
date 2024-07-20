using System;
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

        public static Entry From(string path) => new(path);
        public static Entry From(IO.FileSystemInfo info) => new(info.FullName);

        public virtual IO.FileSystemInfo? Info
        {
            get
            {
                try
                {
                    return IsFile ? AsFile!.Info : (IsDirectory ? AsDirectory!.Info : null);
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

        public bool IsFile => IO.File.Exists(path);
        public bool IsDirectory => IO.Directory.Exists(path);

        public File? AsFile => IsFile ? File.From(path) : null;
        public Directory? AsDirectory => IsDirectory ? Directory.From(path) : null;
    }
}
