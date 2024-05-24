using IO = System.IO;

namespace Conesoft.Files
{
    internal record FileIncludingInfo : File
    {
        private readonly IO.FileInfo info;
        public FileIncludingInfo(IO.FileInfo info) : base(From(info.FullName))
        {
            this.info = info;
        }

        public virtual bool Equals(FileIncludingInfo? other) => path == other?.path;
        override public int GetHashCode() => base.GetHashCode();

        public override IO.FileInfo Info => info;
    }
}
