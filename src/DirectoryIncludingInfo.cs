using System.ComponentModel;
using IO = System.IO;

namespace Conesoft.Files
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal record DirectoryIncludingInfo : Directory
    {
        private readonly IO.DirectoryInfo info;
        public DirectoryIncludingInfo(IO.DirectoryInfo info) : base(From(info.FullName))
        {
            this.info = info;
        }

        public virtual bool Equals(DirectoryIncludingInfo? other) => path == other?.path;
        override public int GetHashCode() => base.GetHashCode();

        public override IO.DirectoryInfo Info => info;
    }
}
