namespace Conesoft.Files
{
    public class Filename
    {
        public Filename(string name, string extension)
        {
            Name = name;
            Extension = extension;
        }

        public static Filename From(string name, string extension) => new Filename(name, extension);

        public string Name { get; }
        public string Extension { get; }
        public string FilenameWithExtension => Name + "." + Extension;
    }
}
