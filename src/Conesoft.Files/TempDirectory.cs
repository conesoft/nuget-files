using System;
using System.ComponentModel;
using IO = System.IO;

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
                dir = From(IO.Path.GetTempPath()) / IO.Path.GetFileNameWithoutExtension(IO.Path.GetRandomFileName());
            } while (dir.Exists);
            return dir;
        }

        public void Dispose()
        {
            Delete();
        }
    }
}
