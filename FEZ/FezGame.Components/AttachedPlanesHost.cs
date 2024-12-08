using System.Collections.Generic;
using System.Linq;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class AttachedPlanesHost : GameComponent
{
	private class AttachedPlaneState
	{
		public BackgroundPlane Plane;

		public TrileInstance FirstTrile;

		public Vector3 Offset;
	}

	private readonly List<AttachedPlaneState> TrackedPlanes = new List<AttachedPlaneState>();

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { get; set; }

	public AttachedPlanesHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		TrackedPlanes.Clear();
		foreach (BackgroundPlane value in LevelManager.BackgroundPlanes.Values)
		{
			if (value.AttachedGroup.HasValue)
			{
				TrileInstance trileInstance = LevelManager.Groups[value.AttachedGroup.Value].Triles.FirstOrDefault();
				if (trileInstance != null)
				{
					Vector3 offset = value.Position - trileInstance.Position;
					TrackedPlanes.Add(new AttachedPlaneState
					{
						FirstTrile = trileInstance,
						Offset = offset,
						Plane = value
					});
				}
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (EngineState.Paused || EngineState.InMap || !CameraManager.Viewpoint.IsOrthographic() || !CameraManager.ActionRunning || EngineState.Loading)
		{
			return;
		}
		foreach (AttachedPlaneState trackedPlane in TrackedPlanes)
		{
			trackedPlane.Plane.Position = trackedPlane.FirstTrile.Position + trackedPlane.Offset;
		}
	}
}
