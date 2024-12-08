using System;
using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class SpinningTreasuresHost : GameComponent
{
	private readonly List<TrileInstance> TrackedTreasures = new List<TrileInstance>();

	private TimeSpan SinceCreated;

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	public SpinningTreasuresHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		LevelManager.TrileRestored += delegate(TrileInstance t)
		{
			if (t.Enabled && (t.Trile.ActorSettings.Type.IsTreasure() || t.Trile.ActorSettings.Type == ActorType.GoldenCube))
			{
				TrackedTreasures.Add(t);
			}
		};
		TryInitialize();
	}

	private void TryInitialize()
	{
		TrackedTreasures.Clear();
		foreach (TrileInstance value in LevelManager.Triles.Values)
		{
			if (value.Enabled && (value.Trile.ActorSettings.Type.IsTreasure() || value.Trile.ActorSettings.Type == ActorType.GoldenCube))
			{
				TrackedTreasures.Add(value);
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMenuCube || GameState.InMap || GameState.InFpsMode || !CameraManager.Viewpoint.IsOrthographic() || !CameraManager.ActionRunning || GameState.Loading)
		{
			return;
		}
		SinceCreated += gameTime.ElapsedGameTime;
		for (int i = 0; i < TrackedTreasures.Count; i++)
		{
			TrileInstance trileInstance = TrackedTreasures[i];
			float num = (float)Math.Sin(SinceCreated.TotalSeconds * 3.1415927410125732 + (double)((float)i / 0.142857f)) * 0.1f;
			float num2 = num - trileInstance.LastTreasureSin;
			trileInstance.LastTreasureSin = num;
			if (trileInstance.Enabled && !trileInstance.Removed)
			{
				if (!trileInstance.Hidden)
				{
					if (trileInstance.Trile.ActorSettings.Type != ActorType.GoldenCube)
					{
						trileInstance.Phi += (float)gameTime.ElapsedGameTime.TotalSeconds * 2f;
					}
					trileInstance.Position += num2 * Vector3.UnitY;
					LevelManager.UpdateInstance(trileInstance);
					LevelMaterializer.GetTrileMaterializer(trileInstance.Trile).UpdateInstance(trileInstance);
				}
			}
			else
			{
				TrackedTreasures.RemoveAt(i--);
			}
		}
	}
}
