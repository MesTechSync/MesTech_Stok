namespace MesTech.Infrastructure.Security;

/// <summary>
/// Login audit log — tüm denemeler dosyaya kaydedilir.
/// %LOCALAPPDATA%/MesTech/audit/login.log
/// Sessiz hata — audit yazma hatası uygulamayı DURDURMAZ.
/// </summary>
public class LoginAuditLogger
{
    private readonly string _logPath;

    public LoginAuditLogger()
    {
        _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MesTech", "audit", "login.log");
        Directory.CreateDirectory(Path.GetDirectoryName(_logPath)!);
    }

    public void Log(string username, bool success, string? failReason = null)
    {
        var line = $"{DateTime.UtcNow:O}|{(success ? "OK" : "FAIL")}|{username}|{Environment.MachineName}|{failReason ?? "-"}";
        try
        {
            File.AppendAllText(_logPath, line + Environment.NewLine);
        }
        catch
        {
            // Audit log yazma hatası sessiz — uygulamayı DURDURMAZ
        }
    }
}
