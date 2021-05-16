using System.Collections.Generic;
using System.Linq;
using IO = System.IO;

namespace Conesoft.Files
{
    public class Directory
    {
        public static Directory Invalid { get; } = new("");
        public static Directories Common { get; } = new();

        protected readonly string path;

        internal Directory(string path)
        {
            this.path = path;
        }

        public Directory(Directory directory)
        {
            this.path = directory.path;
        }

        public Directory Parent => IO.Path.GetDirectoryName(path) != null ? new Directory(IO.Path.GetDirectoryName(path)!) : Invalid;

        Directory SubDirectory(string subdirectory) => new(IO.Path.Combine(path, subdirectory));

        public File AsFile => new(this);

        public string Path => path;

        public string Name => IO.Path.GetFileName(path);

        public virtual bool Exists => IO.Directory.Exists(path);

        public IO.DirectoryInfo Info => new(path);

        public void Create() => IO.Directory.CreateDirectory(path);

        public virtual void Delete() => IO.Directory.Delete(path, recursive: true);

        public virtual void MoveTo(Directory target) => IO.Directory.Move(Path, target.Path);

        public static Directory From(string path) => new(path);

        public IEnumerable<File> Files => IO.Directory.GetFiles(path, "*").Select(File.From);
        public IEnumerable<File> Filtered(string filter, bool allDirectories) => IO.Directory.GetFiles(path, filter, allDirectories ? IO.SearchOption.AllDirectories : IO.SearchOption.TopDirectoryOnly).Select(File.From);
        public IEnumerable<File> OfType(string extension, bool allDirectories) => IO.Directory.GetFiles(path, "*." + extension, allDirectories ? IO.SearchOption.AllDirectories : IO.SearchOption.TopDirectoryOnly).Select(File.From);
        public IEnumerable<File> AllFiles => IO.Directory.GetFiles(path, "*", IO.SearchOption.AllDirectories).Select(File.From);

        public IEnumerable<Directory> Directories => IO.Directory.GetDirectories(path, "*").Select(From);

        public static Directory operator /(Directory directory, string subdirectory) => directory.SubDirectory(subdirectory);
        public static File operator /(Directory directory, Filename filename) => new(directory.SubDirectory(filename.FilenameWithExtension));
    }
}
