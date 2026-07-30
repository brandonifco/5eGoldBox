namespace FiveEGoldBox.Application.Content;

/// Finds a file under the repository's data/ content directory at runtime.
///
/// A placeholder for the real answer Phase 4 of
/// docs/2026-07-30-data-driven-content-plan.md owns -- how a shipped
/// Console/Godot client locates its content once there is an actual
/// packaging story. Today, every real caller (the test suite, Console, the
/// Godot editor) runs from inside a repo checkout, so walking up from the
/// running assembly's own location to find that checkout's data/ directory
/// is a genuine, working answer for now, not a permanent one.
internal static class DataDirectoryLocator
{
    internal static string ResolveDataFilePath(
        string relativePath)
    {
        ArgumentNullException.ThrowIfNull(relativePath);

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
            $"Could not locate '{relativePath}' under any data/ directory above '{AppContext.BaseDirectory}'.");
    }
}
