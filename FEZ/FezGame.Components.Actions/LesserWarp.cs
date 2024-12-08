using System;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.Actions;

internal class LesserWarp : PlayerAction
{
	private enum Phases
	{
		None,
		Rise,
		SpinWait,
		Lower,
		Accelerate,
		Warping,
		Decelerate,
		FadeOut,
		LevelChange,
		FadeIn
	}

	private const float FadeSeconds = 2.5f;

	private ArtObjectInstance GateAo;

	private Vector3 OriginalPosition;

	private Mesh MaskMesh;

	private Texture2D WhiteTexture;

	private Texture2D StarTexture;

	private Phases Phase;

	private float GateAngle;

	private float GateTurnSpeed;

	private Vector3 RiseAxis;

	private float RisePhi;

	private float RiseStep;

	private TimeSpan SinceRisen;

	private TimeSpan SinceStarted;

	private TrileInstance CubeShard;

	private Vector3 OriginalCenter;

	private PlaneParticleSystem particles;

	private Mesh rgbPlanes;

	private float sinceInitialized;

	private SoundEffect sRise;

	private SoundEffect sLower;

	private SoundEffect sActivate;

	private SoundEmitter eIdleSpin;

	private IWaiter fader;

	[ServiceDependency]
	public IDotManager DotManager { private get; set; }

	[ServiceDependency]
	public ISpeechBubbleManager SpeechBubble { private get; set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { private get; set; }

	[ServiceDependency]
	public IPlaneParticleSystems PlaneParticleSystems { private get; set; }

	public LesserWarp(Game game)
		: base(game)
	{
		base.DrawOrder = 901;
	}

	public override void Initialize()
	{
		base.Initialize();
		base.LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		if (eIdleSpin != null && eIdleSpin.Cue != null && !eIdleSpin.Cue.IsDisposed && eIdleSpin.Cue.State != SoundState.Stopped)
		{
			eIdleSpin.Cue.Stop();
		}
		eIdleSpin = null;
		rgbPlanes = null;
		particles = null;
		Phase = Phases.None;
		sinceInitialized = 0f;
		GateAo = base.LevelManager.ArtObjects.Values.FirstOrDefault((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.LesserGate);
		if (GateAo != null)
		{
			InitializeRgbGate();
			MaskMesh = new Mesh
			{
				DepthWrites = false,
				Texture = WhiteTexture,
				Rotation = Quaternion.CreateFromAxisAngle(Vector3.Right, -(float)Math.PI / 2f)
			};
			DrawActionScheduler.Schedule(delegate
			{
				MaskMesh.Effect = new DefaultEffect.Textured
				{
					Fullbright = true
				};
			});
			MaskMesh.AddFace(new Vector3(2f), Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true, doublesided: true);
			MaskMesh.BakeTransformWithNormal<FezVertexPositionNormalTexture>();
			MaskMesh.Position = GateAo.Position - Vector3.UnitY * 1.25f;
			CubeShard = base.LevelManager.Triles.Values.FirstOrDefault((TrileInstance x) => x.Trile.ActorSettings.Type.IsCubeShard() && Vector3.Distance(x.Center, GateAo.Position) < 3f);
			OriginalPosition = GateAo.Position;
			sRise = base.CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Zu/LesserWarpRise");
			sLower = base.CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Zu/LesserWarpLower");
			sActivate = base.CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Zu/WarpGateActivate");
			eIdleSpin = base.CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Zu/LesserWarpIdleSpin").EmitAt(GateAo.Position, loop: true, paused: true);
		}
	}

	private void InitializeRgbGate()
	{
		PlaneParticleSystems.Add(particles = new PlaneParticleSystem(base.Game, 200, new PlaneParticleSystemSettings
		{
			SpawnVolume = new BoundingBox
			{
				Min = GateAo.Position + new Vector3(-2f, -3.5f, -2f),
				Max = GateAo.Position + new Vector3(2f, 2f, 2f)
			},
			Velocity = 
			{
				Base = new Vector3(0f, 0.6f, 0f),
				Variation = new Vector3(0f, 0.1f, 0.1f)
			},
			SpawningSpeed = 5f,
			ParticleLifetime = 6f,
			Acceleration = 0.375f,
			SizeBirth = new Vector3(0.25f, 0.25f, 0.25f),
			ColorBirth = Color.Black,
			ColorLife = Color.Black,
			ColorDeath = Color.Black,
			FullBright = true,
			RandomizeSpawnTime = true,
			Billboarding = true,
			BlendingMode = BlendingMode.Additive
		}));
		DrawActionScheduler.Schedule(delegate
		{
			particles.Settings.Texture = base.CMProvider.Global.Load<Texture2D>("Background Planes/dust_particle");
			particles.RefreshTexture();
		});
		rgbPlanes = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = true,
			SamplerState = SamplerState.LinearClamp,
			Blending = BlendingMode.Additive
		};
		DrawActionScheduler.Schedule(delegate
		{
			rgbPlanes.Effect = new DefaultEffect.Textured();
			rgbPlanes.Texture = base.CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/rgb_gradient");
		});
		PlaneParticleSystem planeParticleSystem = particles;
		bool enabled = (particles.Visible = false);
		planeParticleSystem.Enabled = enabled;
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
		rgbPlanes.Position = GateAo.Position + new Vector3(0f, -2f, 0f);
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		WhiteTexture = base.CMProvider.Global.Load<Texture2D>("Other Textures/FullWhite");
		StarTexture = base.CMProvider.Global.Load<Texture2D>("Other Textures/black_hole/Stars");
		LightingPostProcess.DrawOnTopLights += DrawLights;
	}

	protected override void TestConditions()
	{
		if (base.PlayerManager.Action == ActionType.LesserWarp || GateAo == null || Phase == Phases.FadeIn)
		{
			return;
		}
		Vector3 b = base.CameraManager.Viewpoint.SideMask();
		Vector3 a = (OriginalPosition - base.PlayerManager.Position).Abs();
		bool flag = a.Dot(b) < 3f && a.Y < 4f && CubeShard == null;
		if (base.LevelManager.Name == "ZU_CODE_LOOP" && base.GameState.SaveData.ThisLevel.ScriptingState == "NOT_COLLECTED")
		{
			flag = false;
		}
		if (flag && (Phase == Phases.None || Phase == Phases.Lower))
		{
			if (Phase != Phases.Lower)
			{
				RiseAxis = base.CameraManager.Viewpoint.RightVector();
				RisePhi = base.CameraManager.Viewpoint.ToPhi();
				SinceRisen = TimeSpan.Zero;
				SinceStarted = TimeSpan.Zero;
			}
			Phase = Phases.Rise;
			sRise.EmitAt(GateAo.Position);
		}
		else if (!flag && Phase != Phases.Lower && Phase != 0)
		{
			if (Phase != Phases.Rise)
			{
				RiseAxis = base.CameraManager.Viewpoint.RightVector();
				RisePhi = base.CameraManager.Viewpoint.ToPhi();
			}
			Phase = ((Phase == Phases.Rise) ? Phases.Lower : Phases.Decelerate);
			SinceStarted = TimeSpan.Zero;
			if (Phase == Phases.Lower)
			{
				sLower.EmitAt(GateAo.Position);
			}
		}
		else if (!flag && Phase == Phases.None && eIdleSpin != null && !eIdleSpin.Dead && eIdleSpin.Cue.State == SoundState.Playing)
		{
			eIdleSpin.Cue.Pause();
		}
		if (CubeShard != null && base.PlayerManager.LastAction == ActionType.FindingTreasure && CubeShard.Removed)
		{
			CubeShard = null;
		}
		if (!(base.PlayerManager.Grounded && flag) || base.InputManager.Up != FezButtonState.Pressed || !SpeechBubble.Hidden)
		{
			return;
		}
		base.PlayerManager.Action = ActionType.LesserWarp;
		if (Phase == Phases.None)
		{
			RiseAxis = base.CameraManager.Viewpoint.RightVector();
			RisePhi = base.CameraManager.Viewpoint.ToPhi();
			SinceRisen = TimeSpan.Zero;
			SinceStarted = TimeSpan.Zero;
			Phase = Phases.Rise;
			sRise.EmitAt(GateAo.Position);
		}
		else
		{
			if (Phase != Phases.SpinWait)
			{
				return;
			}
			if (eIdleSpin.Cue.State == SoundState.Playing)
			{
				if (fader != null)
				{
					fader.Cancel();
				}
				eIdleSpin.FadeOutAndPause(1f);
			}
			DotManager.PreventPoI = true;
			DotManager.Burrow();
			foreach (SoundEmitter emitter in base.SoundManager.Emitters)
			{
				emitter.FadeOutAndDie(2f);
			}
			base.SoundManager.FadeFrequencies(interior: true, 2f);
			base.SoundManager.FadeVolume(base.SoundManager.MusicVolumeFactor, 0f, 3f);
			sActivate.EmitAt(GateAo.Position);
			Phase = Phases.Accelerate;
			base.CameraManager.Constrained = true;
			OriginalCenter = base.CameraManager.Center;
			base.PlayerManager.LookingDirection = HorizontalDirection.Left;
			base.PlayerManager.Velocity = Vector3.Zero;
		}
	}

	protected override void Begin()
	{
		base.Begin();
		SinceStarted = TimeSpan.Zero;
	}

	public override void Update(GameTime gameTime)
	{
		if (!base.GameState.Loading)
		{
			base.Update(gameTime);
			if (rgbPlanes != null)
			{
				rgbPlanes.Rotation = base.CameraManager.Rotation;
			}
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		sinceInitialized += (float)elapsed.TotalSeconds;
		if (rgbPlanes != null)
		{
			float num = 0.25f + ((Phase == Phases.Lower || Phase == Phases.Rise || Phase == Phases.FadeOut) ? RiseStep : 1f) * 0.75f;
			double totalSeconds = SinceStarted.TotalSeconds;
			for (int i = 0; i < 3; i++)
			{
				float num2 = (Easing.EaseIn(FezMath.Saturate((totalSeconds - 3.0) / 1.25), EasingType.Decic) * 0.9f + Easing.EaseIn(FezMath.Saturate((totalSeconds - 1.0) / 2.0), EasingType.Cubic) * 0.1f) * base.CameraManager.Radius / 2f;
				float num3 = 0f;
				float amount = Easing.EaseIn(FezMath.Saturate((totalSeconds - 2.0) / 2.0), EasingType.Septic);
				rgbPlanes.Groups[i].Position = (float)Math.Sin(sinceInitialized * 1.5f + (float)i * ((float)Math.PI / 2f)) * 0.375f * Vector3.UnitY + (float)(i - 1) * 0.001f * Vector3.UnitZ + MathHelper.Lerp((float)Math.Cos(sinceInitialized * 0.5f + (float)i * ((float)Math.PI / 2f) + Easing.EaseIn(totalSeconds, EasingType.Quadratic)) * (num2 / 10f + 0.75f), 0f, amount) * Vector3.UnitX - num2 * Vector3.UnitY * 1.125f + (i - 1) * Vector3.UnitY * 0.3f;
				if (Phase < Phases.Decelerate)
				{
					amount = Easing.EaseIn(FezMath.Saturate((totalSeconds - 3.0) / 1.375), EasingType.Decic) * 0.6f + Easing.EaseIn(FezMath.Saturate((totalSeconds - 1.0) / 2.0), EasingType.Cubic) * 0.4f;
					float value = 1f + num3;
					value = MathHelper.Lerp(value, 0.0625f, amount);
					rgbPlanes.Groups[i].Scale = new Vector3(value, num + num2, value);
				}
			}
		}
		switch (Phase)
		{
		case Phases.Rise:
		{
			PlaneParticleSystem planeParticleSystem = particles;
			bool enabled = (particles.Visible = true);
			planeParticleSystem.Enabled = enabled;
			SinceRisen += elapsed;
			RiseStep = Easing.EaseInOut(FezMath.Saturate((float)SinceRisen.TotalSeconds / 2f), EasingType.Sine);
			GateAo.Position = Vector3.Lerp(OriginalPosition, OriginalPosition + Vector3.UnitY, RiseStep);
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(RiseAxis, RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Position = GateAo.Position - Vector3.UnitY * 1.25f * (float)Math.Cos(RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Rotation = GateAo.Rotation;
			if (base.PlayerManager.Action == ActionType.LesserWarp)
			{
				base.PlayerManager.Position = Vector3.Lerp(base.PlayerManager.Position, GateAo.Position - base.CameraManager.Viewpoint.ForwardVector() * 3f, 0.025f);
			}
			Vector3 vector = Vector3.Transform(new Vector3((float)Math.Cos(GateAngle), 0f, (float)Math.Sin((float)Math.PI + GateAngle)), Quaternion.CreateFromAxisAngle(Vector3.Up, RisePhi - (float)Math.PI / 2f));
			GateAo.Position += vector * 1.25f * (float)Math.Sin(RiseStep * ((float)Math.PI / 2f));
			if (!((float)SinceRisen.TotalSeconds > 1.5f))
			{
				break;
			}
			Phase = ((base.PlayerManager.Action == ActionType.LesserWarp) ? Phases.Accelerate : Phases.SpinWait);
			if (Phase == Phases.Accelerate)
			{
				sActivate.EmitAt(GateAo.Position);
				OriginalCenter = base.CameraManager.Center;
				base.CameraManager.Constrained = true;
				break;
			}
			eIdleSpin.VolumeFactor = 0f;
			fader = Waiters.Interpolate(1.0, delegate(float s)
			{
				eIdleSpin.VolumeFactor = s;
			}, delegate
			{
				fader = null;
			});
			fader.AutoPause = true;
			if (!eIdleSpin.Dead)
			{
				eIdleSpin.Cue.Resume();
			}
			break;
		}
		case Phases.SpinWait:
		{
			SinceRisen += elapsed;
			if (SinceRisen.TotalSeconds >= 2.0)
			{
				SinceRisen = TimeSpan.FromSeconds(2.0);
			}
			RiseStep = Easing.EaseInOut(FezMath.Saturate((float)SinceRisen.TotalSeconds / 2f), EasingType.Sine);
			GateAo.Position = Vector3.Lerp(OriginalPosition, OriginalPosition + Vector3.UnitY, RiseStep);
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(RiseAxis, RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Position = GateAo.Position - Vector3.UnitY * 1.25f * (float)Math.Cos(RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Rotation = GateAo.Rotation;
			GateTurnSpeed = MathHelper.Lerp(GateTurnSpeed, 0.015f, 0.075f);
			GateAngle = FezMath.WrapAngle(GateAngle + GateTurnSpeed);
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, GateAngle) * GateAo.Rotation;
			MaskMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, GateAngle) * MaskMesh.Rotation;
			Vector3 vector = Vector3.Transform(new Vector3((float)Math.Cos(GateAngle), 0f, (float)Math.Sin((float)Math.PI + GateAngle)), Quaternion.CreateFromAxisAngle(Vector3.Up, RisePhi - (float)Math.PI / 2f));
			GateAo.Position += vector * 1.25f * (float)Math.Sin(RiseStep * ((float)Math.PI / 2f));
			break;
		}
		case Phases.Lower:
		{
			SinceRisen -= elapsed;
			RiseStep = Easing.EaseInOut(FezMath.Saturate((float)SinceRisen.TotalSeconds / 2f), EasingType.Sine);
			GateAo.Position = Vector3.Lerp(OriginalPosition, OriginalPosition + Vector3.UnitY, RiseStep);
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(RiseAxis, RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Position = GateAo.Position - Vector3.UnitY * 1.25f * (float)Math.Cos(RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Rotation = GateAo.Rotation;
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, GateAngle) * GateAo.Rotation;
			MaskMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, GateAngle) * MaskMesh.Rotation;
			Vector3 vector = Vector3.Transform(new Vector3((float)Math.Cos(GateAngle), 0f, (float)Math.Sin((float)Math.PI + GateAngle)), Quaternion.CreateFromAxisAngle(Vector3.Up, RisePhi - (float)Math.PI / 2f));
			GateAo.Position += vector * 1.25f * (float)Math.Sin(RiseStep * ((float)Math.PI / 2f));
			if (SinceRisen.Ticks <= 0)
			{
				Phase = Phases.None;
			}
			break;
		}
		case Phases.Accelerate:
		case Phases.Warping:
		{
			DotManager.PreventPoI = true;
			SinceRisen += elapsed;
			if (SinceRisen.TotalSeconds >= 2.0)
			{
				SinceRisen = TimeSpan.FromSeconds(2.0);
			}
			SinceStarted += elapsed;
			base.PlayerManager.Animation.Timing.Update(elapsed, Math.Max((float)SinceStarted.TotalSeconds / 4f, 0f));
			RiseStep = Easing.EaseInOut(FezMath.Saturate((float)SinceRisen.TotalSeconds / 2f), EasingType.Sine);
			GateAo.Position = Vector3.Lerp(OriginalPosition, OriginalPosition + Vector3.UnitY, RiseStep);
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(RiseAxis, RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Position = GateAo.Position - Vector3.UnitY * 1.25f * (float)Math.Cos(RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Rotation = GateAo.Rotation;
			GateTurnSpeed *= 1.01f;
			GateTurnSpeed += (float)elapsed.TotalSeconds / 50f;
			GateAngle += GateTurnSpeed;
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, GateAngle) * GateAo.Rotation;
			MaskMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, GateAngle) * MaskMesh.Rotation;
			Vector3 vector2 = Vector3.UnitY * 15f * Easing.EaseIn((float)SinceStarted.TotalSeconds / 4.5f, EasingType.Decic);
			base.PlayerManager.Position = Vector3.Lerp(base.PlayerManager.Position, GateAo.Position - base.CameraManager.Viewpoint.ForwardVector() * 3f + vector2, 0.0375f);
			base.CameraManager.Center = OriginalCenter + vector2 * 0.4f;
			particles.Settings.Acceleration *= 1.0075f;
			particles.Settings.SpawningSpeed *= 1.0075f;
			particles.Settings.Velocity.Base *= 1.01f;
			particles.Settings.Velocity.Variation *= 1.01f;
			Vector3 vector = Vector3.Transform(new Vector3((float)Math.Cos(GateAngle), 0f, (float)Math.Sin((float)Math.PI + GateAngle)), Quaternion.CreateFromAxisAngle(Vector3.Up, RisePhi - (float)Math.PI / 2f));
			GateAo.Position += vector * 1.25f * (float)Math.Sin(RiseStep * ((float)Math.PI / 2f));
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
		{
			DotManager.PreventPoI = true;
			SinceStarted += elapsed;
			RiseStep = 1f;
			GateAo.Position = Vector3.Lerp(OriginalPosition, OriginalPosition + Vector3.UnitY, RiseStep);
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(RiseAxis, RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Position = GateAo.Position - Vector3.UnitY * 1.25f * (float)Math.Cos(RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Rotation = GateAo.Rotation;
			float num4 = ((GateAngle < (float)Math.PI) ? 0f : ((float)Math.PI * 2f));
			GateTurnSpeed = FezMath.Saturate(GateTurnSpeed - 1f / 120f);
			GateAngle += GateTurnSpeed;
			if (GateTurnSpeed < 0.05f)
			{
				GateAngle = MathHelper.Lerp(GateAngle, num4, 0.05f);
			}
			else
			{
				GateAngle = FezMath.WrapAngle(GateAngle);
			}
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, GateAngle) * GateAo.Rotation;
			MaskMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, GateAngle) * MaskMesh.Rotation;
			if (base.PlayerManager.Action == ActionType.LesserWarp)
			{
				base.PlayerManager.Position = Vector3.Lerp(base.PlayerManager.Position, GateAo.Position - base.CameraManager.Viewpoint.ForwardVector() * 3f, 0.025f);
			}
			Vector3 vector = Vector3.Transform(new Vector3((float)Math.Cos(GateAngle), 0f, (float)Math.Sin((float)Math.PI + GateAngle)), Quaternion.CreateFromAxisAngle(Vector3.Up, RisePhi - (float)Math.PI / 2f));
			GateAo.Position += vector * 1.25f * (float)Math.Sin(RiseStep * ((float)Math.PI / 2f));
			if (!(SinceStarted.TotalSeconds > 2.0) && (!FezMath.AlmostEqual(GateTurnSpeed, 0f) || !FezMath.AlmostEqual(GateAngle, num4, 0.1)))
			{
				break;
			}
			GateAngle = num4;
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, GateAngle) * GateAo.Rotation;
			Phase = ((base.PlayerManager.Action == ActionType.LesserWarp) ? Phases.FadeOut : Phases.Lower);
			SinceStarted = TimeSpan.Zero;
			if (!eIdleSpin.Dead && eIdleSpin.Cue.State == SoundState.Playing)
			{
				if (fader != null)
				{
					fader.Cancel();
				}
				eIdleSpin.FadeOutAndPause(1f);
			}
			if (base.PlayerManager.Action != ActionType.LesserWarp)
			{
				sLower.EmitAt(GateAo.Position);
			}
			break;
		}
		case Phases.FadeOut:
		case Phases.LevelChange:
		{
			DotManager.PreventPoI = true;
			SinceStarted += elapsed;
			RiseStep = 1f - Easing.EaseInOut(FezMath.Saturate((float)SinceStarted.TotalSeconds / 1.75f), EasingType.Sine);
			GateAo.Position = Vector3.Lerp(OriginalPosition, OriginalPosition + Vector3.UnitY, RiseStep);
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(RiseAxis, RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Position = GateAo.Position - Vector3.UnitY * 1.25f * (float)Math.Cos(RiseStep * ((float)Math.PI / 2f));
			MaskMesh.Rotation = GateAo.Rotation;
			GateAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, GateAngle) * GateAo.Rotation;
			MaskMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, GateAngle) * MaskMesh.Rotation;
			base.PlayerManager.Position = Vector3.Lerp(base.PlayerManager.Position, GateAo.Position - base.CameraManager.Viewpoint.ForwardVector() * 3f, 0.025f);
			Vector3 vector = Vector3.Transform(new Vector3((float)Math.Cos(GateAngle), 0f, (float)Math.Sin((float)Math.PI + GateAngle)), Quaternion.CreateFromAxisAngle(Vector3.Up, RisePhi - (float)Math.PI / 2f));
			GateAo.Position += vector * 1.25f * (float)Math.Sin(RiseStep * ((float)Math.PI / 2f));
			if (Phase != Phases.LevelChange && SinceStarted.TotalSeconds > 2.5)
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
		}
		case Phases.FadeIn:
			SinceStarted += elapsed;
			if (SinceStarted.TotalSeconds > 2.5)
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
		base.LevelManager.ChangeLevel(GateAo.ActorSettings.DestinationLevel);
		Phase = Phases.FadeIn;
		base.GameState.SaveData.Ground = base.LevelManager.ArtObjects.Values.First((ArtObjectInstance x) => x.ArtObject.ActorType == ActorType.WarpGate).Position + Vector3.Down + base.GameState.SaveData.View.VisibleOrientation().AsVector() * 2f;
		DotManager.PreventPoI = false;
		base.PlayerManager.Hidden = false;
		base.PlayerManager.CheckpointGround = null;
		base.PlayerManager.RespawnAtCheckpoint();
		base.CameraManager.Center = base.PlayerManager.Position + Vector3.Up * base.PlayerManager.Size.Y / 2f + Vector3.UnitY;
		base.CameraManager.SnapInterpolation();
		base.LevelMaterializer.CullInstances();
		base.GameState.ScheduleLoadEnd = true;
		SinceStarted = TimeSpan.Zero;
	}

	public override void Draw(GameTime gameTime)
	{
		if (base.GameState.Loading || base.GameState.Paused || !IsActionAllowed(base.PlayerManager.Action))
		{
			return;
		}
		if (Phase != Phases.LevelChange && Phase != Phases.FadeIn)
		{
			base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.WarpGate);
			MaskMesh.Draw();
			base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.WarpGate);
			float viewScale = base.GraphicsDevice.GetViewScale();
			float num = base.CameraManager.Radius / ((float)StarTexture.Width / 16f) / viewScale;
			float num2 = base.CameraManager.Radius / base.CameraManager.AspectRatio / ((float)StarTexture.Height / 16f) / viewScale;
			Matrix textureMatrix = new Matrix(num, 0f, 0f, 0f, 0f, num2, 0f, 0f, (0f - num) / 2f, (0f - num2) / 2f, 1f, 0f, 0f, 0f, 0f, 1f);
			base.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
			TargetRenderer.DrawFullscreen(StarTexture, textureMatrix);
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
		}
		if (rgbPlanes != null && Phase <= Phases.Decelerate)
		{
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			float num3 = ((Phase == Phases.Lower || Phase == Phases.Rise) ? RiseStep : 1f) * (1f - (float)(int)base.LevelManager.ActualDiffuse.R / 512f) * 0.8f;
			float amount = ((Phase == Phases.Decelerate || Phase == Phases.Lower || Phase == Phases.FadeOut) ? 1f : 0.01f);
			if (Phase == Phases.Accelerate)
			{
				num3 = 1f;
			}
			for (int i = 0; i < 3; i++)
			{
				rgbPlanes.Groups[i].Material.Diffuse = Vector3.Lerp(rgbPlanes.Groups[i].Material.Diffuse, new Vector3((i == 0) ? num3 : 0f, (i == 1) ? num3 : 0f, (i == 2) ? num3 : 0f), amount);
				rgbPlanes.Groups[i].Material.Opacity = MathHelper.Lerp(rgbPlanes.Groups[i].Material.Opacity, num3, amount);
			}
			rgbPlanes.Draw();
		}
		if (Phase == Phases.FadeOut || Phase == Phases.FadeIn || Phase == Phases.LevelChange)
		{
			double num4 = SinceStarted.TotalSeconds / 2.5;
			if (Phase == Phases.FadeIn)
			{
				num4 = 1.0 - num4;
			}
			float alpha = FezMath.Saturate(Easing.EaseIn(num4, EasingType.Cubic));
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			TargetRenderer.DrawFullscreen(new Color(0f, 0f, 0f, alpha));
		}
	}

	private void DrawLights()
	{
		if (!base.GameState.Loading && !base.GameState.Paused && IsActionAllowed(base.PlayerManager.Action) && base.LevelManager.WaterType != LiquidType.Sewer)
		{
			float num = ((Phase == Phases.Lower || Phase == Phases.Rise || Phase == Phases.FadeOut) ? RiseStep : 1f);
			if (rgbPlanes != null && Phase <= Phases.Decelerate)
			{
				particles.Settings.ColorLife.Base = new Color(num / 2f, num / 2f, num / 2f, 1f);
				particles.Settings.ColorLife.Variation = new Color(num / 2f, num / 2f, num / 2f, 1f);
				bool depthBufferWriteEnable = base.GraphicsDevice.GetDssCombiner().DepthBufferWriteEnable;
				StencilOperation stencilPass = base.GraphicsDevice.GetDssCombiner().StencilPass;
				base.GraphicsDevice.GetDssCombiner().DepthBufferWriteEnable = false;
				base.GraphicsDevice.GetDssCombiner().StencilPass = StencilOperation.Keep;
				(rgbPlanes.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
				rgbPlanes.Draw();
				(rgbPlanes.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
				base.GraphicsDevice.GetDssCombiner().DepthBufferWriteEnable = depthBufferWriteEnable;
				base.GraphicsDevice.GetDssCombiner().StencilPass = stencilPass;
			}
			(MaskMesh.Effect as DefaultEffect).Pass = LightingEffectPass.Pre;
			MaskMesh.Material.Opacity = num;
			MaskMesh.Draw();
			MaskMesh.Material.Opacity = 1f;
			(MaskMesh.Effect as DefaultEffect).Pass = LightingEffectPass.Main;
		}
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.LesserWarp)
		{
			return Phase != Phases.None;
		}
		return true;
	}
}
