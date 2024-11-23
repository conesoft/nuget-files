using System.ComponentModel;
using IO = System.IO;

namespace Conesoft.Files
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal record EntryIncludingInfo : Entry
    {
        private readonly IO.FileSystemInfo info;
        public EntryIncludingInfo(IO.FileSystemInfo info) : base(From(info.FullName)!)
        {
            this.info = info;
        }

        public virtual bool Equals(EntryIncludingInfo? other) => path == other?.path;
        override public int GetHashCode() => base.GetHashCode();

        public override IO.FileSystemInfo Info => info;
    }
}
