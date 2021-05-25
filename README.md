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
await foreach(var files in Directory.From("some\path"))
{
    Console.WriteLine($"ALL FILES ({files.All.Length})");
    foreach(var file in files.All)
    {
        Console.WriteLine(file);
    }
    Console.WriteLine();
    
    Console.WriteLine("NEW FILES ({files.Added.Length})");
    foreach(var file in files.Added)
    {
        Console.WriteLine(file);
    }
    Console.WriteLine();
    
    Console.WriteLine("CHANGED FILES ({files.Changed.Length})");
    foreach(var file in files.Changed)
    {
        Console.WriteLine(file);
    }
    Console.WriteLine();
    
    Console.WriteLine("DELETED FILES ({files.Deleted.Length})");
    foreach(var file in files.Deleted)
    {
        Console.WriteLine(file);
    }
    Console.WriteLine();
}
```
