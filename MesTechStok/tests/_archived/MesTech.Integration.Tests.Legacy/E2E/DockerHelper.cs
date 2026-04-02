using System.Runtime.InteropServices;
using Xunit;

namespace MesTech.Integration.Tests.E2E;

/// <summary>
/// Docker kullanilabilirligini kontrol eder.
/// Docker yoksa E2E testleri skip olur — CI'da veya Docker olmayan ortamlarda kirilmaz.
/// </summary>
public static class DockerHelper
{
    private static bool? _isAvailable;

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

    /// <summary>Docker yoksa test skip et (xUnit SkippableFact).</summary>
    public static void SkipIfNoDocker()
    {
        Skip.IfNot(IsAvailable,
            "Docker daemon calismadigi icin E2E testi atlaniyor. Docker Desktop baslatip tekrar deneyin.");
    }
}
