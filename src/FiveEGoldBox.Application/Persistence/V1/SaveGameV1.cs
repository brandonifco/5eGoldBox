namespace FiveEGoldBox.Application.Persistence.V1;

internal sealed record SaveGameV1
{
    public required int FormatVersion { get; init; }

    public required SaveSessionV1 Session { get; init; }
}
