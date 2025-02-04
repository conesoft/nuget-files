namespace Conesoft.Files;

public static class WaitTillReadyExtensions
{
    // https://stackoverflow.com/a/41559/1528847
    const int ERROR_SHARING_VIOLATION = 0x00000020;
    const int ERROR_LOCK_VIOLATION = 0x00000021;

    public static bool WaitTillReady(this File file, int tries = 10, int delayInMilliseconds = 500)
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

    public static async Task<bool> WaitTillReadyAsync(this File file, int tries = 10, int delayInMilliseconds = 500)
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

    public static File WhenReady(this File file)
    {
        file.WaitTillReady();
        return file;
    }

    public static async Task<File> WhenReadyAsync(this File file)
    {
        await file.WaitTillReadyAsync();
        return file;
    }

}
