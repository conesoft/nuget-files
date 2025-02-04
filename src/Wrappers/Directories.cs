namespace Conesoft.Files;

[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class Directories
{
    static internal Directory FromSpecial(Env.SpecialFolder folder) => Directory.From(Env.GetFolderPath(folder));

    public Directory Current => Directory.From(Env.CurrentDirectory);

    public TempDirectory Temporary => new();

    public UserDirectories User { get; } = new UserDirectories();

}

[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class UserDirectories
{
    public Directory Local => Directories.FromSpecial(Env.SpecialFolder.LocalApplicationData);
    public Directory Roaming => Directories.FromSpecial(Env.SpecialFolder.ApplicationData);
    public Directory Documents => Directories.FromSpecial(Env.SpecialFolder.MyDocuments);
    public Directory Pictures => Directories.FromSpecial(Env.SpecialFolder.MyPictures);
    public Directory Music => Directories.FromSpecial(Env.SpecialFolder.MyMusic);
    public Directory Videos => Directories.FromSpecial(Env.SpecialFolder.MyVideos);
    public Directory Desktop => Directories.FromSpecial(Env.SpecialFolder.DesktopDirectory);
    public Directory Downloads => Directory.From(SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0));


    [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = default);

}
