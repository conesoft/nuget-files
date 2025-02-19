namespace Conesoft.Files;

public record File : Entry
{
    private File(string path) : base(path)
    {
    }
    public static new File From(string path) => new(path);
    public static File From(IO.FileInfo info) => new FileIncludingInfo(info);

    public SyncWrapper Now => new(this);

    #region Reading
    public async Task<string?> ReadText() => await Safe.TryAsync(async () => await IO.File.ReadAllTextAsync(path));
    public async Task<string[]?> ReadLines() => await Safe.TryAsync(async () => await IO.File.ReadAllLinesAsync(path));
    public async Task<byte[]?> ReadBytes() => await Safe.TryAsync(async () => await IO.File.ReadAllBytesAsync(path));
    public async Task<T?> ReadFromJson<T>(JsonSerializerOptions? options = null) => await Safe.TryAsync(async () =>
    {
        using var stream = new IO.FileStream(path, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite | IO.FileShare.Delete, 0x1000, IO.FileOptions.SequentialScan);
        return await JsonSerializer.DeserializeAsync<T>(stream, options ?? Json.DefaultOptions);
    });
    #endregion

    #region Writing
    public IO.FileStream OpenRead() => IO.File.OpenRead(path);

    public async Task WriteText(string content)
    {
        Parent.Create();
        await IO.File.WriteAllTextAsync(path, content);
    }

    public async Task WriteLines(IEnumerable<string> lines)
    {
        Parent.Create();
        await IO.File.WriteAllLinesAsync(path, lines);
    }

    public async Task WriteBytes(byte[] content)
    {
        Parent.Create();
        await IO.File.WriteAllBytesAsync(path, content);
    }

    public async Task WriteAsJson<T>(T content, JsonSerializerOptions? options = null)
    {
        Parent.Create();
        using var stream = IO.File.Create(path);
        await JsonSerializer.SerializeAsync(stream, content, options ?? Json.DefaultOptions);
    }

    public IO.FileStream OpenWrite() => IO.File.OpenWrite(path);

    public async Task AppendText(string content)
    {
        Parent.Create();
        await IO.File.AppendAllTextAsync(path, content);
    }

    public async Task AppendLine(string content) => await AppendText(content + Env.NewLine);
    #endregion

    public new virtual IO.FileInfo Info => new(path);
    public override bool Exists => IO.File.Exists(path);
    public override Task Delete() => Safe.TryAsync(async () =>
    {
        IO.File.Delete(path);
        while(IO.File.Exists(path) == true)
        {
            await Task.Delay(10);
        }
    });

    public File WhenReady => this.WhenReady();

    #region Zip
    public Zip AsZip() => new(this, false);
    public Zip AsNewZip() => new(this, true);
    #endregion

    #region AlternateDataStream
    public bool IsAlternateDataStream => Name.Contains(':');

    public string? AlternateDataStreamName => IsAlternateDataStream ? Name.Split(':')[1] : null;


    public File[] AlternateDataStreams => this.GetStreams().ToArray();
    #endregion AlternateDataStream
}
