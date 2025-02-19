namespace Conesoft.Files;

[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public class SyncWrapper(File my)
{
    #region Reading
    public string? ReadText() => Safe.Try(() => IO.File.ReadAllText(my.Path));
    public string[]? ReadLines() => Safe.Try(() => IO.File.ReadAllLines(my.Path));
    public byte[]? ReadBytes() => Safe.Try(() => IO.File.ReadAllBytes(my.Path));
    public T? ReadFromJson<T>(JsonSerializerOptions? options = null) => Safe.Try(() => JsonSerializer.Deserialize<T>(ReadText()!, options ?? Json.DefaultOptions));
    #endregion

    #region Writing
    public void WriteText(string content)
    {
        my.Parent.Create();
        IO.File.WriteAllText(my.Path, content);
    }
    public void WriteLines(IEnumerable<string> lines)
    {
        my.Parent.Create();
        IO.File.WriteAllLines(my.Path, lines);
    }
    public void WriteBytes(byte[] content)
    {
        my.Parent.Create();
        IO.File.WriteAllBytes(my.Path, content);
    }

    public void WriteAsJson<T>(T content, JsonSerializerOptions? options = null)
    {
        my.Parent.Create();
        WriteText(JsonSerializer.Serialize(content, options ?? Json.DefaultOptions));
    }

    public void AppendText(string content)
    {
        my.Parent.Create();
        IO.File.AppendAllText(my.Path, content);
    }

    public void AppendLine(string line) => AppendText(line + Environment.NewLine);
    #endregion

    public void Delete() => IO.File.Delete(my.Path);
}