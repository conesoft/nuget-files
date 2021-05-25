# files

[![publish to nuget](https://github.com/conesoft/files/actions/workflows/publish.yml/badge.svg)](https://github.com/conesoft/files/actions/workflows/publish.yml)

## tiny example

adapted from realworld use
```csharp
var logfile = Directory.From("SomeRoot") / "Scheduler" / Filename.From(DateTime.Today.ToShortDateString(), "md");

await logfile.AppendText($"- **{task.GetType().Name}** *executed* ");
```

instead of
```csharp
var logPath = System.IO.Path.Combine("SomeRoot", "Scheduler", $"{DateTime.Today.ToShortDateString()}.md");
await System.IO.File.AppendAllTextAsync(logPath, $"- **{task.GetType().Name}** *executed* ");
```

## reading/writing objects
```csharp
var data = await File.From("some\path.json").ReadFromJson<SomeType>();

File.From("some\other path.json").WriteAsJson(data);
```

## temporary directories
```csharp
{
    using var temp = Directory.Common.Temporary();
    
    await (temp / Filename.From("some temporary", "tmp")).WriteBytes(somebytes);
    
    // temporary directory gets deleted when out of scope
}
```

## watching a folder
```csharp
void PrettyPrint(string title, File[] files)
{
    if(files.Length > 0)
    {
        Console.WriteLine($"{title} ({files.Length})");
        foreach(var file in files)
        {
            Console.WriteLine(file);
        }
        Console.WriteLine();
    }
}

await foreach(var files in Directory.From("some\path"))
{
    PrettyPrint("ALL FILES", files.All);
    PrettyPrint("ADDED FILES", files.Added);
    PrettyPrint("CHANGED FILES", files.Changed);
    PrettyPrint("DELETED FILES", files.Deleted);
}
```
