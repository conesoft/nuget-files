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

        public override IO.FileInfo Info => info;
    }
}
