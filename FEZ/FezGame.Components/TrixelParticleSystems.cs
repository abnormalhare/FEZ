using System;
using System.Collections.Generic;
using System.Threading;
using FezEngine.Services;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class TrixelParticleSystems : GameComponent, ITrixelParticleSystems
{
	private const int LimitBeforeMultithread = 2;

	private Worker<MtUpdateContext<List<TrixelParticleSystem>>> otherThread;

	private readonly List<TrixelParticleSystem> OtherDeadParticleSystems = new List<TrixelParticleSystem>();

	private readonly List<TrixelParticleSystem> ActiveParticleSystems = new List<TrixelParticleSystem>();

	private readonly List<TrixelParticleSystem> DeadParticleSystems = new List<TrixelParticleSystem>();

	public int Count => ActiveParticleSystems.Count;

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	public TrixelParticleSystems(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		LevelManager.LevelChanged += delegate
		{
			foreach (TrixelParticleSystem activeParticleSystem in ActiveParticleSystems)
			{
				ServiceHelper.RemoveComponent(activeParticleSystem);
			}
			ActiveParticleSystems.Clear();
		};
		otherThread = ThreadPool.Take<MtUpdateContext<List<TrixelParticleSystem>>>(UpdateParticleSystems);
		otherThread.Priority = ThreadPriority.Normal;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap || !CameraManager.Viewpoint.IsOrthographic() || !CameraManager.ActionRunning)
		{
			return;
		}
		TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
		int count = ActiveParticleSystems.Count;
		if (count >= 2)
		{
			MtUpdateContext<List<TrixelParticleSystem>> mtUpdateContext = default(MtUpdateContext<List<TrixelParticleSystem>>);
			mtUpdateContext.Elapsed = elapsedGameTime;
			mtUpdateContext.StartIndex = 0;
			mtUpdateContext.EndIndex = count / 2;
			mtUpdateContext.Result = OtherDeadParticleSystems;
			MtUpdateContext<List<TrixelParticleSystem>> context = mtUpdateContext;
			otherThread.Start(context);
			mtUpdateContext = default(MtUpdateContext<List<TrixelParticleSystem>>);
			mtUpdateContext.Elapsed = elapsedGameTime;
			mtUpdateContext.StartIndex = count / 2;
			mtUpdateContext.EndIndex = count;
			mtUpdateContext.Result = DeadParticleSystems;
			MtUpdateContext<List<TrixelParticleSystem>> context2 = mtUpdateContext;
			UpdateParticleSystems(context2);
			otherThread.Join();
		}
		else
		{
			UpdateParticleSystems(new MtUpdateContext<List<TrixelParticleSystem>>
			{
				Elapsed = elapsedGameTime,
				StartIndex = 0,
				EndIndex = count,
				Result = DeadParticleSystems
			});
		}
		if (OtherDeadParticleSystems.Count > 0)
		{
			DeadParticleSystems.AddRange(OtherDeadParticleSystems);
			OtherDeadParticleSystems.Clear();
		}
		if (DeadParticleSystems.Count <= 0)
		{
			return;
		}
		foreach (TrixelParticleSystem deadParticleSystem in DeadParticleSystems)
		{
			ActiveParticleSystems.Remove(deadParticleSystem);
			ServiceHelper.RemoveComponent(deadParticleSystem);
		}
		DeadParticleSystems.Clear();
	}

	private void UpdateParticleSystems(MtUpdateContext<List<TrixelParticleSystem>> context)
	{
		for (int i = context.StartIndex; i < context.EndIndex; i++)
		{
			if (i < ActiveParticleSystems.Count)
			{
				ActiveParticleSystems[i].Update(context.Elapsed);
				if (ActiveParticleSystems[i].Dead)
				{
					context.Result.Add(ActiveParticleSystems[i]);
				}
			}
		}
	}

	public void Add(TrixelParticleSystem system)
	{
		ServiceHelper.AddComponent(system);
		ActiveParticleSystems.Add(system);
	}

	public void PropagateEnergy(Vector3 energySource, float energy)
	{
		foreach (TrixelParticleSystem activeParticleSystem in ActiveParticleSystems)
		{
			activeParticleSystem.AddImpulse(energySource, energy);
		}
	}

	protected override void Dispose(bool disposing)
	{
		ThreadPool.Return(otherThread);
	}

	public void UnGroundAll()
	{
		foreach (TrixelParticleSystem activeParticleSystem in ActiveParticleSystems)
		{
			activeParticleSystem.UnGround();
		}
	}

	public void ForceDraw()
	{
		foreach (TrixelParticleSystem activeParticleSystem in ActiveParticleSystems)
		{
			activeParticleSystem.DoDraw();
		}
	}
}
