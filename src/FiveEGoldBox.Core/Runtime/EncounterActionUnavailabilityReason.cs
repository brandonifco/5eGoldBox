namespace FiveEGoldBox.Core.Runtime;

public enum EncounterActionUnavailabilityReason
{
    None,
    EncounterCompleted,
    ActorNotParticipant,
    ActorNotActive,
    ActorCannotAct,
    ActionUnavailable,
    BonusActionUnavailable,
    ReactionUnavailable,
    MovementUnavailable,
    ReactionWindowRequired,
    UnsupportedTiming,
    TargetNotParticipant,
    SelfTargetNotAllowed,
    TargetNotHostile,
    TargetCannotBeAttacked,
    WeaponUnavailable,
    TargetOutOfRange,
    LineOfSightBlocked,
    AmmunitionUnavailable
}
