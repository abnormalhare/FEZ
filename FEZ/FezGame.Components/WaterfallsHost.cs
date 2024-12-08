using System;
using System.Collections.Generic;
using System.Linq;
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

internal class WaterfallsHost : GameComponent
{
	private class WaterfallState
	{
		private readonly List<BackgroundPlane> AttachedPlanes = new List<BackgroundPlane>();

		private readonly BackgroundPlane Plane;

		private readonly BackgroundPlane Splash;

		private readonly PlaneParticleSystem ParticleSystem;

		private readonly float Top;

		private readonly Vector3 TerminalPosition;

		private readonly WaterfallsHost Host;

		private float lastDistToTop;

		private SoundEmitter BubblingEmitter;

		private float sinceAlive;

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public ILevelMaterializer LevelMaterializer { private get; set; }

		[ServiceDependency]
		public IGameLevelManager LevelManager { private get; set; }

		[ServiceDependency]
		public IContentManagerProvider CMProvider { private get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		public WaterfallState(BackgroundPlane plane, PlaneParticleSystem ps, WaterfallsHost host)
		{
			WaterfallState waterfallState = this;
			ServiceHelper.InjectServices(this);
			Host = host;
			Plane = plane;
			ParticleSystem = ps;
			AnimatedTexture animatedTexture = null;
			bool flag = plane.ActorType == ActorType.Trickle;
			Splash = new BackgroundPlane(animation: (LevelManager.WaterType == LiquidType.Sewer) ? CMProvider.CurrentLevel.Load<AnimatedTexture>("Background Planes/sewer/" + (flag ? "sewer_small_splash" : "sewer_large_splash")) : ((LevelManager.WaterType != LiquidType.Purple) ? CMProvider.CurrentLevel.Load<AnimatedTexture>("Background Planes/water/" + (flag ? "water_small_splash" : "water_large_splash")) : CMProvider.CurrentLevel.Load<AnimatedTexture>("Background Planes/waterPink/" + (flag ? "water_small_splash" : "water_large_splash"))), hostMesh: LevelMaterializer.AnimatedPlanesMesh)
			{
				Doublesided = true,
				Crosshatch = true
			};
			LevelManager.AddPlane(Splash);
			Top = (Plane.Position + Plane.Scale * Plane.Size / 2f).Dot(Vector3.UnitY);
			TerminalPosition = Plane.Position - Plane.Scale * Plane.Size / 2f * Vector3.UnitY + Vector3.Transform(Vector3.UnitZ, plane.Rotation) / 16f;
			foreach (BackgroundPlane item in LevelManager.BackgroundPlanes.Values.Where((BackgroundPlane x) => x.AttachedPlane == plane.Id && FezMath.AlmostEqual(Vector3.Transform(Vector3.UnitZ, plane.Rotation).Y, 0f)))
			{
				AttachedPlanes.Add(item);
			}
			Vector3 position = ((LevelManager.WaterType == LiquidType.None) ? (Top * Vector3.UnitY + Plane.Position * FezMath.XZMask) : (TerminalPosition * FezMath.XZMask + LevelManager.WaterHeight * Vector3.UnitY));
			Waiters.Wait(RandomHelper.Between(0.0, 1.0), delegate
			{
				waterfallState.BubblingEmitter = waterfallState.Host.SewageFallSound.EmitAt(position, loop: true, RandomHelper.Centered(0.025), 0f);
			});
		}

		public void Update(TimeSpan elapsed)
		{
			float num = LevelManager.WaterHeight - 0.5f;
			if (BubblingEmitter != null)
			{
				bool flag = !GameState.FarawaySettings.InTransition && !PlayerManager.Action.IsEnteringDoor();
				sinceAlive = FezMath.Saturate(sinceAlive + (float)elapsed.TotalSeconds / 2f * (float)(flag ? 1 : (-1)));
			}
			if (TerminalPosition.Y <= num)
			{
				float num2 = Top - num;
				if (num2 <= 0f)
				{
					if (!Splash.Hidden)
					{
						PlaneParticleSystem particleSystem = ParticleSystem;
						bool enabled = (ParticleSystem.Visible = false);
						particleSystem.Enabled = enabled;
						Splash.Hidden = true;
						Plane.Hidden = true;
						foreach (BackgroundPlane attachedPlane in AttachedPlanes)
						{
							attachedPlane.Hidden = true;
						}
					}
					if (BubblingEmitter != null)
					{
						BubblingEmitter.VolumeFactor = 0f;
					}
					return;
				}
				if (Splash.Hidden)
				{
					PlaneParticleSystem particleSystem2 = ParticleSystem;
					bool enabled = (ParticleSystem.Visible = true);
					particleSystem2.Enabled = enabled;
					Splash.Hidden = false;
					Plane.Hidden = false;
					foreach (BackgroundPlane attachedPlane2 in AttachedPlanes)
					{
						attachedPlane2.Hidden = false;
					}
				}
				if (BubblingEmitter != null)
				{
					BubblingEmitter.VolumeFactor = FezMath.Saturate(num2 / 2f) * sinceAlive;
					if (LevelManager.WaterType != 0)
					{
						BubblingEmitter.Position = FezMath.XZMask * TerminalPosition + num * Vector3.UnitY;
					}
				}
				Splash.Position = new Vector3(TerminalPosition.X, num + Splash.Size.Y / 2f, TerminalPosition.Z);
				if (FezMath.AlmostEqual(lastDistToTop, num2, 0.0625f))
				{
					return;
				}
				foreach (BackgroundPlane attachedPlane3 in AttachedPlanes)
				{
					attachedPlane3.Scale = new Vector3(attachedPlane3.Scale.X, num2 / attachedPlane3.Size.Y, attachedPlane3.Scale.Z);
					attachedPlane3.Position = new Vector3(attachedPlane3.Position.X, num + num2 / 2f, attachedPlane3.Position.Z);
				}
				Plane.Scale = new Vector3(Plane.Scale.X, num2 / Plane.Size.Y, Plane.Scale.Z);
				Plane.Position = new Vector3(Plane.Position.X, num + num2 / 2f, Plane.Position.Z);
				lastDistToTop = num2;
			}
			else if (!Splash.Hidden)
			{
				Splash.Hidden = true;
			}
		}

		public void Dispose()
		{
			if (BubblingEmitter != null && !BubblingEmitter.Dead)
			{
				BubblingEmitter.Cue.Stop();
			}
		}
	}

	private readonly List<WaterfallState> Waterfalls = new List<WaterfallState>();

	private SoundEffect SewageFallSound;

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IPlaneParticleSystems PlaneParticleSystems { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public WaterfallsHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		SewageFallSound = CMProvider.Global.Load<SoundEffect>("Sounds/Sewer/SewageFall");
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		foreach (WaterfallState waterfall in Waterfalls)
		{
			waterfall.Dispose();
		}
		Waterfalls.Clear();
		BackgroundPlane[] array = LevelManager.BackgroundPlanes.Values.ToArray();
		foreach (BackgroundPlane backgroundPlane in array)
		{
			if (backgroundPlane.ActorType == ActorType.Waterfall || backgroundPlane.ActorType == ActorType.Trickle)
			{
				Vector3 vector = Vector3.Transform(backgroundPlane.Size * backgroundPlane.Scale * Vector3.UnitX / 2f, backgroundPlane.Rotation);
				Vector3 vector2 = Vector3.Transform(Vector3.UnitZ, backgroundPlane.Rotation);
				Vector3 vector3 = FezMath.XZMask - vector2.Abs();
				Vector3 vector4 = backgroundPlane.Position + backgroundPlane.Size * backgroundPlane.Scale * Vector3.UnitY / 2f - new Vector3(0f, 1f / 32f, 0f) - vector2 * 2f / 16f;
				PlaneParticleSystem planeParticleSystem = new PlaneParticleSystem(base.Game, 25, new PlaneParticleSystemSettings
				{
					SpawnVolume = new BoundingBox
					{
						Min = vector4 - vector,
						Max = vector4 + vector
					},
					Velocity = new VaryingVector3
					{
						Base = Vector3.Up * 1.6f + vector2 / 4f,
						Variation = Vector3.Up * 0.8f + vector2 / 4f + vector3 / 2f
					},
					Gravity = new Vector3(0f, -0.15f, 0f),
					SpawningSpeed = 5f,
					RandomizeSpawnTime = true,
					ParticleLifetime = 2f,
					FadeInDuration = 0f,
					FadeOutDuration = 0.1f,
					SizeBirth = new Vector3(0.0625f),
					ColorLife = ((LevelManager.WaterType == LiquidType.Sewer) ? new Color(215, 232, 148) : new Color(1f, 1f, 1f, 0.75f)),
					Texture = CMProvider.Global.Load<Texture2D>("Background Planes/white_square"),
					BlendingMode = BlendingMode.Alphablending,
					ClampToTrixels = true,
					Billboarding = true,
					FullBright = (LevelManager.WaterType == LiquidType.Sewer),
					UseCallback = true
				});
				if (LevelManager.WaterType == LiquidType.Sewer)
				{
					planeParticleSystem.DrawOrder = 20;
					planeParticleSystem.Settings.StencilMask = StencilMask.Level;
				}
				PlaneParticleSystems.Add(planeParticleSystem);
				Waterfalls.Add(new WaterfallState(backgroundPlane, planeParticleSystem, this));
			}
			else if (backgroundPlane.ActorType == ActorType.Drips)
			{
				Vector3 vector5 = new Vector3(backgroundPlane.Size.X, 0f, backgroundPlane.Size.X) / 2f;
				Vector3 vector6 = Vector3.Transform(Vector3.UnitZ, backgroundPlane.Rotation);
				Vector3 vector7 = FezMath.XZMask - vector6.Abs();
				Vector3 vector8 = backgroundPlane.Position - new Vector3(0f, 0.125f, 0f);
				bool num = backgroundPlane.Crosshatch || backgroundPlane.Billboard;
				PlaneParticleSystem planeParticleSystem2 = new PlaneParticleSystem(base.Game, 25, new PlaneParticleSystemSettings
				{
					SpawnVolume = new BoundingBox
					{
						Min = vector8 - vector5,
						Max = vector8 + vector5
					},
					Velocity = new VaryingVector3
					{
						Base = Vector3.Zero,
						Variation = Vector3.Zero
					},
					Gravity = new Vector3(0f, -0.15f, 0f),
					SpawningSpeed = 2f,
					RandomizeSpawnTime = true,
					ParticleLifetime = 2f,
					FadeInDuration = 0f,
					FadeOutDuration = 0f,
					SizeBirth = new Vector3(0.0625f),
					ColorLife = ((LevelManager.WaterType == LiquidType.Sewer) ? new Color(215, 232, 148) : Color.White),
					Texture = CMProvider.Global.Load<Texture2D>("Background Planes/white_square"),
					BlendingMode = BlendingMode.Alphablending,
					ClampToTrixels = true,
					FullBright = true
				});
				if (LevelManager.WaterType == LiquidType.Sewer)
				{
					planeParticleSystem2.DrawOrder = 20;
					planeParticleSystem2.Settings.StencilMask = StencilMask.Level;
				}
				if (num)
				{
					planeParticleSystem2.Settings.Billboarding = true;
					planeParticleSystem2.Settings.SpawnVolume = new BoundingBox
					{
						Min = vector8 - vector5,
						Max = vector8 + vector5
					};
				}
				else
				{
					planeParticleSystem2.Settings.Doublesided = backgroundPlane.Doublesided;
					planeParticleSystem2.Settings.SpawnVolume = new BoundingBox
					{
						Min = vector8 - vector5 * vector7,
						Max = vector8 + vector5 * vector7
					};
					planeParticleSystem2.Settings.Orientation = FezMath.OrientationFromDirection(vector6);
				}
				PlaneParticleSystems.Add(planeParticleSystem2);
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading)
		{
			return;
		}
		foreach (WaterfallState waterfall in Waterfalls)
		{
			waterfall.Update(gameTime.ElapsedGameTime);
		}
	}
}
