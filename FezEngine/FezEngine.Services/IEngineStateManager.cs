using System;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public interface IEngineStateManager
{
	bool Paused { get; }

	bool InMap { get; }

	bool InMenuCube { get; }

	float FramesPerSecond { get; set; }

	bool LoopRender { get; set; }

	bool SkyRender { get; set; }

	bool Loading { get; set; }

	bool InEditor { get; set; }

	bool TimePaused { get; }

	float SkyOpacity { get; set; }

	bool SkipRendering { get; set; }

	float WaterLevelOffset { get; }

	bool StereoMode { get; set; }

	bool DotLoading { get; set; }

	Vector3 WaterBodyColor { get; set; }

	Vector3 WaterFoamColor { get; set; }

	bool InFpsMode { get; set; }

	FarawayTransitionSettings FarawaySettings { get; }

	event Action PauseStateChanged;
}
