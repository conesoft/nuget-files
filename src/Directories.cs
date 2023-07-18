using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using E = System.Environment;

namespace Conesoft.Files
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class Directories
    {
        static internal Directory FromSpecial(E.SpecialFolder folder) => Directory.From(E.GetFolderPath(folder));

        public Directory Current => Directory.From(E.CurrentDirectory);

        public TempDirectory Temporary => new();

        public UserDirectories User { get; } = new UserDirectories();

    }

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class UserDirectories
    {
        public Directory Local => Directories.FromSpecial(E.SpecialFolder.LocalApplicationData);
        public Directory Roaming => Directories.FromSpecial(E.SpecialFolder.ApplicationData);
        public Directory Documents => Directories.FromSpecial(E.SpecialFolder.MyDocuments);
        public Directory Pictures => Directories.FromSpecial(E.SpecialFolder.MyPictures);
        public Directory Music => Directories.FromSpecial(E.SpecialFolder.MyMusic);
        public Directory Videos => Directories.FromSpecial(E.SpecialFolder.MyVideos);
        public Directory Desktop => Directories.FromSpecial(E.SpecialFolder.DesktopDirectory);
        public Directory Downloads => Directory.From(SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0));


        [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
        private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = default);

    }
}
