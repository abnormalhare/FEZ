using System;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public abstract class EngineStateManager : IEngineStateManager
{
	protected bool paused;

	protected bool inMap;

	protected bool inMenuCube;

	private bool loading;

	public bool Paused => paused;

	public bool InMap => inMap;

	public bool InMenuCube => inMenuCube;

	public abstract bool TimePaused { get; }

	public float FramesPerSecond { get; set; }

	public bool LoopRender { get; set; }

	public bool SkyRender { get; set; }

	public virtual bool Loading
	{
		get
		{
			return loading;
		}
		set
		{
			loading = value;
		}
	}

	public virtual float WaterLevelOffset => 0f;

	public Vector3 WaterBodyColor { get; set; }

	public Vector3 WaterFoamColor { get; set; }

	public bool InEditor { get; set; }

	public float SkyOpacity { get; set; }

	public bool SkipRendering { get; set; }

	public bool StereoMode { get; set; }

	public bool DotLoading { get; set; }

	public FarawayTransitionSettings FarawaySettings { get; private set; }

	public bool InFpsMode { get; set; }

	public event Action PauseStateChanged;

	public EngineStateManager()
	{
		FarawaySettings = new FarawayTransitionSettings();
		SkyOpacity = 1f;
	}

	protected void OnPauseStateChanged()
	{
		this.PauseStateChanged();
	}
}
