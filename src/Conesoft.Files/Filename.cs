namespace Conesoft.Files
{
    public class Filename
    {
        public Filename(string name, string extension)
        {
            Name = name;
            Extension = extension.StartsWith(".") ? extension.Substring(1) : extension;
        }

        public static Filename From(string name, string extension) => new(name, extension);
        public static Filename FromExtended(string nameWithExtension) => new(System.IO.Path.GetFileNameWithoutExtension(nameWithExtension), System.IO.Path.GetExtension(nameWithExtension));

        public string Name { get; }
        public string Extension { get; }
        public string FilenameWithExtension => Name + "." + Extension;
    }
}
