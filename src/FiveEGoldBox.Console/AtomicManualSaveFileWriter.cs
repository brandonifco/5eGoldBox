using System.Text;

namespace FiveEGoldBox.Console;

internal sealed class AtomicManualSaveFileWriter
{
    private const int BufferSize = 4096;

    private static readonly Encoding Utf8WithoutByteOrderMark =
        new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false);

    private readonly Action<string, string> writeTemporaryFile;
    private readonly Action<string, string> replaceTemporaryFile;
    private readonly Action<string> deleteTemporaryFile;

    internal AtomicManualSaveFileWriter(
        Action<string, string>? writeTemporaryFile = null,
        Action<string, string>? replaceTemporaryFile = null,
        Action<string>? deleteTemporaryFile = null)
    {
        this.writeTemporaryFile =
            writeTemporaryFile ?? WriteTemporaryFile;
        this.replaceTemporaryFile =
            replaceTemporaryFile ?? ReplaceTemporaryFile;
        this.deleteTemporaryFile =
            deleteTemporaryFile ?? File.Delete;
    }

    internal void WriteAllText(
        string destinationPath,
        string contents)
    {
        if (string.IsNullOrWhiteSpace(destinationPath))
        {
            throw new ArgumentException(
                "A destination path is required.",
                nameof(destinationPath));
        }

        ArgumentNullException.ThrowIfNull(contents);

        string fullDestinationPath =
            Path.GetFullPath(destinationPath);
        string destinationDirectory =
            Path.GetDirectoryName(fullDestinationPath)
            ?? throw new InvalidOperationException(
                "The destination path does not have a containing directory.");
        string temporaryPath = Path.Combine(
            destinationDirectory,
            $".{Path.GetFileName(fullDestinationPath)}.{Guid.NewGuid():N}.tmp");

        using TemporaryFileCleanup cleanup = new(
            temporaryPath,
            deleteTemporaryFile);

        writeTemporaryFile(
            temporaryPath,
            contents);
        replaceTemporaryFile(
            temporaryPath,
            fullDestinationPath);
        cleanup.MarkReplacementComplete();
    }

    private static void WriteTemporaryFile(
        string temporaryPath,
        string contents)
    {
        using FileStream stream = new(
            temporaryPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            FileOptions.WriteThrough);
        using (StreamWriter writer = new(
            stream,
            Utf8WithoutByteOrderMark,
            BufferSize,
            leaveOpen: true))
        {
            writer.Write(contents);
            writer.Flush();
        }

        stream.Flush(flushToDisk: true);
    }

    private static void ReplaceTemporaryFile(
        string temporaryPath,
        string destinationPath)
    {
        File.Move(
            temporaryPath,
            destinationPath,
            overwrite: true);
    }

    private sealed class TemporaryFileCleanup : IDisposable
    {
        private readonly string temporaryPath;
        private readonly Action<string> deleteTemporaryFile;
        private bool replacementComplete;

        internal TemporaryFileCleanup(
            string temporaryPath,
            Action<string> deleteTemporaryFile)
        {
            this.temporaryPath = temporaryPath;
            this.deleteTemporaryFile = deleteTemporaryFile;
        }

        internal void MarkReplacementComplete()
        {
            replacementComplete = true;
        }

        public void Dispose()
        {
            if (replacementComplete)
            {
                return;
            }

            try
            {
                deleteTemporaryFile(temporaryPath);
            }
            catch (UnauthorizedAccessException)
            {
                // Best-effort cleanup must not conceal the save failure.
            }
            catch (IOException)
            {
                // Best-effort cleanup must not conceal the save failure.
            }
        }
    }
}
