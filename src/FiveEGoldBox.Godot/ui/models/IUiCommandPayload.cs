// Empty marker, not a generic object/dictionary — the governing plan's
// §7.3 payload guidance is explicit that a command's Payload should be a
// small, named, per-command-family record (e.g. a future TextEntryPayload
// for naming a save slot), typed through this interface, rather than a
// loosely-typed blob. No concrete payload exists yet because no command
// in the three live command sets needs free-text or structured input —
// the first one that does adds its own record implementing this.
internal interface IUiCommandPayload
{
}
