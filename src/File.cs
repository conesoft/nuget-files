using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
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

        public async Task<string?> ReadText() => await Safe(async () => await IO.File.ReadAllTextAsync(path));
        public async Task<string[]?> ReadLines() => await Safe(async () => await IO.File.ReadAllLinesAsync(path));
        public async Task<byte[]?> ReadBytes() => await Safe(async () => await IO.File.ReadAllBytesAsync(path));
        public async Task<T?> ReadFromJson<T>(JsonSerializerOptions? options = null) => await Safe(async () =>
        {
            using var stream = new IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite | IO.FileShare.Delete, 0x1000, IO.FileOptions.SequentialScan);
            return await JsonSerializer.DeserializeAsync<T>(stream, options ?? defaultOptions);
        });

        public IO.FileStream OpenRead() => IO.File.OpenRead(path);

        public async Task WriteText(string content)
        {
            Parent.Create();
            await IO.File.WriteAllTextAsync(path, content);
        }

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

        public async Task WriteAsJson<T>(T content, JsonSerializerOptions? options = null)
        {
            Parent.Create();
            using var stream = IO.File.Create(path);
            await JsonSerializer.SerializeAsync(stream, content, options ?? defaultOptions);
        }

        public IO.FileStream OpenWrite() => IO.File.OpenWrite(path);

        public async Task AppendText(string content)
        {
            Parent.Create();
            await IO.File.AppendAllTextAsync(path, content);
        }

        public bool Exists => IO.File.Exists(path);

        public virtual IO.FileInfo Info => new(path);

        public string Extension => IO.Path.GetExtension(path);

        public string NameWithoutExtension => IO.Path.GetFileNameWithoutExtension(path);

        public bool IsAlternateDataStream => Name.Contains(':');

        public string? AlternateDataStreamName => IsAlternateDataStream ? Name.Split(':')[1] : null;

        public void Delete() => IO.File.Delete(path);

        public async Task AppendLine(string content) => await AppendText(content + Environment.NewLine);

        public static File From(string path) => new(path);

        public static File From(IO.FileInfo info) => new FileIncludingInfo(info);

        // https://stackoverflow.com/a/41559/1528847
        public bool WaitTillReady(int tries = 10, int delayInMilliseconds = 500)
        {
            const int ERROR_SHARING_VIOLATION = 0x00000020;
            const int ERROR_LOCK_VIOLATION = 0x00000021;

            int numTries = 0;
            while (true)
            {
                ++numTries;
                try
                {
                    using var fs = new IO.FileStream(Path, IO.FileMode.Open, IO.FileAccess.ReadWrite, IO.FileShare.None, 100);
                    fs.ReadByte();
                    break;
                }
                catch (IO.IOException e) when ((e.HResult & 0x0000FFFF) == ERROR_SHARING_VIOLATION || (e.HResult & 0x0000FFFF) == ERROR_LOCK_VIOLATION)
                {
                    if (numTries > tries)
                    {
                        return false;
                    }
                    System.Threading.Thread.Sleep(delayInMilliseconds);
                }
            }
            return true;
        }

        public async Task<bool> WaitTillReadyAsync(int tries = 10, int delayInMilliseconds = 500)
        {
            const int ERROR_SHARING_VIOLATION = 0x00000020;
            const int ERROR_LOCK_VIOLATION = 0x00000021;

            int numTries = 0;
            while (true)
            {
                ++numTries;
                try
                {
                    using var fs = new IO.FileStream(Path, IO.FileMode.Open, IO.FileAccess.ReadWrite, IO.FileShare.None, 100);
                    fs.ReadByte();
                    break;
                }
                catch (IO.IOException e) when ((e.HResult & 0x0000FFFF) == ERROR_SHARING_VIOLATION || (e.HResult & 0x0000FFFF) == ERROR_LOCK_VIOLATION)
                {
                    if (numTries > tries)
                    {
                        return false;
                    }
                }

                await Task.Delay(delayInMilliseconds);
            }
            return true;
        }

        public async Task<File> WhenReadyAsync()
        {
            await WaitTillReadyAsync();
            return this;
        }

        public File WhenReady
        {
            get
            {
                WaitTillReady();
                return this;
            }
        }

        public Zip AsZip() => new(this, false);
        public Zip AsNewZip() => new(this, true);


        public File[] AlternateDataStreams => this.GetStreams().ToArray();

        private T? Safe<T>(Func<T?> func)
        {
            if (Exists)
            {
                try
                {
                    return func();
                }
                catch
                {
                }
            }
            return default;
        }

        private async Task<T?> Safe<T>(Func<Task<T?>> func)
        {
            if (Exists)
            {
                try
                {
                    return await func();
                }
                catch
                {
                }
            }
            return default;
        }


        private static readonly JsonSerializerOptions defaultOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }
}
