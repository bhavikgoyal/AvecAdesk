namespace AvecADeskApi.LOG;

public class LogHelper
{
    private static readonly object FileLock = new();
    private readonly string _logFolder;

    public LogHelper(IWebHostEnvironment environment)
    {
        _logFolder = Path.Combine(environment.ContentRootPath, "LOG");
        Directory.CreateDirectory(_logFolder);
    }

    public void LogError(string methodName, Exception exception)
    {
        try
        {
            var logFile = Path.Combine(_logFolder, $"error_{DateTime.Now:yyyy-MM-dd}.log");
            var message =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{methodName}] {exception.Message}{Environment.NewLine}" +
                $"{exception.StackTrace}{Environment.NewLine}{Environment.NewLine}";

            lock (FileLock)
            {
                for (var attempt = 0; attempt < 3; attempt++)
                {
                    try
                    {
                        using var stream = new FileStream(
                            logFile,
                            FileMode.Append,
                            FileAccess.Write,
                            FileShare.ReadWrite);
                        using var writer = new StreamWriter(stream);
                        writer.Write(message);
                        return;
                    }
                    catch (IOException) when (attempt < 2)
                    {
                        Thread.Sleep(25 * (attempt + 1));
                    }
                }
            }
        }
        catch
        {
            // Never let logging failures replace the original exception.
        }
    }
}
