using System;
using System.Threading.Tasks;

namespace Conesoft.Files.Try;

static class Safe
{
    public static T? Try<T>(Func<T?> method)
    {
        try
        {
            return method();
        }
        catch (Exception)
        {
            return default;
        }
    }

    public static void Try(Action action)
    {
        try
        {
            action();
        }
        catch (Exception)
        {
        }
    }

    public static async void TryAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception)
        {
        }
    }
}