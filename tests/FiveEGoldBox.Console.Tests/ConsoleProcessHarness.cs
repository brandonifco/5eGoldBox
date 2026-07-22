using System.Diagnostics;
using System.Text;

namespace FiveEGoldBox.Console.Tests;

internal static class ConsoleProcessHarness
{
    private static readonly TimeSpan DefaultTimeout =
        TimeSpan.FromSeconds(30);

    internal static async Task<ConsoleProcessResult> RunAsync(
        string workingDirectory,
        string scriptedInput,
        TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(workingDirectory))
        {
            throw new ArgumentException(
                "A process working directory is required.",
                nameof(workingDirectory));
        }

        ArgumentNullException.ThrowIfNull(scriptedInput);

        string consoleDllPath = ResolveConsoleDllPath();
        string dotnetHostPath = ResolveDotnetHostPath();
        ProcessStartInfo startInfo = new()
        {
            FileName = dotnetHostPath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        startInfo.ArgumentList.Add(consoleDllPath);

        using Process process = new()
        {
            StartInfo = startInfo
        };

        if (!process.Start())
        {
            throw new InvalidOperationException(
                "The Console process could not be started.");
        }

        int processId = process.Id;
        Task<string> standardOutputTask =
            process.StandardOutput.ReadToEndAsync();
        Task<string> standardErrorTask =
            process.StandardError.ReadToEndAsync();

        try
        {
            await process.StandardInput.WriteAsync(
                scriptedInput);
            await process.StandardInput.FlushAsync();
        }
        catch (IOException) when (process.HasExited)
        {
            // The process ended before consuming every buffered character.
            // Its captured output and exit code remain the primary evidence.
        }
        finally
        {
            process.StandardInput.Close();
        }

        bool timedOut = false;
        using CancellationTokenSource timeoutSource = new(
            timeout ?? DefaultTimeout);

        try
        {
            await process.WaitForExitAsync(
                timeoutSource.Token);
        }
        catch (OperationCanceledException)
            when (timeoutSource.IsCancellationRequested)
        {
            timedOut = true;

            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }
        }

        string standardOutput = await standardOutputTask;
        string standardError = await standardErrorTask;
        bool exitedBeforeReturn = process.HasExited;
        int? exitCode = exitedBeforeReturn
            ? process.ExitCode
            : null;

        return new ConsoleProcessResult(
            ProcessId: processId,
            ExitCode: exitCode,
            TimedOut: timedOut,
            ExitedBeforeReturn: exitedBeforeReturn,
            WorkingDirectory: workingDirectory,
            ConsoleDllPath: consoleDllPath,
            StandardOutput: NormalizeLineEndings(
                standardOutput),
            StandardError: NormalizeLineEndings(
                standardError));
    }

    private static string ResolveConsoleDllPath()
    {
        string repositoryRoot = ResolveRepositoryRoot();
        string configuration = ResolveConfiguration();
        string consoleDllPath = Path.Combine(
            repositoryRoot,
            "src",
            "FiveEGoldBox.Console",
            "bin",
            configuration,
            "net8.0",
            "FiveEGoldBox.Console.dll");

        if (!File.Exists(consoleDllPath))
        {
            throw new FileNotFoundException(
                $"The built Console artifact was not found at '{consoleDllPath}'. Build the solution before running the process tests.",
                consoleDllPath);
        }

        return consoleDllPath;
    }

    private static string ResolveRepositoryRoot()
    {
        DirectoryInfo? directory = new(
            AppContext.BaseDirectory);

        while (directory is not null)
        {
            string solutionPath = Path.Combine(
                directory.FullName,
                "5eGoldBox.sln");

            if (File.Exists(solutionPath))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Could not locate 5eGoldBox.sln by walking upward from '{AppContext.BaseDirectory}'.");
    }

    private static string ResolveConfiguration()
    {
        string fullBaseDirectory = Path.GetFullPath(
            AppContext.BaseDirectory);
        string[] segments = fullBaseDirectory.Split(
            [
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            ],
            StringSplitOptions.RemoveEmptyEntries);

        for (int index = segments.Length - 2;
            index >= 0;
            index--)
        {
            if (string.Equals(
                segments[index],
                "bin",
                StringComparison.OrdinalIgnoreCase))
            {
                return segments[index + 1];
            }
        }

        throw new InvalidOperationException(
            $"Could not infer the active build configuration from '{AppContext.BaseDirectory}'.");
    }

    private static string ResolveDotnetHostPath()
    {
        string? configuredHost = Environment.GetEnvironmentVariable(
            "DOTNET_HOST_PATH");

        if (!string.IsNullOrWhiteSpace(configuredHost))
        {
            return configuredHost;
        }

        string? currentProcessPath = Environment.ProcessPath;

        if (!string.IsNullOrWhiteSpace(currentProcessPath)
            && string.Equals(
                Path.GetFileNameWithoutExtension(
                    currentProcessPath),
                "dotnet",
                StringComparison.OrdinalIgnoreCase))
        {
            return currentProcessPath;
        }

        return "dotnet";
    }

    private static string NormalizeLineEndings(
        string value)
    {
        return value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }
}

internal sealed record ConsoleProcessResult(
    int ProcessId,
    int? ExitCode,
    bool TimedOut,
    bool ExitedBeforeReturn,
    string WorkingDirectory,
    string ConsoleDllPath,
    string StandardOutput,
    string StandardError)
{
    internal string CreateFailureDiagnostic(
        string savePath)
    {
        bool saveExists = File.Exists(savePath);
        long? saveLength = saveExists
            ? new FileInfo(savePath).Length
            : null;
        StringBuilder diagnostic = new();

        diagnostic.AppendLine(
            $"Process ID: {ProcessId}");
        diagnostic.AppendLine(
            $"Exit code: {ExitCode?.ToString() ?? "<not available>"}");
        diagnostic.AppendLine(
            $"Timed out: {TimedOut}");
        diagnostic.AppendLine(
            $"Exited before return: {ExitedBeforeReturn}");
        diagnostic.AppendLine(
            $"Working directory: {WorkingDirectory}");
        diagnostic.AppendLine(
            $"Console DLL: {ConsoleDllPath}");
        diagnostic.AppendLine(
            $"Save exists: {saveExists}");
        diagnostic.AppendLine(
            $"Save length: {saveLength?.ToString() ?? "<not available>"}");
        diagnostic.AppendLine("Standard output:");
        diagnostic.AppendLine(StandardOutput);
        diagnostic.AppendLine("Standard error:");
        diagnostic.AppendLine(StandardError);

        return diagnostic.ToString();
    }
}
