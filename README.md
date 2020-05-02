# files

## tiny example
adapted from realworld use
```csharp
var logfile = Directory.From("SomeRoot") / "Scheduler" / File.Name(DateTime.Today.ToShortDateString(), "md");

await logfile.AppendText($"- **{task.GetType().Name}** *executed* ");
```
