using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using IO = System.IO;

namespace Conesoft.Files
{
    public class File : Directory
    {
        public File(Directory directoryAsFile) : base(directoryAsFile)
        {
        }

        public async Task<string> ReadText() => await IO.File.ReadAllTextAsync(path);
        public async Task<string[]> ReadLines() => await IO.File.ReadAllLinesAsync(path);
        public async Task<T> ReadFromJson<T>(JsonSerializerOptions? options = null)
        {
            using var stream = IO.File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, options);
        }

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

        public bool Exists => IO.File.Exists(path);

        public IO.FileInfo Info => new IO.FileInfo(path);

        public async Task AppendLine(string content) => await AppendText(content + Environment.NewLine);

        public async Task WriteAsJson<T>(T content, bool pretty = false)
        {
            Parent.Create();
            using var stream = IO.File.Create(path);
            await JsonSerializer.SerializeAsync(stream, content, new JsonSerializerOptions { WriteIndented = pretty });
        }

        public static Filename Name(string filename, string extension) => new Filename(filename, extension);

        public async Task WriteLines(IEnumerable<string> contents)
        {
            Parent.Create();
            await IO.File.WriteAllLinesAsync(path, contents);
        }
    }
}
