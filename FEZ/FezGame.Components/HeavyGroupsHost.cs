using System.Collections.Generic;
using System.Linq;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class HeavyGroupsHost : GameComponent
{
	private readonly List<HeavyGroupState> trackedGroups = new List<HeavyGroupState>();

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	public HeavyGroupsHost(Game game)
		: base(game)
	{
		base.UpdateOrder = -2;
	}

	public override void Initialize()
	{
		base.Initialize();
		base.Enabled = false;
		LevelManager.LevelChanging += TrackNewGroups;
		TrackNewGroups();
	}

	private void TrackNewGroups()
	{
		trackedGroups.Clear();
		foreach (TrileGroup item in LevelManager.Groups.Values.Where((TrileGroup x) => x.Heavy))
		{
			trackedGroups.Add(new HeavyGroupState(item));
		}
		base.Enabled = trackedGroups.Count > 0;
	}

	public override void Update(GameTime gameTime)
	{
		if (EngineState.Paused || EngineState.InMap || !CameraManager.Viewpoint.IsOrthographic() || !CameraManager.ActionRunning)
		{
			return;
		}
		foreach (HeavyGroupState trackedGroup in trackedGroups)
		{
			trackedGroup.Update(gameTime.ElapsedGameTime);
		}
	}
}
