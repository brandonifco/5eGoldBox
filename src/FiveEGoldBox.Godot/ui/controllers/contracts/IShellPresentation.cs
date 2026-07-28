internal interface IShellPresentation
{
	void ShowExploration();

	// Real-session variant: sets the exploration scene, header, and message
	// in one call rather than relying on ShowExploration()'s hardcoded
	// outpost defaults.
	void ShowExploration(
		string sceneKey,
		string location,
		string mode,
		string message);

	void ShowRegionalMap();

	void ShowCombat();

	void SetMessage(string message);

	void SetHeader(string location, string mode);
}
