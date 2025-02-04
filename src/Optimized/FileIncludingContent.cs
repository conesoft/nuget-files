namespace Conesoft.Files;

[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public record FileIncludingContent<T> : File
{
    public FileIncludingContent(File file, T content) : base(file)
    {
        Content = content;
    }

    public T Content { get; set; }
}

[Browsable(false)]
[EditorBrowsable(EditorBrowsableState.Never)]
public record FileIncludingContentMaybe<T> : File
{
    public FileIncludingContentMaybe(File file, T? contentMaybe) : base(file)
    {
        ContentMaybe = contentMaybe;
    }

    public T? ContentMaybe { get; set; }
}
