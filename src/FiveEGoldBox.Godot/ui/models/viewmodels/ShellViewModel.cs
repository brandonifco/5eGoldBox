// Exploration/RegionalMap/CombatViewModel are deliberately not nested
// here even though Mode picks one of them — per the governing plan's
// §15.2 sketch of the (not-yet-authorized) future UiSnapshot, they sit
// alongside ShellViewModel, not inside it. ShellViewModel is the shell
// chrome only: header, party, messages, commands, modal.
internal sealed record ShellViewModel(
	PresentationMode Mode,
	bool Immersive,
	HeaderViewModel Header,
	PartyViewModel Party,
	MessageLogViewModel Messages,
	CommandSetViewModel ActiveCommandSet,
	ModalViewModel? ActiveModal = null);
