# files

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
