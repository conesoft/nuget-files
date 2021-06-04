using System.ComponentModel;
using E = System.Environment;

namespace Conesoft.Files
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class Directories
    {
        static internal Directory FromSpecial(E.SpecialFolder folder) => Directory.From(E.GetFolderPath(folder));

        public static Directory Current => Directory.From(E.CurrentDirectory);

        public static TempDirectory Temporary => new();

        public UserDirectories User { get; } = new UserDirectories();

    }

    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class UserDirectories
    {
        public static Directory Local => Directories.FromSpecial(E.SpecialFolder.LocalApplicationData);
        public static Directory Roaming => Directories.FromSpecial(E.SpecialFolder.ApplicationData);
        public static Directory Documents => Directories.FromSpecial(E.SpecialFolder.MyDocuments);
        public static Directory Pictures => Directories.FromSpecial(E.SpecialFolder.MyPictures);
        public static Directory Music => Directories.FromSpecial(E.SpecialFolder.MyMusic);
        public static Directory Videos => Directories.FromSpecial(E.SpecialFolder.MyVideos);
        public static Directory Desktop => Directories.FromSpecial(E.SpecialFolder.DesktopDirectory);
    }
}
