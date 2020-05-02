namespace Conesoft.Files
{
    public class Filename
    {
        public Filename(string name, string extension)
        {
            Name = name;
            Extension = extension;
        }

        public string Name { get; }
        public string Extension { get; }
        public string FilenameWithExtension => Name + "." + Extension;
    }
}
