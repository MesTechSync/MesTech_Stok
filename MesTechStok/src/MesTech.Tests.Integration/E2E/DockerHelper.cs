using System.Runtime.InteropServices;

namespace MesTech.Tests.Integration.E2E;

/// <summary>
/// Docker daemon kullanılabilirliğini kontrol eder.
/// Docker yoksa E2E testleri skip olur — CI'da veya Docker olmayan ortamlarda kırılmaz.
/// </summary>
public static class DockerHelper
{
    private static bool? _isAvailable;

    /// <summary>Docker daemon çalışıyor mu?</summary>
    public static bool IsAvailable
    {
        get
        {
            _isAvailable ??= CheckDocker();
            return _isAvailable.Value;
        }
    }

    private static bool CheckDocker()
    {
        try
        {
            var cmd = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? "docker"
                : "/usr/bin/docker";

            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = cmd,
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null) return false;
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Docker yoksa test skip et.</summary>
    public static void SkipIfNoDocker()
    {
        if (!IsAvailable)
        {
            Skip.If(true,
                "Docker daemon çalışmıyor — E2E testi atlanıyor. " +
                "Docker Desktop başlatıp tekrar deneyin.");
        }
    }
}
