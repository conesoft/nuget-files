using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using IO = System.IO;

namespace Conesoft.Files
{
    public record File
    {
        protected readonly string path;

        internal File(Directory directoryAsFile)
        {
            path = directoryAsFile.Path;
        }

        private File(string path)
        {
            this.path = path;
        }

        public File(File file)
        {
            path = file.path;
        }

        public override string ToString() => $"{Name}: \"{Parent.Path ?? Path}\"";

        public Directory Parent => IO.Path.GetDirectoryName(path) != null ? new Directory(IO.Path.GetDirectoryName(path)!) : Directory.Invalid;

        public string Path => path;

        public string Name => IO.Path.GetFileName(path);

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

        public async Task<T[]?> ReadFromCsv<T>(CsvConfiguration? options = null)
        {
            if(Exists)
            {
                using var stream = new IO.StreamReader(path, Encoding.UTF8);
                using var csv = options != null ? new CsvReader(stream, options) : new CsvReader(stream, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";"
                });
                return await Task.FromResult(csv.GetRecords<T>().ToArray());
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

        public bool Exists => IO.File.Exists(path);

        public IO.FileInfo Info => new(path);

        public string Extension => IO.Path.GetExtension(path);

        public string NameWithoutExtension => IO.Path.GetFileNameWithoutExtension(path);

        public void Delete() => IO.File.Delete(path);

        public async Task AppendLine(string content) => await AppendText(content + Environment.NewLine);

        [Obsolete("Use WriteAsJson with own supplied JsonSerializerOptions instead")]
        public async Task WriteAsJson<T>(T content, bool pretty = false)
        {
            Parent.Create();
            using var stream = IO.File.Create(path);
            await JsonSerializer.SerializeAsync(stream, content, new JsonSerializerOptions { WriteIndented = pretty });
        }

        public async Task WriteAsJson<T>(T content, JsonSerializerOptions? options = null)
        {
            Parent.Create();
            using var stream = IO.File.Create(path);
            await JsonSerializer.SerializeAsync(stream, content, options);
        }

        public async Task WriteAsCsv<T>(T[] content, bool append = false, CsvConfiguration? options = null)
        {
            Parent.Create();
            using var stream = new IO.StreamWriter(path, append, Encoding.UTF8);
            using var csv = options != null ? new CsvWriter(stream, options) : new CsvWriter(stream, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ";"
            });
            csv.WriteRecords(content);
            await Task.CompletedTask;
        }

        public static File From(string path) => new(path);

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

        // https://stackoverflow.com/a/41559/1528847
        public bool Wait(File file)
        {
            int numTries = 0;
            while (true)
            {
                ++numTries;
                try
                {
                    using var fs = new IO.FileStream(file.Path, IO.FileMode.Open, IO.FileAccess.ReadWrite, IO.FileShare.None, 100);
                    fs.ReadByte();
                    break;
                }
                catch (Exception)
                {
                    if (numTries > 10)
                    {
                        return false;
                    }
                    System.Threading.Thread.Sleep(500);
                }
            }
            return true;
        }

        public Zip AsZip() => new(this, false);
        public Zip AsNewZip() => new(this, true);
    }
}
