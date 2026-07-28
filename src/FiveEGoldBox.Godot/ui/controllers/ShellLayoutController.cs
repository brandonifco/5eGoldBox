using System;
using Godot;

internal sealed class ShellLayoutController : IShellLayoutState
{
	private readonly Window _window;
	private readonly Control _standardLayout;
	private readonly Control _immersiveLayout;
	private readonly AspectRatioContainer _presentationAspect;
	private readonly AspectRatioContainer _immersivePresentationAspect;
	private readonly PanelContainer _presentationSurface;
	private readonly Action _refreshImmersivePartyPreview;
	private readonly Action _refreshCommandBars;

	public ShellLayoutController(
		Window window,
		Control standardLayout,
		Control immersiveLayout,
		AspectRatioContainer presentationAspect,
		AspectRatioContainer immersivePresentationAspect,
		PanelContainer presentationSurface,
		Action refreshImmersivePartyPreview,
		Action refreshCommandBars)
	{
		_window = window;
		_standardLayout = standardLayout;
		_immersiveLayout = immersiveLayout;
		_presentationAspect = presentationAspect;
		_immersivePresentationAspect = immersivePresentationAspect;
		_presentationSurface = presentationSurface;
		_refreshImmersivePartyPreview = refreshImmersivePartyPreview;
		_refreshCommandBars = refreshCommandBars;
	}

	private int _resolutionPresetIndex;

	public bool IsImmersive { get; private set; }

	public void Initialize()
	{
		_window.MinSize = UiResolutionPresets.Minimum.Size;

		IsImmersive = false;
		_standardLayout.Show();
		_immersiveLayout.Hide();
		_refreshImmersivePartyPreview();
	}

	// Dev-only (M5d): cycles the window through UiResolutionPresets.All so
	// a developer can check a mock scenario at every required test size
	// without editing code or resizing by hand.
	public UiResolutionPreset CycleResolutionPreset()
	{
		_resolutionPresetIndex =
			(_resolutionPresetIndex + 1) % UiResolutionPresets.All.Count;

		UiResolutionPreset preset = UiResolutionPresets.All[_resolutionPresetIndex];
		_window.Size = preset.Size;

		return preset;
	}

	public void Toggle()
	{
		IsImmersive = !IsImmersive;

		ReparentPresentationSurface(
			IsImmersive
				? _immersivePresentationAspect
				: _presentationAspect);

		if (IsImmersive)
		{
			_refreshImmersivePartyPreview();
			_standardLayout.Hide();
			_immersiveLayout.Show();
		}
		else
		{
			_immersiveLayout.Hide();
			_standardLayout.Show();
		}

		_refreshCommandBars();
	}

	private void ReparentPresentationSurface(
		AspectRatioContainer newParent)
	{
		if (_presentationSurface.GetParent() != newParent)
		{
			_presentationSurface.Reparent(newParent, false);
		}

		_presentationSurface.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;
		_presentationSurface.SizeFlagsVertical =
			Control.SizeFlags.ExpandFill;
		newParent.QueueSort();
	}
}
