using System.ComponentModel;

namespace Conesoft.Files
{
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class FileIncludingContent<T> : File
    {
        public FileIncludingContent(File file, T? content) : base(file)
        {
            this.Content = content;
        }

        public T? Content { get; set; }
    }
}
