namespace Conesoft.Files;

public class Filename(string name, string extension, string? alternateDataStreamName)
{
    public static Filename From(string name, string extension, string? alternateDataStream = null) => new(name, extension, alternateDataStream);
    public static Filename FromExtended(string nameWithExtensionAndAlternateDataStream) => new(
        name: IO.Path.GetFileNameWithoutExtension(nameWithExtensionAndAlternateDataStream.Split(":").First()),
        extension: IO.Path.GetExtension(nameWithExtensionAndAlternateDataStream.Split(":").First()),
        alternateDataStreamName: IO.Path.GetFileName(nameWithExtensionAndAlternateDataStream.Split(":").Length > 1 ? nameWithExtensionAndAlternateDataStream.Split(":")[1] : null)
    );

    public string Name { get; } = name;
    public string Extension { get; } = extension.StartsWith(".") ? extension[1..] : extension;
    public string? AlternateDataStreamName { get; } = alternateDataStreamName;
    public string FilenameWithExtension => Name + "." + Extension;
    public string FullFilename => FilenameWithExtension + (AlternateDataStreamName != null ? $":{AlternateDataStreamName}" : "");
}
