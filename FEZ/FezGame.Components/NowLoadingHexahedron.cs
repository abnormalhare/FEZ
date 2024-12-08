using System;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Components.Actions;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class NowLoadingHexahedron : DrawableGameComponent
{
	private enum Phases
	{
		VectorOutline,
		WireframeWaves,
		FillCube,
		FillDot,
		TurnTurnTurn,
		Rays,
		FadeToWhite,
		Load,
		FadeOut
	}

	public class Darkener : DrawableGameComponent
	{
		[ServiceDependency]
		public ITargetRenderingManager TargetRenderer { get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { get; set; }

		public Darkener(Game game)
			: base(game)
		{
			base.DrawOrder = 899;
		}

		public override void Draw(GameTime gameTime)
		{
			if (Phase != Phases.FadeOut)
			{
				TargetRenderer.DrawFullscreen(new Color(0f, 0f, 0f, FezMath.Saturate(Easing.EaseIn(SinceWavesStarted.TotalSeconds / 12.0, EasingType.Cubic)) * 0.375f * GameState.SkyOpacity));
			}
		}
	}

	private static TimeSpan SincePhaseStarted;

	private static TimeSpan SinceWavesStarted;

	private static TimeSpan SinceTurning;

	private readonly string ToLevel;

	private readonly Vector3 Center;

	private static Phases Phase;

	private Mesh Outline;

	private Mesh WireCube;

	private Mesh SolidCube;

	private Mesh Flare;

	private Mesh Rays;

	private int NextOutline = 1;

	private float OutlineIn;

	private float WhiteFillStep;

	private float Phi;

	private SoundEffect WarpSound;

	private Darkener TheDarkening;

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public ISpeechBubbleManager Speech { get; set; }

	[ServiceDependency]
	public IDotManager Dot { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { get; set; }

	[ServiceDependency]
	public IThreadPool ThreadPool { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency(Optional = true)]
	public IWalkToService WalkTo { protected get; set; }

	public NowLoadingHexahedron(Game game, Vector3 center, string toLevel)
		: base(game)
	{
		ToLevel = toLevel;
		Center = center;
		base.UpdateOrder = 10;
		base.DrawOrder = 901;
	}

	public override void Initialize()
	{
		base.Initialize();
		Phase = Phases.VectorOutline;
		SincePhaseStarted = (SinceWavesStarted = (SinceTurning = TimeSpan.Zero));
		PlayerManager.CanControl = false;
		WarpSound = CMProvider.GetForLevel(GameState.IsTrialMode ? "trial/ELDERS" : "ELDERS").Load<SoundEffect>("Sounds/Zu/HexaWarpIn");
		Dot.Hidden = false;
		Dot.Behaviour = DotHost.BehaviourType.ClampToTarget;
		Dot.Target = Center;
		Dot.ScalePulsing = 0f;
		Dot.Opacity = 0f;
		Waiters.Wait(() => PlayerManager.Grounded, delegate
		{
			WalkTo.Destination = () => PlayerManager.Position * Vector3.UnitY + Center * FezMath.XZMask;
			WalkTo.NextAction = ActionType.Idle;
			PlayerManager.Action = ActionType.WalkingTo;
		});
		TimeManager.TimeFactor = TimeManager.DefaultTimeFactor;
		Outline = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = true
		};
		Outline.AddWireframePolygon(Color.White, new Vector3(0f, 0.8660254f, 0f), new Vector3(0.7071068f, 0.2886752f, 0f), new Vector3(0.7071068f, -0.2886752f, 0f), new Vector3(0f, -0.8660254f, 0f), new Vector3(-0.7071068f, -0.2886752f, 0f), new Vector3(-0.7071068f, 0.2886752f, 0f), new Vector3(0f, 0.8660254f, 0f));
		Outline.Scale = new Vector3(4f);
		Outline.BakeTransform<FezVertexPositionColor>();
		Group firstGroup = Outline.FirstGroup;
		firstGroup.Material = new Material();
		firstGroup.Enabled = false;
		for (int i = 0; i < 1024; i++)
		{
			Outline.CloneGroup(firstGroup);
		}
		firstGroup.Enabled = true;
		WireCube = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = true,
			Material = 
			{
				Opacity = 0f
			}
		};
		WireCube.AddWireframeBox(Vector3.One * 4f, Vector3.Zero, Color.White, centeredOnOrigin: true);
		WireCube.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Right, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 4f);
		WireCube.BakeTransform<FezVertexPositionColor>();
		SolidCube = new Mesh
		{
			AlwaysOnTop = true,
			Material = 
			{
				Opacity = 0f
			}
		};
		SolidCube.AddFlatShadedBox(Vector3.One * 4f, Vector3.Zero, Color.White, centeredOnOrigin: true);
		SolidCube.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Right, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 4f);
		SolidCube.BakeTransform<VertexPositionNormalColor>();
		Flare = new Mesh
		{
			DepthWrites = false,
			Material = 
			{
				Opacity = 0f
			},
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.LinearClamp
		};
		Flare.AddFace(Vector3.One * 4f, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
		Rays = new Mesh
		{
			Blending = BlendingMode.Additive,
			SamplerState = SamplerState.AnisotropicClamp,
			DepthWrites = false,
			AlwaysOnTop = true
		};
		for (int j = 0; j < 128; j++)
		{
			float x = 0.75f;
			float num = 0.0075f;
			Group group = Rays.AddGroup();
			group.Geometry = new IndexedUserPrimitives<FezVertexPositionTexture>(new FezVertexPositionTexture[6]
			{
				new FezVertexPositionTexture(new Vector3(0f, num / 2f * 0.1f, 0f), new Vector2(0f, 0f)),
				new FezVertexPositionTexture(new Vector3(x, num / 2f, 0f), new Vector2(1f, 0f)),
				new FezVertexPositionTexture(new Vector3(x, num / 2f * 0.1f, 0f), new Vector2(1f, 0.45f)),
				new FezVertexPositionTexture(new Vector3(x, (0f - num) / 2f * 0.1f, 0f), new Vector2(1f, 0.55f)),
				new FezVertexPositionTexture(new Vector3(x, (0f - num) / 2f, 0f), new Vector2(1f, 1f)),
				new FezVertexPositionTexture(new Vector3(0f, (0f - num) / 2f * 0.1f, 0f), new Vector2(0f, 1f))
			}, new int[12]
			{
				0, 1, 2, 0, 2, 5, 5, 2, 3, 5,
				3, 4
			}, PrimitiveType.TriangleList);
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Forward, RandomHelper.Between(0.0, 6.2831854820251465));
			group.Material = new Material
			{
				Diffuse = new Vector3(0f)
			};
		}
		DrawActionScheduler.Schedule(delegate
		{
			Outline.Effect = new DefaultEffect.VertexColored
			{
				Fullbright = true,
				AlphaIsEmissive = false
			};
			WireCube.Effect = new DefaultEffect.VertexColored
			{
				Fullbright = true,
				AlphaIsEmissive = false
			};
			SolidCube.Effect = new DefaultEffect.LitVertexColored
			{
				Fullbright = false,
				AlphaIsEmissive = false
			};
			Flare.Effect = new DefaultEffect.Textured
			{
				Fullbright = true,
				AlphaIsEmissive = false
			};
			Flare.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/flare_alpha");
			Rays.Effect = new DefaultEffect.Textured();
			Rays.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/smooth_ray");
		});
		ServiceHelper.AddComponent(TheDarkening = new Darkener(base.Game));
		LevelManager.LevelChanged += Kill;
		WarpSound.Emit().Persistent = true;
	}

	private void Kill()
	{
		if (Phase != Phases.Load)
		{
			ServiceHelper.RemoveComponent(this);
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		Outline.Dispose();
		WireCube.Dispose();
		SolidCube.Dispose();
		Rays.Dispose();
		Flare.Dispose();
		ServiceHelper.RemoveComponent(TheDarkening);
		LevelManager.LevelChanged -= Kill;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading)
		{
			return;
		}
		TimeSpan timeSpan = TimeSpan.Zero;
		if (!GameState.Paused && CameraManager.ActionRunning && CameraManager.Viewpoint.IsOrthographic())
		{
			timeSpan = gameTime.ElapsedGameTime;
		}
		float num = (float)SincePhaseStarted.TotalSeconds;
		float num2 = (float)timeSpan.TotalSeconds;
		Mesh rays = Rays;
		Mesh flare = Flare;
		Mesh solidCube = SolidCube;
		Mesh wireCube = WireCube;
		Vector3 vector = (Outline.Position = Center);
		Vector3 vector3 = (wireCube.Position = vector);
		Vector3 vector5 = (solidCube.Position = vector3);
		Vector3 position = (flare.Position = vector5);
		rays.Position = position;
		Mesh flare2 = Flare;
		Quaternion rotation2 = (Outline.Rotation = CameraManager.Rotation);
		flare2.Rotation = rotation2;
		Mesh solidCube2 = SolidCube;
		rotation2 = (WireCube.Rotation = CameraManager.Rotation * Quaternion.CreateFromAxisAngle(Vector3.Up, Phi));
		solidCube2.Rotation = rotation2;
		SincePhaseStarted += timeSpan;
		if (Phase < Phases.TurnTurnTurn)
		{
			SinceWavesStarted += timeSpan;
			float num3 = (float)(1.0 + Math.Pow(Math.Max(SinceWavesStarted.TotalSeconds - 2.0, 0.0) / 5.0, 4.0));
			if (SinceWavesStarted.TotalSeconds > 2.0 && SinceWavesStarted.TotalSeconds < 12.0)
			{
				OutlineIn -= num2;
				if (OutlineIn <= 0f)
				{
					Outline.Groups[NextOutline].Enabled = true;
					Outline.Groups[NextOutline].Scale = new Vector3(5f);
					if (++NextOutline >= Outline.Groups.Count)
					{
						NextOutline = 1;
					}
					if (num3 > 15f)
					{
						OutlineIn = 0f;
					}
					else
					{
						OutlineIn += 2f / (float)Math.Pow(num3 * 1.25f, 2.0);
					}
				}
			}
			foreach (Group group in Outline.Groups)
			{
				if (group.Enabled && group.Id != 0)
				{
					group.Scale -= new Vector3(num3 * num2);
					group.Material.Opacity = Easing.EaseOut(FezMath.Saturate(1f - (group.Scale.X - 1f) / 4f), EasingType.Sine);
					if (group.Scale.X <= 1f)
					{
						group.Enabled = false;
					}
				}
			}
		}
		if (Phase >= Phases.TurnTurnTurn)
		{
			SinceTurning += timeSpan;
			float num4 = MathHelper.Lerp(0f, 13f, Easing.EaseIn(Easing.EaseOut(SinceTurning.TotalSeconds / 9.75, EasingType.Sine), EasingType.Quintic));
			Dot.RotationSpeed = 0f - num4;
			Phi += num2 * (float)Math.Pow(num4, 1.125);
		}
		switch (Phase)
		{
		case Phases.VectorOutline:
		{
			float num6 = FezMath.Saturate(Easing.Ease(num / 3f, -0.75f, EasingType.Sine));
			Outline.FirstGroup.Material.Opacity = num6;
			Outline.FirstGroup.Scale = new Vector3(4f - num6 * 3f);
			if (num >= 3f)
			{
				Phase = Phases.WireframeWaves;
				PlayerManager.Action = ActionType.LookingUp;
				SincePhaseStarted = TimeSpan.Zero;
				ServiceHelper.AddComponent(new CamShake(ServiceHelper.Game)
				{
					Duration = TimeSpan.FromSeconds(10.0),
					Distance = 0.25f
				});
			}
			break;
		}
		case Phases.WireframeWaves:
		{
			Speech.Hide();
			float num10 = Easing.EaseIn(FezMath.Saturate(num / 5f), EasingType.Quadratic);
			WireCube.Material.Opacity = num10;
			Outline.FirstGroup.Material.Opacity = 1f - num10;
			if (num10 == 1f)
			{
				Phase = Phases.FillCube;
				SincePhaseStarted = TimeSpan.Zero;
			}
			break;
		}
		case Phases.FillCube:
		{
			float num11 = FezMath.Saturate(num / 4f);
			WireCube.Material.Opacity = 1f - num11;
			SolidCube.Material.Opacity = num11;
			(SolidCube.Effect as DefaultEffect).Emissive = num11 / 2f;
			Flare.Material.Opacity = num11 / 2f;
			Flare.Scale = new Vector3(4f * num11);
			if (num11 == 1f)
			{
				Phase = Phases.FillDot;
				SincePhaseStarted = TimeSpan.Zero;
			}
			break;
		}
		case Phases.FillDot:
		{
			float num7 = FezMath.Saturate(num / 2.75f);
			Dot.Hidden = false;
			Dot.ScaleFactor = 50f * Easing.EaseOut(num7, EasingType.Sine);
			Dot.InnerScale = 1f;
			Dot.Opacity = 0.5f + 0.25f * num7;
			Dot.RotationSpeed = 0f;
			GameState.SkyOpacity = 1f - num7;
			if (num7 == 1f)
			{
				Phase = Phases.TurnTurnTurn;
				SincePhaseStarted = TimeSpan.Zero;
			}
			break;
		}
		case Phases.TurnTurnTurn:
			if (FezMath.Saturate(num / 7.5f) == 1f)
			{
				Phase = Phases.Rays;
				SincePhaseStarted = TimeSpan.Zero;
			}
			break;
		case Phases.Rays:
		{
			float num8 = Easing.EaseIn(FezMath.Saturate(num / 1.75f), EasingType.Quadratic);
			(SolidCube.Effect as DefaultEffect).Emissive = 0.5f + num8 / 2f;
			Flare.Material.Opacity = 0.75f + num8 * 0.25f;
			Flare.Scale = new Vector3(4f + 5f * num8);
			num8 = Easing.EaseIn(FezMath.Saturate(num / 1.75f), EasingType.Cubic);
			foreach (Group group2 in Rays.Groups)
			{
				float num9 = (float)group2.Id / (float)Rays.Groups.Count;
				group2.Material.Diffuse = new Vector3(FezMath.Saturate(num8 / num9 * 4f) * 0.25f);
				float val = (float)Math.Pow(num8 / num9 * 4f, 2.0);
				group2.Scale = new Vector3(Math.Min(val, 50f), Math.Min(val, 100f), 1f);
			}
			if (num8 == 1f)
			{
				Phase = Phases.FadeToWhite;
				SincePhaseStarted = TimeSpan.Zero;
			}
			break;
		}
		case Phases.FadeToWhite:
			if ((WhiteFillStep = FezMath.Saturate(num / 1f)) == 1f)
			{
				Phase = Phases.Load;
				SincePhaseStarted = TimeSpan.Zero;
			}
			break;
		case Phases.Load:
		{
			GameState.SkyOpacity = 1f;
			GameState.SkipLoadScreen = true;
			GameState.Loading = true;
			Worker<bool> worker = ThreadPool.Take<bool>(DoLoad);
			worker.Finished += delegate
			{
				ThreadPool.Return(worker);
			};
			worker.Start(context: false);
			break;
		}
		case Phases.FadeOut:
		{
			float num5 = Easing.EaseOut(FezMath.Saturate(num / 0.75f), EasingType.Quintic);
			WhiteFillStep = 1f - num5;
			if (num > 0.75f)
			{
				ServiceHelper.RemoveComponent(this);
			}
			break;
		}
		}
	}

	private void DoLoad(bool dummy)
	{
		HorizontalDirection lookingDirection = PlayerManager.LookingDirection;
		Dot.Reset();
		LevelManager.ChangeLevel(ToLevel);
		PlayerManager.LookingDirection = lookingDirection;
		Phase = Phases.FadeOut;
		PlayerManager.CheckpointGround = null;
		PlayerManager.RespawnAtCheckpoint();
		CameraManager.Center = PlayerManager.Position + Vector3.Up * PlayerManager.Size.Y / 2f + Vector3.UnitY;
		CameraManager.SnapInterpolation();
		LevelMaterializer.CullInstances();
		GameState.ScheduleLoadEnd = true;
		GameState.SkipLoadScreen = false;
		TimeManager.TimeFactor = TimeManager.DefaultTimeFactor;
	}

	public override void Draw(GameTime gameTime)
	{
		if (Phase != Phases.FadeOut)
		{
			Outline.Draw();
			WireCube.Draw();
			Rays.Draw();
			SolidCube.Draw();
			Flare.Draw();
		}
		if (WhiteFillStep > 0f)
		{
			TargetRenderer.DrawFullscreen(new Color(1f, 1f, 1f, WhiteFillStep));
		}
	}
}
