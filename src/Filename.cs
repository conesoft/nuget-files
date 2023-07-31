using System.Linq;
using IO = System.IO;

namespace Conesoft.Files
{
    public class Filename
    {
        public Filename(string name, string extension, string? alternateDataStreamName)
        {
            Name = name;
            Extension = extension.StartsWith(".") ? extension[1..] : extension;
            AlternateDataStreamName = alternateDataStreamName;
        }

        public static Filename From(string name, string extension, string? alternateDataStream = null) => new(name, extension, alternateDataStream);
        public static Filename FromExtended(string nameWithExtensionAndAlternateDataStream) => new(
            name: IO.Path.GetFileNameWithoutExtension(nameWithExtensionAndAlternateDataStream.Split(":").First()),
            extension: IO.Path.GetExtension(nameWithExtensionAndAlternateDataStream.Split(":").First()),
            alternateDataStreamName: IO.Path.GetFileName(nameWithExtensionAndAlternateDataStream.Split(":").Length > 1 ? nameWithExtensionAndAlternateDataStream.Split(":")[1] : null)
        );

        public string Name { get; }
        public string Extension { get; }
        public string? AlternateDataStreamName { get; }
        public string FilenameWithExtension => Name + "." + Extension;
        public string FullFilename => FilenameWithExtension + (AlternateDataStreamName != null ? $":{AlternateDataStreamName}" : "");
    }
}
