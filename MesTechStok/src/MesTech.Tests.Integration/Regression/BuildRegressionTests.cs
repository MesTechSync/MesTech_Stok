using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using FluentAssertions;

namespace MesTech.Tests.Integration.Regression;

/// <summary>
/// 5.D1-04: Build regression test — ensures the production projects build with zero errors.
/// Builds each non-test project individually to avoid DLL lock issues from testhost.
/// </summary>
[Trait("Category", "Regression")]
[Trait("Category", "Build")]
public class BuildRegressionTests
{
    // Non-test projects that must build cleanly
    private static readonly string[] ProductionProjects =
    {
        "MesTech.Domain",
        "MesTech.Application",
        "MesTech.Infrastructure",
        "MesTechStok.Core",
    };

    private static string FindSrcRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
            if (Directory.Exists(Path.Combine(dir, "MesTech.Domain")))
                return dir;
        }
        throw new DirectoryNotFoundException("src/ root not found from test output directory");
    }

    [Theory]
    [InlineData("MesTech.Domain")]
    [InlineData("MesTech.Application")]
    [InlineData("MesTech.Infrastructure")]
    [InlineData("MesTechStok.Core")]
    public void ProductionProject_ShouldBuild_WithZeroErrors(string projectName)
    {
        var srcRoot = FindSrcRoot();
        var csprojFiles = Directory.GetFiles(srcRoot, $"{projectName}.csproj", SearchOption.AllDirectories);
        csprojFiles.Should().NotBeEmpty($"{projectName}.csproj should exist");

        var csprojPath = csprojFiles.First();
        var (exitCode, output) = RunDotnetBuild(csprojPath);

        exitCode.Should().Be(0,
            $"'{projectName}' should build with exit code 0.\nOutput:\n{output}");
    }

    [Theory]
    [InlineData("MesTech.Domain")]
    [InlineData("MesTech.Application")]
    [InlineData("MesTech.Infrastructure")]
    [InlineData("MesTechStok.Core")]
    public void ProductionProject_ShouldHaveNoCompilerErrors(string projectName)
    {
        var srcRoot = FindSrcRoot();
        var csprojFiles = Directory.GetFiles(srcRoot, $"{projectName}.csproj", SearchOption.AllDirectories);
        csprojFiles.Should().NotBeEmpty();

        var csprojPath = csprojFiles.First();
        var (_, output) = RunDotnetBuild(csprojPath);

        output.Should().NotContain("error CS",
            $"'{projectName}' build output should contain no C# compiler errors");
    }

    private static (int ExitCode, string Output) RunDotnetBuild(string csprojPath)
    {
        var stdout = new StringBuilder();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"build \"{csprojPath}\" -v q --nologo",
                WorkingDirectory = Path.GetDirectoryName(csprojPath)!,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            }
        };

        process.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var exited = process.WaitForExit(180_000); // 3 minute timeout per project
        if (!exited)
        {
            try { process.Kill(); } catch { /* Intentional: test cleanup — process disposal failure non-critical */ }
            return (-1, "Build timed out after 3 minutes");
        }

        return (process.ExitCode, stdout.ToString());
    }
}
