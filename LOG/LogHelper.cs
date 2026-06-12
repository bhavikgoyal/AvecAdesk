namespace AvecADeskApi.LOG;

public class LogHelper
{
    private readonly string _logFolder;

    public LogHelper(IWebHostEnvironment environment)
    {
        _logFolder = Path.Combine(environment.ContentRootPath, "LOG");
        Directory.CreateDirectory(_logFolder);
    }

    public void LogError(string methodName, Exception exception)
    {
        var logFile = Path.Combine(_logFolder, $"error_{DateTime.Now:yyyy-MM-dd}.log");
        var message =
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{methodName}] {exception.Message}{Environment.NewLine}" +
            $"{exception.StackTrace}{Environment.NewLine}{Environment.NewLine}";

        File.AppendAllText(logFile, message);
    }
}
