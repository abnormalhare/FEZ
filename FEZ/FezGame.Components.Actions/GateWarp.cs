using System;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.Actions;

internal class GateWarp : PlayerAction
{
	private enum Phases
	{
		None,
		Rise,
		Accelerate,
		Warping,
		Decelerate,
		FadeOut,
		LevelChange,
		FadeIn
	}

	private const float FadeSeconds = 2.25f;

	private ArtObjectInstance GateAo;

	private Phases Phase;

	private TimeSpan SinceStarted;

	private TimeSpan SinceRisen;

	private SoundEffect WarpSound;

	private PlaneParticleSystem particles;

	private Mesh rgbPlanes;

	private float sinceInitialized;

	private Vector3 originalCenter;

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { private get; set; }

	[ServiceDependency]
	public IPlaneParticleSystems PlaneParticleSystems { private get; set; }

	public GateWarp(Game game)
		: base(game)
	{
		base.DrawOrder = 901;
	}

	public override void Initialize()
	{
		base.Initialize();
		LightingPostProcess.DrawOnTopLights += DrawLights;
		WarpSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Zu/WarpGateActivate");
	}

	private void InitializeRgbGate()
	{
		PlaneParticleSystems.Add(particles = new PlaneParticleSystem(base.Game, 400, new PlaneParticleSystemSettings
		{
			SpawnVolume = new BoundingBox
			{
				Min = GateAo.Position + new Vector3(-3.25f, -6f, -3.25f),
				Max = GateAo.Position + new Vector3(3.25f, -2f, 3.25f)
			},
			Velocity = 
			{
				Base = new Vector3(0f, 0.6f, 0f),
				Variation = new Vector3(0f, 0.1f, 0.1f)
			},
			SpawningSpeed = 7f,
			ParticleLifetime = 6f,
			Acceleration = 0.375f,
			SizeBirth = new Vector3(0.25f, 0.25f, 0.25f),
			ColorBirth = Color.Black,
			ColorLife = 
			{
				Base = new Color(0.5f, 0.5f, 0.5f, 1f),
				Variation = new Color(0.5f, 0.5f, 0.5f, 1f)
			},
			ColorDeath = Color.Black,
			FullBright = true,
			RandomizeSpawnTime = true,
			Billboarding = true,
			Texture = base.CMProvider.Global.Load<Texture2D>("Background Planes/dust_particle"),
			BlendingMode = BlendingMode.Additive
		}));
		rgbPlanes = new Mesh
		{
			Effect = new DefaultEffect.Textured(),
			DepthWrites = false,
			AlwaysOnTop = true,
			SamplerState = SamplerState.LinearClamp,
			Blending = BlendingMode.Additive,
			Texture = base.CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/rgb_gradient")
		};
		rgbPlanes.AddFace(new Vector3(1f, 4.5f, 1f), new Vector3(0f, 3f, 0f), FaceOrientation.Front, centeredOnOrigin: true).Material = new Material
		{
			Diffuse = Vector3.Zero,
			Opacity = 0f
		};
		rgbPlanes.AddFace(new Vector3(1f, 4.5f, 1f), new Vector3(0f, 3f, 0f), FaceOrientation.Front, centeredOnOrigin: true).Material = new Material
		{
			Diffuse = Vector3.Zero,
			Opacity = 0f
		};
		rgbPlanes.AddFace(new Vector3(1f, 4.5f, 1f), new Vector3(0f, 3f, 0f), FaceOrientation.Front, centeredOnOrigin: true).Material = new Material
		{
			Diffuse = Vector3.Zero,
			Opacity = 0f
		};
	}

	protected override void Begin()
	{
		base.Begin();
		SinceStarted = TimeSpan.Zero;
		base.PlayerManager.LookingDirection = HorizontalDirection.Left;
		SinceRisen = TimeSpan.Zero;
		Phase = Phases.Rise;
		foreach (SoundEmitter emitter in base.SoundManager.Emitters)
		{
			emitter.FadeOutAndDie(2f);
		}
		base.SoundManager.FadeFrequencies(interior: true, 2f);
		base.SoundManager.FadeVolume(base.SoundManager.MusicVolumeFactor, 0f, 3f);
		WarpSound.EmitAt(base.PlayerManager.Position);
		rgbPlanes = null;
		particles = null;
		sinceInitialized = 0f;
		originalCenter = base.CameraManager.Center;
		base.CameraManager.Constrained = true;
		GateAo = base.LevelManager.ArtObjects.Values.FirstOrDefault((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.WarpGate);
		if (GateAo != null && base.GameState.SaveData.UnlockedWarpDestinations.Count > 1)
		{
			InitializeRgbGate();
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		Vector3 position = GateAo.Position;
		if (GateAo.ArtObject != null && GateAo.ArtObject.Size.Y == 8f)
		{
			position -= new Vector3(0f, 1f, 0f);
		}
		sinceInitialized += (float)elapsed.TotalSeconds;
		if (rgbPlanes != null)
		{
			rgbPlanes.Rotation = base.CameraManager.Rotation;
			rgbPlanes.Position = position + new Vector3(0f, -2f, 0f);
			double totalSeconds = SinceStarted.TotalSeconds;
			for (int i = 0; i < 3; i++)
			{
				float num = (Easing.EaseIn(FezMath.Saturate((totalSeconds - 3.0) / 1.25), EasingType.Decic) * 0.9f + Easing.EaseIn(FezMath.Saturate((totalSeconds - 1.0) / 2.0), EasingType.Cubic) * 0.1f) * base.CameraManager.Radius / 2f;
				float num2 = 0f;
				float amount = Easing.EaseIn(FezMath.Saturate((totalSeconds - 2.0) / 2.0), EasingType.Septic);
				rgbPlanes.Groups[i].Position = (float)Math.Sin(sinceInitialized * 1.5f + (float)i * ((float)Math.PI / 2f)) * 0.375f * Vector3.UnitY + (float)(i - 1) * 0.001f * Vector3.UnitZ + MathHelper.Lerp((float)Math.Cos(sinceInitialized * 0.5f + (float)i * ((float)Math.PI / 2f) + Easing.EaseIn(totalSeconds, EasingType.Quadratic)) * (num / 10f + 0.75f), 0f, amount) * Vector3.UnitX - num * Vector3.UnitY * 1.125f + (i - 1) * Vector3.UnitY * 0.3f;
				if (Phase < Phases.Decelerate)
				{
					amount = Easing.EaseIn(FezMath.Saturate((totalSeconds - 3.0) / 1.375), EasingType.Decic) * 0.6f + Easing.EaseIn(FezMath.Saturate((totalSeconds - 1.0) / 2.0), EasingType.Cubic) * 0.4f;
					float value = 1f + num2;
					value = MathHelper.Lerp(value, 0.0625f, amount);
					rgbPlanes.Groups[i].Scale = new Vector3(value, 1f + num, value);
				}
			}
		}
		switch (Phase)
		{
		case Phases.Rise:
			Phase = Phases.Accelerate;
			break;
		case Phases.Accelerate:
		case Phases.Warping:
		{
			SinceRisen += elapsed;
			SinceStarted += elapsed;
			base.PlayerManager.Animation.Timing.Update(elapsed, Math.Max((float)SinceRisen.TotalSeconds / 2f, 0f));
			Vector3 vector = Vector3.UnitY * 15f * Easing.EaseIn((float)SinceStarted.TotalSeconds / 4.5f, EasingType.Decic);
			base.PlayerManager.Position = Vector3.Lerp(base.PlayerManager.Position, GateAo.Position - base.CameraManager.Viewpoint.ForwardVector() * 3f + vector, 0.0375f);
			base.CameraManager.Center = originalCenter + vector * 0.4f;
			particles.Settings.Acceleration *= 1.0075f;
			particles.Settings.SpawningSpeed *= 1.0075f;
			particles.Settings.Velocity.Base *= 1.01f;
			particles.Settings.Velocity.Variation *= 1.0115f;
			if (Phase != Phases.Warping && SinceStarted.TotalSeconds > 4.0)
			{
				Phase = Phases.Warping;
				ScreenFade obj = new ScreenFade(ServiceHelper.Game)
				{
					FromColor = ColorEx.TransparentWhite,
					ToColor = Color.White,
					Duration = 0.5f
				};
				ServiceHelper.AddComponent(obj);
				obj.Faded = (Action)Delegate.Combine(obj.Faded, (Action)delegate
				{
					base.PlayerManager.Hidden = true;
					SinceStarted = TimeSpan.Zero;
					Phase = Phases.Decelerate;
					particles.FadeOutAndDie(1f);
					rgbPlanes = null;
					ServiceHelper.AddComponent(new ScreenFade(ServiceHelper.Game)
					{
						FromColor = Color.White,
						ToColor = ColorEx.TransparentWhite,
						Duration = 1f
					});
				});
			}
			break;
		}
		case Phases.Decelerate:
			SinceStarted += elapsed;
			base.PlayerManager.Position = Vector3.Lerp(base.PlayerManager.Position, position - base.CameraManager.Viewpoint.ForwardVector() * 3f, 0.025f);
			if (SinceStarted.TotalSeconds > 1.0)
			{
				Phase = Phases.FadeOut;
				SinceStarted = TimeSpan.Zero;
			}
			break;
		case Phases.FadeOut:
		case Phases.LevelChange:
			SinceStarted += elapsed;
			base.PlayerManager.Position = Vector3.Lerp(base.PlayerManager.Position, position - base.CameraManager.Viewpoint.ForwardVector() * 3f, 0.025f);
			if (Phase != Phases.LevelChange && SinceStarted.TotalSeconds > 2.25)
			{
				Phase = Phases.LevelChange;
				SinceStarted = TimeSpan.Zero;
				base.GameState.Loading = true;
				Worker<bool> worker = ThreadPool.Take<bool>(DoLoad);
				worker.Finished += delegate
				{
					ThreadPool.Return(worker);
				};
				worker.Start(context: false);
			}
			break;
		case Phases.FadeIn:
			SinceStarted += elapsed;
			if (SinceStarted.TotalSeconds > 2.25)
			{
				SinceStarted = TimeSpan.Zero;
				Phase = Phases.None;
			}
			break;
		}
		return false;
	}

	private void DoLoad(bool dummy)
	{
		base.LevelManager.ChangeLevel(base.PlayerManager.WarpPanel.Destination);
		Phase = Phases.FadeIn;
		base.GameState.SaveData.View = base.PlayerManager.OriginWarpViewpoint;
		base.GameState.SaveData.Ground = base.LevelManager.ArtObjects.Values.First((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.WarpGate).Position - Vector3.UnitY + base.GameState.SaveData.View.VisibleOrientation().AsVector() * 2f;
		base.PlayerManager.CheckpointGround = null;
		base.PlayerManager.RespawnAtCheckpoint();
		base.CameraManager.Center = base.PlayerManager.Position + Vector3.Up * base.PlayerManager.Size.Y / 2f + Vector3.UnitY;
		base.CameraManager.SnapInterpolation();
		base.LevelMaterializer.CullInstances();
		base.PlayerManager.Hidden = false;
		base.GameState.ScheduleLoadEnd = true;
		SinceStarted = TimeSpan.Zero;
		base.PlayerManager.WarpPanel = null;
		particles = null;
	}

	public override void Draw(GameTime gameTime)
	{
		base.Draw(gameTime);
		if (!IsActionAllowed(base.PlayerManager.Action))
		{
			return;
		}
		if (rgbPlanes != null && Phase <= Phases.Decelerate)
		{
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			int num = 1;
			float amount = 0.01f;
			for (int i = 0; i < 3; i++)
			{
				rgbPlanes.Groups[i].Material.Diffuse = Vector3.Lerp(rgbPlanes.Groups[i].Material.Diffuse, new Vector3((i == 0) ? num : 0, (i == 1) ? num : 0, (i == 2) ? num : 0), amount);
				rgbPlanes.Groups[i].Material.Opacity = MathHelper.Lerp(rgbPlanes.Groups[i].Material.Opacity, num, amount);
			}
			rgbPlanes.Draw();
		}
		if (Phase == Phases.FadeOut || Phase == Phases.FadeIn || Phase == Phases.LevelChange)
		{
			double num2 = SinceStarted.TotalSeconds / 2.25;
			if (Phase == Phases.FadeIn)
			{
				num2 = 1.0 - num2;
			}
			float alpha = FezMath.Saturate(Easing.EaseIn(num2, EasingType.Cubic));
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			TargetRenderer.DrawFullscreen(new Color(0f, 0f, 0f, alpha));
		}
	}

	private void DrawLights()
	{
		if (IsActionAllowed(base.PlayerManager.Action) && base.LevelManager.WaterType != LiquidType.Sewer && rgbPlanes != null && Phase <= Phases.Decelerate)
		{
			(rgbPlanes.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
			rgbPlanes.Draw();
			(rgbPlanes.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
		}
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.GateWarp)
		{
			return Phase != Phases.None;
		}
		return true;
	}
}
