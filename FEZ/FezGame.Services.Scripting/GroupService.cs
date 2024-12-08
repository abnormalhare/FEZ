using System.Collections.Generic;
using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components;
using Microsoft.Xna.Framework;

namespace FezGame.Services.Scripting;

internal class GroupService : IGroupService, IScriptingBase
{
	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	public void MovePathToEnd(int id)
	{
		LevelManager.Groups[id].MoveToEnd = true;
	}

	public void StartPath(int id, bool backwards)
	{
		MovementPath path = LevelManager.Groups[id].Path;
		path.Backwards = backwards;
		path.NeedsTrigger = false;
		if ((!path.SaveTrigger || !LevelManager.IsPathRecorded(id)) && path.SaveTrigger)
		{
			LevelManager.RecordMoveToEnd(id);
		}
	}

	public void RunPathOnce(int id, bool backwards)
	{
		MovementPath path = LevelManager.Groups[id].Path;
		path.Backwards = backwards;
		path.NeedsTrigger = false;
		path.RunOnce = true;
		if ((!path.SaveTrigger || !LevelManager.IsPathRecorded(id)) && path.SaveTrigger)
		{
			LevelManager.RecordMoveToEnd(id);
		}
	}

	public void RunSingleSegment(int id, bool backwards)
	{
		LevelManager.Groups[id].Path.Backwards = backwards;
		LevelManager.Groups[id].Path.NeedsTrigger = false;
		LevelManager.Groups[id].Path.RunSingleSegment = true;
	}

	public void Stop(int id)
	{
		LevelManager.Groups[id].Path.NeedsTrigger = true;
	}

	public void SetEnabled(int id, bool enabled)
	{
		foreach (TrileInstance trile in LevelManager.Groups[id].Triles)
		{
			trile.Enabled = enabled;
		}
		LevelMaterializer.CullInstances();
	}

	public void GlitchyDespawn(int id, bool permanent)
	{
		foreach (TrileInstance trile in LevelManager.Groups[id].Triles)
		{
			if (permanent)
			{
				GameState.SaveData.ThisLevel.DestroyedTriles.Add(trile.OriginalEmplacement);
			}
			ServiceHelper.AddComponent(new GlitchyDespawner(ServiceHelper.Game, trile));
		}
	}

	public LongRunningAction Move(int id, float dX, float dY, float dZ)
	{
		TrileGroup group = LevelManager.Groups[id];
		group.Triles.Sort(new MovingTrileInstanceComparer(new Vector3(dX, dY, dZ)));
		foreach (TrileInstance trile in group.Triles)
		{
			if (trile.PhysicsState == null)
			{
				trile.PhysicsState = new InstancePhysicsState(trile);
			}
		}
		List<ArtObjectInstance> attachedAos = new List<ArtObjectInstance>();
		foreach (ArtObjectInstance value in LevelManager.ArtObjects.Values)
		{
			if (value.ActorSettings.AttachedGroup == id)
			{
				attachedAos.Add(value);
			}
		}
		Vector3 velocity = new Vector3(dX, dY, dZ);
		return new LongRunningAction(delegate(float elapsedSeconds, float _)
		{
			foreach (TrileInstance trile2 in group.Triles)
			{
				trile2.PhysicsState.Velocity = velocity * elapsedSeconds;
				trile2.Position += velocity * elapsedSeconds;
				LevelManager.UpdateInstance(trile2);
			}
			foreach (ArtObjectInstance item in attachedAos)
			{
				item.Position += velocity * elapsedSeconds;
			}
			return false;
		});
	}

	public void ResetEvents()
	{
	}
}
