namespace FiveEGoldBox.Application.Content;

/// Finds a file under the data/ content directory at runtime.
///
/// Phase 4 of docs/2026-07-30-data-driven-content-plan.md. Neither real
/// client has an actual publish/export pipeline yet (Console is run via
/// `dotnet run` from a checkout; Godot has no export preset configured), so
/// there is no existing packaging convention to hook into -- only an
/// explicit override, for whenever one exists, and a dev-time fallback for
/// today.
///
/// FIVEEGOLDBOX_DATA_ROOT, when set, names the data/ directory explicitly
/// and is trusted absolutely: a file missing there is a real
/// misconfiguration and fails loudly rather than silently falling through
/// to the search below. Without it, every real caller today (the test
/// suite, Console, the Godot editor) runs from inside a repo checkout, so
/// walking up from the running assembly's own location to find that
/// checkout's data/ directory is a genuine, working dev-time answer -- not
/// a permanent one, and not one that helps a build with no checkout above
/// it.
internal static class DataDirectoryLocator
{
    internal const string DataRootEnvironmentVariable =
        "FIVEEGOLDBOX_DATA_ROOT";

    internal static string ResolveDataFilePath(
        string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);

        string? explicitRoot = Environment.GetEnvironmentVariable(
            DataRootEnvironmentVariable);

        if (explicitRoot is not null)
        {
            string explicitCandidate = Path.Combine(
                explicitRoot,
                relativePath);

            if (!File.Exists(explicitCandidate))
            {
                throw new InvalidOperationException(
                    $"{DataRootEnvironmentVariable} is set to '{explicitRoot}', but '{relativePath}' was not found there.");
            }

            return explicitCandidate;
        }

        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "data",
                relativePath);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"Could not locate '{relativePath}' under any data/ directory above '{AppContext.BaseDirectory}', and {DataRootEnvironmentVariable} is not set.");
    }
}
