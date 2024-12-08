using System;
using System.Collections.Generic;
using System.Threading;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class PlaneParticleSystems : DrawableGameComponent, IPlaneParticleSystems
{
	private readonly Pool<PlaneParticleSystem> PooledParticleSystems = new Pool<PlaneParticleSystem>();

	private const int LimitBeforeMultithread = 8;

	private Worker<MtUpdateContext<List<PlaneParticleSystem>>> otherThread;

	private readonly List<PlaneParticleSystem> OtherDeadParticleSystems = new List<PlaneParticleSystem>();

	private readonly List<PlaneParticleSystem> ActiveParticleSystems = new List<PlaneParticleSystem>();

	private readonly List<PlaneParticleSystem> DeadParticleSystems = new List<PlaneParticleSystem>();

	private Texture2D WhiteSquare;

	private SoundEffect liquidSplash;

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	public PlaneParticleSystems(Game game)
		: base(game)
	{
		base.DrawOrder = 20;
	}

	public override void Initialize()
	{
		base.Initialize();
		WhiteSquare = CMProvider.Global.Load<Texture2D>("Background Planes/white_square");
		liquidSplash = CMProvider.Global.Load<SoundEffect>("Sounds/Nature/WaterSplash");
		PooledParticleSystems.Size = 5;
		LevelManager.LevelChanged += delegate
		{
			foreach (PlaneParticleSystem activeParticleSystem in ActiveParticleSystems)
			{
				activeParticleSystem.Clear();
				PooledParticleSystems.Return(activeParticleSystem);
			}
			ActiveParticleSystems.Clear();
			if (!LevelManager.Rainy)
			{
				PooledParticleSystems.Size = 5;
				OtherDeadParticleSystems.Capacity = 50;
				DeadParticleSystems.Capacity = 50;
				ActiveParticleSystems.Capacity = 100;
				while (PooledParticleSystems.Available > 5)
				{
					ServiceHelper.RemoveComponent(PooledParticleSystems.Take());
				}
			}
		};
		otherThread = ThreadPool.Take<MtUpdateContext<List<PlaneParticleSystem>>>(UpdateParticleSystems);
		otherThread.Priority = ThreadPriority.Normal;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.InMap || GameState.InMenuCube)
		{
			return;
		}
		TimeSpan elapsed = gameTime.ElapsedGameTime;
		if (GameState.Paused || GameState.InMap || !CameraManager.Viewpoint.IsOrthographic() || !CameraManager.ActionRunning)
		{
			elapsed = TimeSpan.Zero;
		}
		int count = ActiveParticleSystems.Count;
		if (count >= 8)
		{
			MtUpdateContext<List<PlaneParticleSystem>> mtUpdateContext = default(MtUpdateContext<List<PlaneParticleSystem>>);
			mtUpdateContext.Elapsed = elapsed;
			mtUpdateContext.StartIndex = 0;
			mtUpdateContext.EndIndex = count / 2;
			mtUpdateContext.Result = OtherDeadParticleSystems;
			MtUpdateContext<List<PlaneParticleSystem>> context = mtUpdateContext;
			otherThread.Start(context);
			mtUpdateContext = default(MtUpdateContext<List<PlaneParticleSystem>>);
			mtUpdateContext.Elapsed = elapsed;
			mtUpdateContext.StartIndex = count / 2;
			mtUpdateContext.EndIndex = count;
			mtUpdateContext.Result = DeadParticleSystems;
			MtUpdateContext<List<PlaneParticleSystem>> context2 = mtUpdateContext;
			UpdateParticleSystems(context2);
			otherThread.Join();
		}
		else
		{
			UpdateParticleSystems(new MtUpdateContext<List<PlaneParticleSystem>>
			{
				Elapsed = elapsed,
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
		foreach (PlaneParticleSystem deadParticleSystem in DeadParticleSystems)
		{
			ActiveParticleSystems.Remove(deadParticleSystem);
			deadParticleSystem.Clear();
			PooledParticleSystems.Return(deadParticleSystem);
		}
		DeadParticleSystems.Clear();
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading || GameState.StereoMode || GameState.InMap)
		{
			return;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		graphicsDevice.GetDssCombiner().StencilPass = StencilOperation.Keep;
		graphicsDevice.GetDssCombiner().StencilFunction = CompareFunction.Always;
		graphicsDevice.GetDssCombiner().DepthBufferEnable = true;
		graphicsDevice.GetDssCombiner().DepthBufferFunction = CompareFunction.LessEqual;
		graphicsDevice.GetDssCombiner().DepthBufferWriteEnable = false;
		graphicsDevice.GetRasterCombiner().CullMode = CullMode.CullCounterClockwiseFace;
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		bool flag = LevelManager.Name == "ELDERS";
		base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
		for (int i = 0; i < ActiveParticleSystems.Count; i++)
		{
			PlaneParticleSystem planeParticleSystem = ActiveParticleSystems[i];
			planeParticleSystem.InScreen = flag || CameraManager.Frustum.Contains(planeParticleSystem.Settings.SpawnVolume) != ContainmentType.Disjoint;
			if (planeParticleSystem.InScreen && planeParticleSystem.DrawOrder == 0)
			{
				planeParticleSystem.Draw();
			}
		}
	}

	public void ForceDraw()
	{
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		graphicsDevice.GetDssCombiner().StencilPass = StencilOperation.Keep;
		graphicsDevice.GetDssCombiner().StencilFunction = CompareFunction.Always;
		graphicsDevice.GetDssCombiner().DepthBufferWriteEnable = false;
		graphicsDevice.GetDssCombiner().DepthBufferFunction = CompareFunction.LessEqual;
		graphicsDevice.GetRasterCombiner().CullMode = CullMode.CullCounterClockwiseFace;
		graphicsDevice.SamplerStates[0] = SamplerState.PointClamp;
		for (int i = 0; i < ActiveParticleSystems.Count; i++)
		{
			if (ActiveParticleSystems[i].InScreen)
			{
				ActiveParticleSystems[i].Draw();
			}
		}
	}

	private void UpdateParticleSystems(MtUpdateContext<List<PlaneParticleSystem>> context)
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

	public PlaneParticleSystem RainSplash(Vector3 center)
	{
		PlaneParticleSystem planeParticleSystem = PooledParticleSystems.Take();
		planeParticleSystem.MaximumCount = 3;
		if (planeParticleSystem.Settings == null)
		{
			planeParticleSystem.Settings = new PlaneParticleSystemSettings();
		}
		planeParticleSystem.Settings.NoLightDraw = true;
		planeParticleSystem.Settings.SpawnVolume = new BoundingBox
		{
			Min = center - FezMath.XZMask * 0.15f,
			Max = center + FezMath.XZMask * 0.15f
		};
		planeParticleSystem.Settings.Velocity.Function = null;
		planeParticleSystem.Settings.Velocity.Base = new Vector3(0f, 3.5f, 0f);
		planeParticleSystem.Settings.Velocity.Variation = new Vector3(2f, 1.5f, 2f);
		planeParticleSystem.Settings.Gravity = new Vector3(0f, -0.4f, 0f);
		planeParticleSystem.Settings.SpawningSpeed = 60f;
		planeParticleSystem.Settings.ParticleLifetime = 0.275f;
		planeParticleSystem.Settings.SystemLifetime = 0.275f;
		planeParticleSystem.Settings.FadeInDuration = 0f;
		planeParticleSystem.Settings.FadeOutDuration = 0.5f;
		planeParticleSystem.Settings.SpawnBatchSize = 3;
		planeParticleSystem.Settings.SizeBirth.Function = null;
		planeParticleSystem.Settings.SizeBirth.Variation = Vector3.Zero;
		planeParticleSystem.Settings.SizeBirth.Base = new Vector3(0.0625f);
		planeParticleSystem.Settings.ColorLife.Base = new Color(145, 182, 255, 96);
		planeParticleSystem.Settings.ColorLife.Variation = new Color(0, 0, 0, 32);
		planeParticleSystem.Settings.ColorLife.Function = null;
		planeParticleSystem.Settings.Texture = WhiteSquare;
		planeParticleSystem.Settings.BlendingMode = BlendingMode.Alphablending;
		planeParticleSystem.Settings.Billboarding = true;
		Add(planeParticleSystem);
		return planeParticleSystem;
	}

	public void Splash(IPhysicsEntity entity, bool outwards)
	{
		Splash(entity, outwards, 0f);
	}

	public void Splash(IPhysicsEntity entity, bool outwards, float velocityBonus)
	{
		if (LevelManager.WaterType != 0)
		{
			Vector3 vector = entity.Center * FezMath.XZMask + LevelManager.WaterHeight * Vector3.UnitY - Vector3.UnitY * 0.5f;
			float num = Math.Min(Math.Abs(entity.Velocity.Y) / 0.25f, 1.75f) + velocityBonus;
			liquidSplash.EmitAt(entity.Center, RandomHelper.Centered(0.014999999664723873), FezMath.Saturate(num));
			if (outwards)
			{
				vector += entity.Velocity * FezMath.XZMask * 10f;
				num = 0.5f;
			}
			bool flag = LevelManager.WaterType == LiquidType.Lava;
			LiquidColorScheme liquidColorScheme = LiquidHost.ColorSchemes[LevelManager.WaterType];
			Color color = (flag ? liquidColorScheme.SolidOverlay : Color.White);
			Color @base = new Color((liquidColorScheme.SubmergedFoam.ToVector3() + color.ToVector3()) / 2f);
			Color variation = new Color(color.ToVector3() - @base.ToVector3());
			int num2 = 1;
			if (flag)
			{
				num /= 4f;
				num2 *= 2;
			}
			PlaneParticleSystemSettings planeParticleSystemSettings = new PlaneParticleSystemSettings
			{
				SpawnVolume = new BoundingBox
				{
					Min = vector - FezMath.XZMask * 0.15f,
					Max = vector + FezMath.XZMask * 0.15f
				},
				Velocity = 
				{
					Base = new Vector3(0f, 7f * num, 0f),
					Variation = new Vector3(2f, 2f * num, 2f)
				},
				Gravity = new Vector3(0f, flag ? (-0.15f) : (-0.35f), 0f),
				SpawningSpeed = 60f,
				ParticleLifetime = 0.85f,
				SystemLifetime = 0.85f,
				FadeInDuration = 0f,
				FadeOutDuration = 0.5f,
				SpawnBatchSize = 50,
				SizeBirth = new VaryingVector3
				{
					Base = new Vector3(0.125f * (float)num2),
					Variation = new Vector3(0.0625f * (float)num2),
					Function = VaryingVector3.Uniform
				},
				SizeDeath = new Vector3(1f / 32f * (float)(num2 * num2)),
				ColorLife = 
				{
					Base = @base,
					Variation = variation,
					Function = VaryingColor.Uniform
				},
				Texture = WhiteSquare,
				BlendingMode = BlendingMode.Alphablending,
				Billboarding = true,
				FullBright = (LevelManager.WaterType == LiquidType.Sewer)
			};
			if (!outwards)
			{
				planeParticleSystemSettings.EnergySource = entity.Center - entity.Velocity * new Vector3(1f, -0.5f, 1f) * 2.5f;
			}
			PlaneParticleSystem planeParticleSystem = PooledParticleSystems.Take();
			planeParticleSystem.MaximumCount = 50;
			planeParticleSystem.Settings = planeParticleSystemSettings;
			Add(planeParticleSystem);
		}
	}

	public void Add(PlaneParticleSystem system)
	{
		if (!system.Initialized)
		{
			ServiceHelper.AddComponent(system);
		}
		if (!system.Initialized)
		{
			system.Initialize();
		}
		system.Revive();
		ActiveParticleSystems.Add(system);
	}

	public void Remove(PlaneParticleSystem system, bool returnToPool)
	{
		ActiveParticleSystems.Remove(system);
		if (returnToPool)
		{
			system.Clear();
			PooledParticleSystems.Return(system);
		}
	}

	protected override void Dispose(bool disposing)
	{
		ThreadPool.Return(otherThread);
		base.Dispose(disposing);
	}
}
