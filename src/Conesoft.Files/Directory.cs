using IO = System.IO;

namespace Conesoft.Files
{
    public class Directory
    {
        public static Directory Invalid = new Directory("");

        protected readonly string path;

        public Directory(string path)
        {
            this.path = path;
        }

        public Directory(Directory directory)
        {
            this.path = directory.path;
        }

        public Directory Parent => IO.Path.GetDirectoryName(path) != null ? new Directory(IO.Path.GetDirectoryName(path)!) : Invalid;

        public Directory SubDirectory(string subdirectory) => new Directory(IO.Path.Combine(path, subdirectory));

        public File AsFile => new File(this);

        public void Create() => IO.Directory.CreateDirectory(path);

        public static Directory operator /(Directory directory, string subdirectory) => directory.SubDirectory(subdirectory);
        public static File operator /(Directory directory, Filename filename) => new File(directory.SubDirectory(filename.FilenameWithExtension));
    }
}
