using System.ComponentModel;
using System.IO.Compression;
using System.Linq;
using IO = System.IO;

namespace Conesoft.Files
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public record Zip : File
    {
        readonly bool asNewFile;

        internal Zip(File zipFile, bool asNewFile) : base(zipFile)
        {
            this.asNewFile = asNewFile;
            if(asNewFile)
            {
                zipFile.Delete();
            }
        }

        private ZipArchive Open()
        {
            return new(new IO.FileStream(Path, asNewFile ? IO.FileMode.OpenOrCreate : IO.FileMode.Open), asNewFile ? ZipArchiveMode.Update : ZipArchiveMode.Read, false);
        }

        public void ExtractTo(Directory target)
        {
            ZipFile.ExtractToDirectory(Path, target.Path, overwriteFiles: true);
        }

        public Entry[] Entries
        {
            get
            {
                using var zip = Open();
                return zip.Entries.Select(e => new Entry(e.Name, DirectoryNameOf(e), e.Length, e.CompressedLength, this)).ToArray();
            }
        }

        public byte[] this[string name]
        {
            set
            {
                using var zip = Open();
                using var file = zip.CreateEntry(name).Open();
                using var writer = new IO.BinaryWriter(file);
                writer.Write(value);
            }
            get
            {
                using var zip = Open();
                var entry = zip.Entries.Where(e => e.Name == name).FirstOrDefault();
                if(entry != null)
                {
                    using var file = entry.Open();
                    using var memory = new IO.MemoryStream();
                    file.CopyTo(memory);
                    return memory.ToArray();
                }
                return [];
            }
        }

        private static string DirectoryNameOf(ZipArchiveEntry entry)
        {
            var path = entry.FullName.Replace(entry.Name, "");
            return path == "" ? "/" : path[..^1];
        }

        public record Entry(string Name, string Path, long Size, long CompressedSize, Zip Parent)
        {
            public double CompressionRatio => (double)Size / CompressedSize;

            public byte[] Contents
            {
                get => Parent[Name];
                set => Parent[Name] = value;
            }
        }
    }
}
