using System.Collections.Generic;
using System.Linq;
using IO = System.IO;

namespace Conesoft.Files
{
    public record Directory : Entry
    {
        public static Directory Invalid { get; } = new("");
        public static Directories Common { get; } = new();

        private Directory(string path) : base(path)
        {
        }
        public static new Directory From(string path) => new(path);
        public static Directory From(IO.DirectoryInfo info) => new DirectoryIncludingInfo(info);

        protected virtual Directory SubDirectory(string subdirectory) => new(IO.Path.Combine(path, subdirectory));

        public bool Exists => IO.Directory.Exists(path);

        public new virtual IO.DirectoryInfo Info => new(path);

        public void Create() => IO.Directory.CreateDirectory(path);

        public void Delete()
        {
            try
            {
                IO.Directory.Delete(path, recursive: true);
            }
            catch (IO.DirectoryNotFoundException)
            {
            }
        }

        public virtual void MoveTo(Directory target)
        {
            target.Delete();
            IO.Directory.Move(Path, target.Path);
        }

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
                .Select(Directory.From)
                ;
        }

        public virtual IEnumerable<File> Files => FilteredFiles("*", false);
        public virtual IEnumerable<Directory> Directories => FilteredDirectories("*", false);
        public IEnumerable<Entry> Filtered(string filter, bool allDirectories) => FilteredDirectories(filter, allDirectories).Concat<Entry>(FilteredFiles(filter, allDirectories));

        public Entry[] FilteredArray(string filter, bool allDirectories) => Filtered(filter, allDirectories).ToArray();

        public IEnumerable<File> OfType(string extension, bool allDirectories) => FilteredFiles("*." + extension, allDirectories);
        public virtual IEnumerable<File> AllFiles => Exists ? FilteredFiles("*", true) : [];

        public static Directory operator /(Directory directory, string subdirectory) => directory.SubDirectory(subdirectory);
        public static File operator /(Directory directory, Filename filename) => File.From(IO.Path.Combine(directory.path, filename.FilenameWithExtension));
    }
}
