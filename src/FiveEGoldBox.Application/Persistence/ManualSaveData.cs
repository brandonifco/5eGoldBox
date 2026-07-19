using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Persistence;

internal sealed record ManualSaveData
{
    public required int FormatVersion { get; init; }

    public required ApplicationSessionState Session { get; init; }
}
