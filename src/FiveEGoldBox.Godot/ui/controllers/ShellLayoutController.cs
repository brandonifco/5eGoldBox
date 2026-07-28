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

	public bool IsImmersive { get; private set; }

	public void Initialize()
	{
		_window.MinSize = UiResolutionPresets.Minimum.Size;

		IsImmersive = false;
		_standardLayout.Show();
		_immersiveLayout.Hide();
		_refreshImmersivePartyPreview();
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
