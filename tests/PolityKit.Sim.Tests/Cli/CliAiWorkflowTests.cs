using System.Diagnostics;
using System.Text.Json;

namespace PolityKit.Sim.Tests.Cli;

public sealed class CliAiWorkflowTests
{
    [Fact]
    public void AiCommandsWriteSectionNineArtifactsWithProvenance()
    {
        var root = FindRepositoryRoot();
        var temp = Path.Combine(Path.GetTempPath(), $"politykit-cli-ai-{Guid.NewGuid():N}");
        var runDirectory = Path.Combine(temp, "run");

        try
        {
            RunCli(root, "run", "--models", "need-based-allocation", "--ticks", "3", "--out", runDirectory);

            RunCli(root, "ai-summary", "--run", runDirectory, "--provider", "fake");
            RunCli(root, "ai-suggest-scenario", "--run", runDirectory, "--provider", "fake");
            RunCli(root, "ai-critique-model", "--run", runDirectory, "--model", "need-based-allocation", "--provider", "fake");

            AssertAiArtifact(Path.Combine(runDirectory, "ai-summary.json"), "RunSummary");
            AssertScenarioSuggestionArtifact(Path.Combine(runDirectory, "ai-scenario-suggestions.json"));
            AssertModelCritiqueArtifact(Path.Combine(runDirectory, "ai-model-critique.json"));
        }
        finally
        {
            if (Directory.Exists(temp))
            {
                Directory.Delete(temp, recursive: true);
            }
        }
    }

    [Fact]
    public void AiSummaryRequiresRunDirectory()
    {
        var root = FindRepositoryRoot();

        var result = RunCli(root, expectSuccess: false, "ai-summary", "--provider", "fake");

        Assert.Equal(1, result.ExitCode);
        Assert.Contains("This command requires --run <directory>.", result.Error);
    }

    private static void AssertAiArtifact(string path, string expectedKind)
    {
        Assert.True(File.Exists(path), $"Expected '{path}' to exist.");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        Assert.Equal(expectedKind, GetProperty(root, "kind").GetString());
        Assert.Equal("Succeeded", GetProperty(GetProperty(root, "result"), "status").GetString());
        Assert.Equal("fake", GetProperty(GetProperty(root, "aiAnalysis"), "providerName").GetString());
        Assert.True(GetProperty(GetProperty(root, "aiAnalysis"), "used").GetBoolean());
        Assert.Contains(
            GetProperty(GetProperty(root, "provenance"), "sourceFiles").EnumerateArray().Select(item => item.GetString()),
            item => string.Equals(Path.GetFileName(item), "summary.json", StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertScenarioSuggestionArtifact(string path)
    {
        Assert.True(File.Exists(path), $"Expected '{path}' to exist.");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        Assert.True(GetProperty(root, "canSave").GetBoolean());
        var analysis = GetProperty(root, "analysis");
        Assert.Equal("ScenarioSuggestion", GetProperty(analysis, "kind").GetString());
        Assert.Equal("fake", GetProperty(GetProperty(analysis, "aiAnalysis"), "providerName").GetString());
        Assert.True(GetProperty(GetProperty(analysis, "aiAnalysis"), "used").GetBoolean());
        Assert.True(GetProperty(GetProperty(root, "draft"), "isDraft").GetBoolean());
    }

    private static void AssertModelCritiqueArtifact(string path)
    {
        Assert.True(File.Exists(path), $"Expected '{path}' to exist.");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;

        var analysis = GetProperty(root, "analysis");
        Assert.Equal("ModelCritique", GetProperty(analysis, "kind").GetString());
        Assert.Equal("fake", GetProperty(GetProperty(analysis, "aiAnalysis"), "providerName").GetString());
        Assert.True(GetProperty(GetProperty(analysis, "aiAnalysis"), "used").GetBoolean());
        Assert.NotEmpty(GetProperty(GetProperty(root, "critique"), "assumptionRisks").EnumerateArray());
    }

    private static CliResult RunCli(string root, params string[] arguments)
    {
        return RunCli(root, expectSuccess: true, arguments);
    }

    private static CliResult RunCli(string root, bool expectSuccess, params string[] arguments)
    {
        var cliDll = FindCliDll(root);
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        startInfo.ArgumentList.Add(cliDll);
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start CLI process.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit(30_000);

        var result = new CliResult(process.ExitCode, output, error);
        if (expectSuccess && result.ExitCode != 0)
        {
            throw new InvalidOperationException($"CLI failed with exit code {result.ExitCode}: {result.Error}{result.Output}");
        }

        return result;
    }

    private static string FindCliDll(string root)
    {
        var testDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var targetFramework = testDirectory.Name;
        var configuration = testDirectory.Parent?.Name ?? "Debug";
        var cliDll = Path.Combine(
            root,
            "src",
            "PolityKit.Sim.Cli",
            "bin",
            configuration,
            targetFramework,
            "PolityKit.Sim.Cli.dll");

        if (!File.Exists(cliDll))
        {
            throw new FileNotFoundException("Compiled CLI assembly was not found.", cliDll);
        }

        return cliDll;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PolityKit.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root could not be located.");
    }

    private static JsonElement GetProperty(JsonElement element, string name)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return property.Value;
            }
        }

        throw new KeyNotFoundException($"JSON property '{name}' was not found.");
    }

    private sealed record CliResult(int ExitCode, string Output, string Error);
}
