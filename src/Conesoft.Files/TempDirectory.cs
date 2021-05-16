using System;
using System.ComponentModel;

namespace Conesoft.Files
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    class TempDirectory : Directory, IDisposable
    {
        public TempDirectory() : base(CreateTempDirectory())
        {
        }

        private static Directory CreateTempDirectory()
        {
            Directory dir;
            do
            {
                dir = From(System.IO.Path.GetTempPath()) / System.IO.Path.GetFileNameWithoutExtension(System.IO.Path.GetRandomFileName());
            } while (dir.Exists);
            return dir;
        }

        public void Dispose()
        {
            Delete();
        }
    }
}
