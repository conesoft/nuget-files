namespace Conesoft.Files;

public class Filename(string name, string extension, string? alternateDataStreamName, string? subpath)
{
    public static Filename From(string name, string extension, string? alternateDataStream = null) => new(name, extension, alternateDataStream, subpath: null);
    public static Filename FromExtended(string nameWithExtensionAndAlternateDataStream)
    {
        var segments = nameWithExtensionAndAlternateDataStream.Split(":");
        return new(
            name: IO.Path.GetFileNameWithoutExtension(segments[0]),
            extension: IO.Path.GetExtension(segments[0]),
            alternateDataStreamName: segments.Length > 1 ? IO.Path.GetFileName(segments[1]) : null,
            subpath: IO.Path.GetDirectoryName(segments[0])
        );
    }

    internal string? Subpath { get; set; } = subpath;
    public string Name { get; } = name;
    public string Extension { get; } = extension.StartsWith('.') ? extension[1..] : extension;
    public string? AlternateDataStreamName { get; } = alternateDataStreamName;
    public string FilenameWithExtension => Name + "." + Extension;
    public string FullFilename => FilenameWithExtension + (AlternateDataStreamName != null ? $":{AlternateDataStreamName}" : "");
}
