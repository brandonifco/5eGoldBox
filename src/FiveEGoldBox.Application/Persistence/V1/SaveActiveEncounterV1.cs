namespace FiveEGoldBox.Application.Persistence.V1;

// Deliberately empty: no application mode can currently be persisted with an
// active encounter (see ManualSaveSerializer.IsSaveableMode), and EncounterState
// carries resolved combat data (weapon attacks, saving-throw bonuses) that must
// be re-derived rather than persisted. This placeholder exists only so that a
// present-but-unsupported "ActiveEncounter" node still round-trips through JSON
// and is rejected by SaveGameMapper rather than silently dropped.
internal sealed record SaveActiveEncounterV1;
