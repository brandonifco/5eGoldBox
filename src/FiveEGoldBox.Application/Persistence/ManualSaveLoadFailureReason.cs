namespace FiveEGoldBox.Application.Persistence;

public enum ManualSaveLoadFailureReason
{
    MalformedSerializedData,
    UnsupportedFormatVersion,
    InvalidSessionState
}
