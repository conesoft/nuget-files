using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using IO = System.IO;

namespace Conesoft.Files
{
    public class File : Directory
    {
        internal File(Directory directoryAsFile) : base(directoryAsFile)
        {
        }

        private File(string path) : base(path)
        {
        }

        public async Task<string?> ReadText() => Exists ? await IO.File.ReadAllTextAsync(path) : null;
        public async Task<string[]?> ReadLines() => Exists ? await IO.File.ReadAllLinesAsync(path) : null;
        public async Task<T?> ReadFromJson<T>(JsonSerializerOptions? options = null)
        {
            if (Exists)
            {
                using var stream = IO.File.OpenRead(path);
                return await JsonSerializer.DeserializeAsync<T>(stream, options);
            }
            return default;
        }

        public IO.FileStream OpenRead() => IO.File.OpenRead(path);
        
        public async Task<byte[]?> ReadBytes() => Exists ? await IO.File.ReadAllBytesAsync(path) : null;

        public async Task WriteText(string content)
        {
            Parent.Create();
            await IO.File.WriteAllTextAsync(path, content);
        }

        public async Task AppendText(string content)
        {
            Parent.Create();
            await IO.File.AppendAllTextAsync(path, content);
        }

        public override bool Exists => IO.File.Exists(path);

        public new IO.FileInfo Info => new(path);

        public string Extension => IO.Path.GetExtension(path);

        public string NameWithoutExtension => IO.Path.GetFileNameWithoutExtension(path);

        public override void Delete() => IO.File.Delete(path);

        public async Task AppendLine(string content) => await AppendText(content + Environment.NewLine);

        public async Task WriteAsJson<T>(T content, bool pretty = false)
        {
            Parent.Create();
            using var stream = IO.File.Create(path);
            await JsonSerializer.SerializeAsync(stream, content, new JsonSerializerOptions { WriteIndented = pretty });
        }

        public static new File From(string path) => new(path);

        public async Task WriteLines(IEnumerable<string> contents)
        {
            Parent.Create();
            await IO.File.WriteAllLinesAsync(path, contents);
        }
        
        public async Task WriteBytes(byte[] contents)
        {
            Parent.Create();
            await IO.File.WriteAllBytesAsync(path, contents);
        }
    }
}
