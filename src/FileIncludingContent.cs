using System.ComponentModel;

namespace Conesoft.Files
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public record FileIncludingContent<T> : File
    {
        public FileIncludingContent(File file, T content) : base(file)
        {
            this.Content = content;
        }

        public T Content { get; set; }
    }

    public record FileIncludingContentMaybe<T> : File
    {
        public FileIncludingContentMaybe(File file, T? contentMaybe) : base(file)
        {
            this.ContentMaybe = contentMaybe;
        }

        public T? ContentMaybe { get; set; }
    }
}
