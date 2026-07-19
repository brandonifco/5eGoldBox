using FiveEGoldBox.Application.Sessions;

namespace FiveEGoldBox.Application.Persistence;

public sealed record ManualSaveLoadResult
{
    private ManualSaveLoadResult(
        ApplicationSessionState? session,
        ManualSaveLoadFailureReason? failureReason)
    {
        Session = session;
        FailureReason = failureReason;
    }

    public bool IsSuccess =>
        Session is not null
        && FailureReason is null;

    public ApplicationSessionState? Session { get; }

    public ManualSaveLoadFailureReason? FailureReason { get; }

    internal static ManualSaveLoadResult Success(
        ApplicationSessionState session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return new ManualSaveLoadResult(
            session,
            failureReason: null);
    }

    internal static ManualSaveLoadResult Failure(
        ManualSaveLoadFailureReason failureReason)
    {
        if (!Enum.IsDefined(failureReason))
        {
            throw new ArgumentOutOfRangeException(
                nameof(failureReason),
                failureReason,
                "Unsupported manual-save load failure reason.");
        }

        return new ManualSaveLoadResult(
            session: null,
            failureReason);
    }
}
