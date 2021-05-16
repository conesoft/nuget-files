using System.ComponentModel;
using System.IO.Compression;
using IO = System.IO;

namespace Conesoft.Files
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class Zip : File
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
            ZipFile.ExtractToDirectory(Path, target.Path);
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
                using var file = zip.GetEntry(name).Open();
                using var memory = new IO.MemoryStream();
                file.CopyTo(memory);
                return memory.ToArray();
            }
        }
    }
}
