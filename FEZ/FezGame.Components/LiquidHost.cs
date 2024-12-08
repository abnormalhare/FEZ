using System;
using System.Collections.Generic;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class LiquidHost : DrawableGameComponent
{
	private class RayCustomData
	{
		public TimeSpan AccumulatedTime;

		public float RandomSpeed;
	}

	private class WaterTransitionRenderer : DrawableGameComponent
	{
		private readonly LiquidHost Host;

		public bool ViewLocked;

		[ServiceDependency]
		public IEngineStateManager EngineState { get; set; }

		[ServiceDependency]
		public IGameCameraManager CameraManager { get; set; }

		public WaterTransitionRenderer(Game game, LiquidHost host)
			: base(game)
		{
			base.DrawOrder = 1001;
			Host = host;
		}

		public void LockView()
		{
			(Host.LiquidMesh.Effect as DefaultEffect).ForcedViewMatrix = Host.CameraManager.View;
			Host.FoamEffect.ForcedViewMatrix = Host.CameraManager.View;
			(Host.RaysMesh.Effect as DefaultEffect).ForcedViewMatrix = Host.CameraManager.View;
			(Host.CausticsMesh.Effect as CausticsEffect).ForcedViewMatrix = Host.CameraManager.View;
			ViewLocked = true;
		}

		public override void Update(GameTime gameTime)
		{
			if (!EngineState.Loading && !EngineState.Paused)
			{
				float num = (float)base.GraphicsDevice.Viewport.Width / (CameraManager.PixelsPerTrixel * 16f);
				float viewScale = base.GraphicsDevice.GetViewScale();
				if (EngineState.FarawaySettings.OriginFadeOutStep == 1f)
				{
					float num2 = (EngineState.FarawaySettings.TransitionStep - 0.1275f) / 0.8725f;
					num = MathHelper.Lerp(num, EngineState.FarawaySettings.DestinationRadius, Easing.EaseInOut(num2, EasingType.Sine));
				}
				Matrix value = Matrix.CreateOrthographic(num / viewScale, num / CameraManager.AspectRatio / viewScale, CameraManager.NearPlane, CameraManager.FarPlane);
				(Host.LiquidMesh.Effect as DefaultEffect).ForcedProjectionMatrix = value;
				Host.FoamEffect.ForcedProjectionMatrix = value;
				(Host.RaysMesh.Effect as DefaultEffect).ForcedProjectionMatrix = value;
				(Host.CausticsMesh.Effect as CausticsEffect).ForcedProjectionMatrix = value;
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			(Host.LiquidMesh.Effect as DefaultEffect).ForcedViewMatrix = null;
			Host.FoamEffect.ForcedViewMatrix = null;
			(Host.RaysMesh.Effect as DefaultEffect).ForcedViewMatrix = null;
			(Host.CausticsMesh.Effect as CausticsEffect).ForcedViewMatrix = null;
			(Host.LiquidMesh.Effect as DefaultEffect).ForcedProjectionMatrix = null;
			Host.FoamEffect.ForcedProjectionMatrix = null;
			(Host.RaysMesh.Effect as DefaultEffect).ForcedProjectionMatrix = null;
			(Host.CausticsMesh.Effect as CausticsEffect).ForcedProjectionMatrix = null;
			Host.ForcedUpdate = true;
			Host.Update(new GameTime());
			Host.ForcedUpdate = false;
		}

		public override void Draw(GameTime gameTime)
		{
			Host.DoDraw(EngineState.StereoMode);
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		}
	}

	private const int MinShoreSegmentWidth = 10;

	private const int MaxShoreSegmentWidth = 22;

	private const int ShoreThickness = 4;

	private const int ShoreActualTotalWidth = 48;

	public static readonly Dictionary<LiquidType, LiquidColorScheme> ColorSchemes = new Dictionary<LiquidType, LiquidColorScheme>(LiquidTypeComparer.Default)
	{
		{
			LiquidType.Water,
			new LiquidColorScheme
			{
				LiquidBody = new Color(61, 117, 254),
				SolidOverlay = new Color(40, 76, 162),
				SubmergedFoam = new Color(91, 159, 254),
				EmergedFoam = new Color(175, 205, 255)
			}
		},
		{
			LiquidType.Blood,
			new LiquidColorScheme
			{
				LiquidBody = new Color(174, 26, 0),
				SolidOverlay = new Color(84, 0, 21),
				SubmergedFoam = new Color(230, 81, 55),
				EmergedFoam = new Color(255, 255, 255)
			}
		},
		{
			LiquidType.Sewer,
			new LiquidColorScheme
			{
				LiquidBody = new Color(82, 127, 57),
				SolidOverlay = new Color(32, 70, 49),
				SubmergedFoam = new Color(174, 196, 64),
				EmergedFoam = new Color(174, 196, 64)
			}
		},
		{
			LiquidType.Lava,
			new LiquidColorScheme
			{
				LiquidBody = new Color(209, 0, 0),
				SolidOverlay = new Color(150, 0, 0),
				SubmergedFoam = new Color(255, 0, 0),
				EmergedFoam = new Color(255, 0, 0)
			}
		},
		{
			LiquidType.Purple,
			new LiquidColorScheme
			{
				LiquidBody = new Color(194, 1, 171),
				SolidOverlay = new Color(76, 9, 103),
				SubmergedFoam = new Color(247, 52, 223),
				EmergedFoam = new Color(254, 254, 254)
			}
		},
		{
			LiquidType.Green,
			new LiquidColorScheme
			{
				LiquidBody = new Color(47, 255, 139),
				SolidOverlay = new Color(0, 167, 134),
				SubmergedFoam = new Color(0, 218, 175),
				EmergedFoam = new Color(184, 249, 207)
			}
		}
	};

	private Mesh LiquidMesh;

	private Mesh FoamMesh;

	private Mesh RaysMesh;

	private Mesh CausticsMesh;

	private AnimatedTexture CausticsAnimation;

	private AnimationTiming BackgroundCausticsTiming;

	private float CausticsHeight;

	private PlaneParticleSystem BubbleSystem;

	private PlaneParticleSystem EmbersSystem;

	private SoundEmitter eSewageBubbling;

	private SoundEffect sSmallBubble;

	private SoundEffect sMidBubble;

	private SoundEffect sLargeBubble;

	private AnimatedTexture LargeBubbleAnim;

	private AnimatedTexture MediumBubbleAnim;

	private AnimatedTexture SmallBubbleAnim;

	private AnimatedTexture SmokeAnim;

	private TimeSpan TimeUntilBubble;

	private LiquidType? LastWaterType;

	private WaterTransitionRenderer TransitionRenderer;

	private FoamEffect FoamEffect;

	private bool WaterVisible;

	private float WaterLevel;

	private Vector3 RightVector;

	private Quaternion CameraRotation;

	private Vector3 ScreenCenter;

	private float CameraRadius;

	private Vector3 CameraPosition;

	private Vector3 CameraInterpolatedCenter;

	private Vector3 ForwardVector;

	private float OriginalPixPerTrix;

	public static LiquidHost Instance;

	private float lastVariation;

	private TimeSpan accumulator;

	private float lastVisibleWaterHeight = -1f;

	public bool ForcedUpdate;

	private float OriginalDistance;

	public bool InTransition => TransitionRenderer != null;

	public static Func<Vector3, Vector3, Vector3> EmberScaling => delegate
	{
		if (RandomHelper.Probability(0.333))
		{
			return new Vector3(0.125f, 0.0625f, 1f);
		}
		return RandomHelper.Probability(0.5) ? new Vector3(0.0625f, 0.125f, 1f) : new Vector3(0.0625f, 0.0625f, 1f);
	};

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IPlaneParticleSystems PlaneParticleSystems { get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public IDotManager DotManager { get; set; }

	public LiquidHost(Game game)
		: base(game)
	{
		base.DrawOrder = 50;
		Instance = this;
	}

	public override void Initialize()
	{
		base.Initialize();
		LightingPostProcess.DrawOnTopLights += DrawLights;
		LevelManager.LevelChanged += TryInitialize;
		bool visible = (base.Enabled = false);
		base.Visible = visible;
		lastVisibleWaterHeight = -1f;
	}

	private void TryInitialize()
	{
		GameState.WaterBodyColor = ColorSchemes[LiquidType.Water].LiquidBody.ToVector3();
		GameState.WaterFoamColor = ColorSchemes[LiquidType.Water].EmergedFoam.ToVector3();
		if (LevelManager.WaterType == LastWaterType)
		{
			CreateParticleSystems();
			ReestablishLiquidHeight();
			ReloadSounds();
			ForcedUpdate = true;
			Update(new GameTime());
			ForcedUpdate = false;
			return;
		}
		LastWaterType = LevelManager.WaterType;
		bool visible = (base.Enabled = LevelManager.WaterType != LiquidType.None);
		base.Visible = visible;
		lastVisibleWaterHeight = -1f;
		ReestablishLiquidHeight();
		ReloadSounds();
		CreateFoam();
		CreateParticleSystems();
		if (base.Enabled)
		{
			LiquidColorScheme liquidColorScheme = ColorSchemes[LevelManager.WaterType];
			GameState.WaterBodyColor = liquidColorScheme.LiquidBody.ToVector3();
			GameState.WaterFoamColor = liquidColorScheme.EmergedFoam.ToVector3();
			LiquidMesh.Groups[0].Material.Diffuse = liquidColorScheme.LiquidBody.ToVector3();
			LiquidMesh.Groups[1].Material.Diffuse = liquidColorScheme.SolidOverlay.ToVector3();
			FoamMesh.Groups[0].Material.Diffuse = liquidColorScheme.SubmergedFoam.ToVector3();
			if (FoamMesh.Groups.Count > 1)
			{
				FoamMesh.Groups[1].Material.Diffuse = liquidColorScheme.EmergedFoam.ToVector3();
			}
		}
		ForcedUpdate = true;
		Update(new GameTime());
		ForcedUpdate = false;
	}

	private void ReloadSounds()
	{
		if (LevelManager.WaterType == LiquidType.Sewer)
		{
			Vector3 position = CameraManager.InterpolatedCenter * FezMath.XZMask + LevelManager.WaterHeight * Vector3.UnitY;
			eSewageBubbling = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Sewer/SewageBubbling").EmitAt(position, loop: true);
		}
		else if (eSewageBubbling != null && !eSewageBubbling.Dead)
		{
			eSewageBubbling.Cue.Stop();
		}
		if (LevelManager.WaterType == LiquidType.Lava)
		{
			sSmallBubble = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Lava/SmallBubble");
			sMidBubble = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Lava/MediumBubble");
			sLargeBubble = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Lava/LargeBubble");
		}
		else
		{
			sSmallBubble = (sMidBubble = (sLargeBubble = null));
		}
	}

	private void ReestablishLiquidHeight()
	{
		if (LevelManager.Name != null)
		{
			if (LevelManager.WaterType == LiquidType.Water)
			{
				LevelManager.OriginalWaterHeight = LevelManager.WaterHeight;
				if (GameState.SaveData.GlobalWaterLevelModifier.HasValue)
				{
					LevelManager.WaterHeight += GameState.SaveData.GlobalWaterLevelModifier.Value;
				}
			}
			else if (!GameState.SaveData.ThisLevel.LastStableLiquidHeight.HasValue)
			{
				GameState.SaveData.ThisLevel.LastStableLiquidHeight = LevelManager.WaterHeight;
			}
			else
			{
				LevelManager.WaterHeight = GameState.SaveData.ThisLevel.LastStableLiquidHeight.Value;
			}
		}
		if (PlayerManager.Position.Y <= LevelManager.WaterHeight)
		{
			float num = PlayerManager.Position.Y - 1f - LevelManager.WaterHeight;
			LevelManager.WaterHeight += num;
			if (LevelManager.WaterType == LiquidType.Water && GameState.SaveData.GlobalWaterLevelModifier.HasValue)
			{
				GameState.SaveData.GlobalWaterLevelModifier += num;
			}
		}
		if (LevelManager.Name == "SEWER_START")
		{
			if (LevelManager.WaterHeight > 21.75f)
			{
				LevelManager.WaterHeight = 24.5f;
			}
			else
			{
				LevelManager.WaterHeight = 19f;
			}
		}
	}

	public void StartTransition()
	{
		ServiceHelper.AddComponent(TransitionRenderer = new WaterTransitionRenderer(base.Game, this));
		WaterLevel = LevelManager.WaterHeight;
		RightVector = CameraManager.Viewpoint.RightVector();
	}

	public void LockView()
	{
		TransitionRenderer.LockView();
	}

	public void EndTransition()
	{
		if (TransitionRenderer != null)
		{
			ServiceHelper.RemoveComponent(TransitionRenderer);
			TransitionRenderer = null;
			ForcedUpdate = true;
			Update(new GameTime());
			ForcedUpdate = false;
		}
	}

	private void CreateFoam()
	{
		if (FoamMesh == null)
		{
			FoamMesh = new Mesh
			{
				AlwaysOnTop = true,
				DepthWrites = false,
				Blending = BlendingMode.Alphablending,
				Culling = CullMode.CullClockwiseFace
			};
			DrawActionScheduler.Schedule(delegate
			{
				FoamMesh.Effect = (FoamEffect = new FoamEffect());
			});
		}
		FoamMesh.ClearGroups();
		RaysMesh.ClearGroups();
		FoamMesh.Rotation = Quaternion.Identity;
		FoamMesh.Position = Vector3.Zero;
		if (LevelManager.WaterType == LiquidType.None)
		{
			return;
		}
		switch (LevelManager.WaterType)
		{
		case LiquidType.Lava:
		case LiquidType.Sewer:
		{
			if (FoamEffect == null)
			{
				DrawActionScheduler.Schedule(delegate
				{
					FoamEffect.IsWobbling = false;
				});
			}
			else
			{
				FoamEffect.IsWobbling = false;
			}
			Group group = FoamMesh.AddFace(new Vector3(1f, 0.125f, 1f), Vector3.Zero, FaceOrientation.Back, Color.White, centeredOnOrigin: false);
			group.Position = new Vector3(0.5f, -0.125f, 0f);
			group.BakeTransform<VertexPositionNormalColor>();
			group.Scale = new Vector3(100f, 0.5f, 1f);
			group.Position = new Vector3(-100f, -1f, 0f);
			group.Material = new Material();
			group.CustomData = true;
			break;
		}
		case LiquidType.Water:
		case LiquidType.Blood:
		case LiquidType.Purple:
		case LiquidType.Green:
		{
			if (FoamEffect == null)
			{
				DrawActionScheduler.Schedule(delegate
				{
					FoamEffect.IsWobbling = true;
				});
			}
			else
			{
				FoamEffect.IsWobbling = true;
			}
			float num = (float)RandomHelper.Random.Next(10, 22) / 16f;
			for (float num2 = -24f; num2 < 24f; num2 += num)
			{
				Group group = FoamMesh.AddFace(new Vector3(1f, 0.125f, 1f), Vector3.Zero, FaceOrientation.Back, Color.White, centeredOnOrigin: true);
				group.Position = new Vector3(0.5f, -0.0625f, 0f);
				group.BakeTransform<VertexPositionNormalColor>();
				IndexedUserPrimitives<VertexPositionNormalColor> indexedUserPrimitives = group.Geometry as IndexedUserPrimitives<VertexPositionNormalColor>;
				for (int i = 0; i < indexedUserPrimitives.Vertices.Length; i++)
				{
					indexedUserPrimitives.Vertices[i].Normal = new Vector3(num2, 0f, 0f);
				}
				group.Scale = new Vector3(num, 1f, 1f);
			}
			Group group2 = FoamMesh.CollapseToBuffer<VertexPositionNormalColor>();
			group2.Material = new Material();
			group2.Position = new Vector3(0f, -1f, 0f);
			group2.CustomData = true;
			for (float num2 = -24f; num2 < 24f; num2 += num)
			{
				Group group = FoamMesh.AddFace(new Vector3(1f, 0.125f, 1f), Vector3.Zero, FaceOrientation.Back, Color.White, centeredOnOrigin: true);
				group.Position = new Vector3(0.5f, 0.0625f, 0f);
				group.BakeTransform<VertexPositionNormalColor>();
				IndexedUserPrimitives<VertexPositionNormalColor> indexedUserPrimitives2 = group.Geometry as IndexedUserPrimitives<VertexPositionNormalColor>;
				for (int j = 0; j < indexedUserPrimitives2.Vertices.Length; j++)
				{
					indexedUserPrimitives2.Vertices[j].Normal = new Vector3(num2, 0f, 0f);
				}
				group.Scale = new Vector3(num, 1f, 1f);
			}
			Group group3 = FoamMesh.CollapseToBuffer<VertexPositionNormalColor>(1, FoamMesh.Groups.Count - 1);
			group3.Material = new Material();
			group3.Position = new Vector3(0f, -1f, 0f);
			group3.CustomData = false;
			break;
		}
		}
	}

	private void CreateParticleSystems()
	{
		if (LevelManager.WaterType == LiquidType.None)
		{
			return;
		}
		LiquidColorScheme liquidColorScheme = ColorSchemes[LevelManager.WaterType];
		LiquidType waterType = LevelManager.WaterType;
		if (waterType == LiquidType.Lava || waterType == LiquidType.Sewer)
		{
			Color color = new Color(liquidColorScheme.SubmergedFoam.ToVector3() * 0.5f);
			PlaneParticleSystemSettings settings = new PlaneParticleSystemSettings
			{
				Velocity = new Vector3(0f, 0.15f, 0f),
				Gravity = new Vector3(0f, 0f, 0f),
				SpawningSpeed = 50f,
				ParticleLifetime = 2.2f,
				SpawnBatchSize = 1,
				SizeBirth = new VaryingVector3
				{
					Base = new Vector3(0.0625f),
					Variation = new Vector3(0.0625f),
					Function = VaryingVector3.Uniform
				},
				SizeDeath = new VaryingVector3
				{
					Base = new Vector3(0.125f),
					Variation = new Vector3(0.125f),
					Function = VaryingVector3.Uniform
				},
				FadeInDuration = 0.1f,
				FadeOutDuration = 0.1f,
				ColorLife = new VaryingColor
				{
					Base = color,
					Variation = color,
					Function = VaryingColor.Uniform
				},
				Texture = CMProvider.Global.Load<Texture2D>("Background Planes/white_square"),
				BlendingMode = BlendingMode.Alphablending,
				Billboarding = true
			};
			IPlaneParticleSystems planeParticleSystems = PlaneParticleSystems;
			PlaneParticleSystem obj = new PlaneParticleSystem(base.Game, 100, settings)
			{
				DrawOrder = base.DrawOrder + 1
			};
			PlaneParticleSystem system = obj;
			BubbleSystem = obj;
			planeParticleSystems.Add(system);
			if (LevelManager.WaterType != LiquidType.Sewer)
			{
				settings = new PlaneParticleSystemSettings
				{
					Velocity = new VaryingVector3
					{
						Variation = new Vector3(1f)
					},
					Gravity = new Vector3(0f, 0.01f, 0f),
					SpawningSpeed = 40f,
					ParticleLifetime = 2f,
					SpawnBatchSize = 1,
					RandomizeSpawnTime = true,
					SizeBirth = new VaryingVector3
					{
						Function = EmberScaling
					},
					FadeInDuration = 0.15f,
					FadeOutDuration = 0.4f,
					ColorBirth = new Color(255, 255, 255, 0),
					ColorLife = 
					{
						Base = new Color(255, 16, 16),
						Variation = new Color(0, 32, 32),
						Function = VaryingColor.Uniform
					},
					ColorDeath = new Color(0, 0, 0, 32),
					Texture = CMProvider.Global.Load<Texture2D>("Background Planes/white_square"),
					BlendingMode = BlendingMode.Alphablending,
					Billboarding = true
				};
				IPlaneParticleSystems planeParticleSystems2 = PlaneParticleSystems;
				PlaneParticleSystem obj2 = new PlaneParticleSystem(base.Game, 50, settings)
				{
					DrawOrder = base.DrawOrder + 1
				};
				system = obj2;
				EmbersSystem = obj2;
				planeParticleSystems2.Add(system);
			}
		}
	}

	protected override void LoadContent()
	{
		LiquidMesh = new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false,
			Blending = BlendingMode.Alphablending,
			Culling = CullMode.None
		};
		Group g = LiquidMesh.AddColoredBox(Vector3.One, Vector3.Zero, Color.White, centeredOnOrigin: true);
		g.Position = new Vector3(0f, -0.5f, 0f);
		g.BakeTransform<FezVertexPositionColor>();
		g.Position = new Vector3(0f, -1f, 0f);
		g.Scale = new Vector3(150f);
		g.Material = new Material();
		g = LiquidMesh.AddColoredBox(Vector3.One, Vector3.Zero, Color.White, centeredOnOrigin: true);
		g.Position = new Vector3(0f, -0.5f, 0f);
		g.BakeTransform<FezVertexPositionColor>();
		g.Position = new Vector3(0f, -1f, 0f);
		g.Scale = new Vector3(150f);
		g.Material = new Material();
		RaysMesh = new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false,
			Blending = BlendingMode.Additive,
			Culling = CullMode.CullClockwiseFace
		};
		CausticsAnimation = CMProvider.Global.Load<AnimatedTexture>("Other Textures/FINAL_caustics");
		CausticsAnimation.Timing.Loop = true;
		BackgroundCausticsTiming = CausticsAnimation.Timing.Clone();
		BackgroundCausticsTiming.RandomizeStep();
		CausticsMesh = new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false,
			SamplerState = SamplerState.PointWrap
		};
		g = CausticsMesh.AddTexturedCylinder(Vector3.One, Vector3.Zero, 3, 4, centeredOnOrigin: false, capped: false);
		g.Material = new Material();
		SmallBubbleAnim = CMProvider.Global.Load<AnimatedTexture>("Background Planes/lava/lava_a");
		MediumBubbleAnim = CMProvider.Global.Load<AnimatedTexture>("Background Planes/lava/lava_b");
		LargeBubbleAnim = CMProvider.Global.Load<AnimatedTexture>("Background Planes/lava/lava_c");
		SmokeAnim = CMProvider.Global.Load<AnimatedTexture>("Background Planes/lava/lava_smoke");
		DrawActionScheduler.Schedule(delegate
		{
			LiquidMesh.Effect = new DefaultEffect.VertexColored();
			RaysMesh.Effect = new DefaultEffect.VertexColored();
			CausticsMesh.Effect = new CausticsEffect();
			g.Texture = CausticsAnimation.Texture;
		});
	}

	public override void Update(GameTime gameTime)
	{
		if ((GameState.Loading && !ForcedUpdate && TransitionRenderer == null) || (GameState.TimePaused && !ForcedUpdate))
		{
			return;
		}
		if (TransitionRenderer == null)
		{
			WaterLevel = LevelManager.WaterHeight;
			RightVector = CameraManager.InverseView.Right;
			CameraRotation = CameraManager.Rotation;
			ScreenCenter = CameraManager.Center;
			CameraRadius = CameraManager.Radius;
			CameraPosition = CameraManager.Position;
			CameraInterpolatedCenter = CameraManager.InterpolatedCenter;
			ForwardVector = CameraManager.InverseView.Forward;
			OriginalPixPerTrix = CameraManager.PixelsPerTrixel;
		}
		if (GameState.FarawaySettings.InTransition && GameState.FarawaySettings.OriginFadeOutStep == 1f && TransitionRenderer != null && !TransitionRenderer.ViewLocked)
		{
			LockView();
			CameraRadius = CameraManager.Radius;
			OriginalDistance = WaterLevel - CameraManager.InverseView.Translation.Y - 0.625f;
		}
		float num = WaterLevel;
		if (TransitionRenderer != null && GameState.FarawaySettings.OriginFadeOutStep == 1f)
		{
			float num2 = MathHelper.Lerp(CameraRadius, GameState.FarawaySettings.DestinationRadius / 4f, GameState.FarawaySettings.TransitionStep);
			num = WaterLevel - OriginalDistance + OriginalDistance * (CameraRadius / num2);
		}
		FoamMesh.Rotation = CameraRotation;
		if (LevelManager.WaterType == LiquidType.Lava || LevelManager.WaterType == LiquidType.Sewer)
		{
			BubbleSystem.Enabled = CameraManager.Viewpoint != Viewpoint.Perspective;
		}
		if (LevelManager.WaterType == LiquidType.Lava)
		{
			EmbersSystem.Enabled = CameraManager.Viewpoint != Viewpoint.Perspective;
		}
		foreach (Group group3 in RaysMesh.Groups)
		{
			group3.Rotation = CameraRotation;
		}
		float num3 = CameraPosition.Y - CameraRadius / 2f / CameraManager.AspectRatio;
		if (WaterVisible || lastVisibleWaterHeight < num)
		{
			lastVisibleWaterHeight = num;
		}
		if (LevelManager.WaterType == LiquidType.Sewer || LevelManager.WaterType == LiquidType.Lava)
		{
			BubbleSystem.Visible = num > num3;
			BubbleSystem.Enabled &= num > num3;
		}
		if (LevelManager.WaterType == LiquidType.Lava)
		{
			EmbersSystem.Settings.SpawnVolume = new BoundingBox
			{
				Min = ScreenCenter - new Vector3(CameraRadius / 2f),
				Max = ScreenCenter + new Vector3(CameraRadius / 2f / CameraManager.AspectRatio)
			};
		}
		if (LevelManager.WaterType == LiquidType.Sewer)
		{
			eSewageBubbling.Position = CameraInterpolatedCenter * FezMath.XZMask + num * Vector3.UnitY;
		}
		WaterVisible = lastVisibleWaterHeight > num3 || TransitionRenderer != null;
		if (!WaterVisible && !ForcedUpdate)
		{
			if ((LevelManager.WaterType == LiquidType.Lava || LevelManager.WaterType == LiquidType.Sewer) && lastVisibleWaterHeight != num)
			{
				BubbleSystem.Clear();
			}
			if (LevelManager.WaterType == LiquidType.Lava && !GameState.Loading)
			{
				SpawnBubbles(gameTime.ElapsedGameTime, num, invisible: true);
			}
			return;
		}
		accumulator += gameTime.ElapsedGameTime;
		if (accumulator.TotalSeconds > 6.2831854820251465)
		{
			accumulator -= TimeSpan.FromSeconds(6.2831854820251465);
		}
		float num4 = (float)Math.Sin(accumulator.TotalSeconds / 2.0) * 2f / 16f;
		num -= lastVariation;
		num += num4;
		lastVariation = num4;
		RaysMesh.Position = (num - 0.5f) * Vector3.UnitY;
		LiquidMesh.Position = ScreenCenter * FezMath.XZMask + (num + 0.5f) * Vector3.UnitY;
		if (LevelManager.Sky != null && LevelManager.Sky.Name != "Cave")
		{
			CausticsMesh.Position = FezMath.XZMask * CausticsMesh.Position + (num - 0.5f) * Vector3.UnitY;
		}
		if (LevelManager.WaterType.IsWater())
		{
			if (LevelManager.Sky != null && LevelManager.Sky.Name == "Cave")
			{
				BackgroundCausticsTiming.Update(gameTime.ElapsedGameTime, 0.375f);
			}
			CausticsAnimation.Timing.Update(gameTime.ElapsedGameTime, 0.875f);
		}
		if (LevelManager.WaterType == LiquidType.Lava || LevelManager.WaterType == LiquidType.Sewer)
		{
			BubbleSystem.Settings.Velocity = new Vector3(0f, 0.15f + LevelManager.WaterSpeed * 0.75f, 0f);
			BubbleSystem.Settings.SpawnVolume = new BoundingBox
			{
				Min = (ScreenCenter - new Vector3(CameraRadius / 1.5f)) * FezMath.XZMask + (num - 1.8f) * Vector3.UnitY,
				Max = (ScreenCenter + new Vector3(CameraRadius / 1.5f)) * FezMath.XZMask + (num - 0.8f) * Vector3.UnitY
			};
		}
		switch (LevelManager.WaterType)
		{
		case LiquidType.Lava:
			FoamMesh.Position = LiquidMesh.Position;
			if (!ForcedUpdate)
			{
				SpawnBubbles(gameTime.ElapsedGameTime, num, invisible: false);
			}
			if (FoamEffect != null)
			{
				FoamEffect.ScreenCenterSide = CameraInterpolatedCenter.Dot(RightVector);
				FoamEffect.ShoreTotalWidth = 48f;
			}
			break;
		case LiquidType.Sewer:
			FoamMesh.Position = LiquidMesh.Position;
			if (FoamEffect != null)
			{
				FoamEffect.ScreenCenterSide = CameraInterpolatedCenter.Dot(RightVector);
				FoamEffect.ShoreTotalWidth = 48f;
			}
			break;
		case LiquidType.Water:
		case LiquidType.Blood:
		case LiquidType.Purple:
		case LiquidType.Green:
		{
			FoamMesh.Position = (num + 0.5f) * Vector3.UnitY + CameraInterpolatedCenter * ForwardVector;
			if (FoamEffect != null)
			{
				FoamEffect.TimeAccumulator = (float)accumulator.TotalSeconds;
				FoamEffect.ScreenCenterSide = CameraInterpolatedCenter.Dot(RightVector);
				FoamEffect.ShoreTotalWidth = 48f;
			}
			if (TransitionRenderer == null && RandomHelper.Probability(0.03))
			{
				Vector3 vector = ScreenCenter - CameraRadius / 2f * FezMath.XZMask;
				Vector3 vector2 = ScreenCenter + CameraRadius / 2f * FezMath.XZMask;
				Vector3 position = new Vector3(RandomHelper.Between(vector.X, vector2.X), 0f, RandomHelper.Between(vector.Z, vector2.Z));
				float num5 = RandomHelper.Between(0.1, 1.25);
				float num6 = 3f + RandomHelper.Centered(1.0);
				Group group = RaysMesh.AddColoredQuad(new Vector3(0f, 0f, 0f), new Vector3(0f - num6 - num5, 0f - num6, 0f), new Vector3(0f - num6, 0f - num6, 0f), new Vector3(0f - num5, 0f, 0f), Color.White, Color.Black, Color.Black, Color.White);
				group.CustomData = new RayCustomData
				{
					RandomSpeed = RandomHelper.Between(0.5, 1.5)
				};
				group.Material = new Material();
				group.Position = position;
			}
			for (int i = 0; i < RaysMesh.Groups.Count; i++)
			{
				Group group2 = RaysMesh.Groups[i];
				RayCustomData rayCustomData = (RayCustomData)group2.CustomData;
				if (rayCustomData != null)
				{
					rayCustomData.AccumulatedTime += gameTime.ElapsedGameTime;
					group2.Material.Diffuse = new Vector3(Easing.EaseOut(Math.Sin(rayCustomData.AccumulatedTime.TotalSeconds / 5.0 * 3.1415927410125732), EasingType.Quadratic) * 0.2f);
					group2.Position += (float)gameTime.ElapsedGameTime.TotalSeconds * RightVector * 0.4f * rayCustomData.RandomSpeed;
					if (rayCustomData.AccumulatedTime.TotalSeconds > 5.0)
					{
						RaysMesh.RemoveGroupAt(i);
						i--;
					}
				}
			}
			break;
		}
		}
		if (LevelManager.WaterType != LiquidType.Lava || !(LevelManager.WaterHeight >= 135.5f))
		{
			return;
		}
		foreach (TrileInstance key in LevelManager.PickupGroups.Keys)
		{
			key.PhysicsState.IgnoreCollision = true;
		}
	}

	private void SpawnBubbles(TimeSpan elapsed, float waterLevel, bool invisible)
	{
		TimeUntilBubble -= elapsed;
		if (!(TimeUntilBubble.TotalSeconds <= 0.0))
		{
			return;
		}
		AnimatedTexture animatedTexture = (RandomHelper.Probability(0.7) ? SmallBubbleAnim : (RandomHelper.Probability(0.7) ? MediumBubbleAnim : LargeBubbleAnim));
		Vector3 position = new Vector3(RandomHelper.Between(ScreenCenter.X - CameraRadius / 2f, ScreenCenter.X + CameraRadius / 2f), waterLevel + (float)animatedTexture.FrameHeight / 32f - 0.5f - 0.0625f, RandomHelper.Between(ScreenCenter.Z - CameraRadius / 2f, ScreenCenter.Z + CameraRadius / 2f));
		if (!invisible)
		{
			LevelManager.AddPlane(new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, animatedTexture)
			{
				Position = position,
				Rotation = CameraRotation * (RandomHelper.Probability(0.5) ? Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI) : Quaternion.Identity),
				Doublesided = true,
				Loop = false,
				Timing = 
				{
					Step = 0f
				}
			});
		}
		if (animatedTexture == SmallBubbleAnim)
		{
			sSmallBubble.EmitAt(position).FadeDistance = 20f;
		}
		else if (animatedTexture == MediumBubbleAnim)
		{
			sMidBubble.EmitAt(position).FadeDistance = 20f;
		}
		else
		{
			sLargeBubble.EmitAt(position).FadeDistance = 20f;
		}
		if (!invisible && animatedTexture != SmallBubbleAnim)
		{
			Waiters.Wait(0.1, delegate
			{
				LevelManager.AddPlane(new BackgroundPlane(LevelMaterializer.AnimatedPlanesMesh, SmokeAnim)
				{
					Position = new Vector3(position.X, waterLevel + (float)SmokeAnim.FrameHeight / 32f - 0.5f + 0.125f, position.Z) + ForwardVector,
					Rotation = CameraRotation * (RandomHelper.Probability(0.5) ? Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI) : Quaternion.Identity),
					Doublesided = true,
					Opacity = 0.4f,
					Loop = false,
					Timing = 
					{
						Step = 0f
					}
				});
			});
		}
		TimeUntilBubble = TimeSpan.FromSeconds(RandomHelper.Between(0.1, 0.4));
	}

	public void DrawLights()
	{
		if (!base.Visible || LevelManager.WaterType == LiquidType.None || GameState.Loading)
		{
			return;
		}
		LiquidColorScheme liquidColorScheme = ColorSchemes[LevelManager.WaterType];
		Vector3 diffuse = new Vector3(0.5f);
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		bool flag = CameraManager.Viewpoint == Viewpoint.Perspective || CameraManager.ProjectionTransition;
		LiquidMesh.AlwaysOnTop = !flag;
		LiquidMesh.Groups[0].Material.Diffuse = diffuse;
		LiquidMesh.Groups[0].Enabled = true;
		LiquidMesh.Groups[1].Enabled = false;
		LiquidMesh.Draw();
		LiquidMesh.Groups[0].Material.Diffuse = liquidColorScheme.LiquidBody.ToVector3();
		if (LevelManager.WaterType.IsWater() && !GameState.InFpsMode)
		{
			if (LevelManager.WaterType != LiquidType.Water)
			{
				Vector3 vector = ColorSchemes[LevelManager.WaterType].LiquidBody.ToVector3();
				CausticsMesh.FirstGroup.Material.Diffuse = vector / Math.Max(Math.Max(vector.X, vector.Y), vector.Z);
			}
			else
			{
				CausticsMesh.FirstGroup.Material.Diffuse = Vector3.One;
			}
			if (LevelManager.Sky != null && LevelManager.Sky.Name == "Cave")
			{
				float num = CameraRadius * 3f;
				CausticsMesh.Position = new Vector3(LevelManager.Size.X / 2f - num / 2f, WaterLevel - 0.5f, LevelManager.Size.Z / 2f - num / 2f);
				CausticsHeight = 12f;
				CausticsMesh.Scale = new Vector3(num, CausticsHeight, num);
				CausticsMesh.SamplerState = SamplerState.LinearWrap;
				CausticsMesh.Culling = CullMode.CullClockwiseFace;
				float num2 = num / (CausticsHeight / 2f);
				int width = CausticsAnimation.Texture.Width;
				int height = CausticsAnimation.Texture.Height;
				int frame = BackgroundCausticsTiming.Frame;
				Rectangle rectangle = CausticsAnimation.Offsets[frame];
				CausticsMesh.TextureMatrix = new Matrix(num2 * (float)rectangle.Width / (float)width, 0f, 0f, 0f, 0f, (float)rectangle.Height / (float)height, 0f, 0f, (float)(-rectangle.Width) / (float)width / 4f * num2, (float)rectangle.Y / (float)height, 1f, 0f, 0f, 0f, 0f, 1f);
				frame = (frame + 1) % BackgroundCausticsTiming.FrameTimings.Length;
				rectangle = CausticsAnimation.Offsets[frame];
				CausticsMesh.CustomData = new Matrix(num2 * (float)rectangle.Width / (float)width, 0f, 0f, 0f, 0f, (float)rectangle.Height / (float)height, 0f, 0f, (float)(-rectangle.Width) / (float)width / 4f * num2, (float)rectangle.Y / (float)height, BackgroundCausticsTiming.NextFrameContribution, 0f, 0f, 0f, 0f, 1f);
				CausticsMesh.Blending = BlendingMode.Maximum;
				base.GraphicsDevice.PrepareStencilRead(CompareFunction.Greater, StencilMask.Level);
				CausticsMesh.Draw();
				Vector3 vector2 = LevelManager.Size * 1.5f;
				float num3 = Math.Max(vector2.X, vector2.Z);
				CausticsHeight = 3f;
				CausticsMesh.Position = new Vector3(vector2.X / 1.5f / 2f + vector2.X * -0.5f, WaterLevel - 0.5f, vector2.Z / 1.5f / 2f + vector2.Z * -0.5f);
				CausticsMesh.Culling = CullMode.CullCounterClockwiseFace;
				CausticsMesh.Scale = new Vector3(num3, CausticsHeight, num3);
				CausticsMesh.Blending = null;
				CausticsMesh.SamplerState = SamplerState.LinearWrap;
				num2 = num3 / (CausticsHeight / 2f);
				frame = CausticsAnimation.Timing.Frame;
				rectangle = CausticsAnimation.Offsets[frame];
				CausticsMesh.TextureMatrix = new Matrix(num2 * (float)rectangle.Width / (float)width, 0f, 0f, 0f, 0f, (float)rectangle.Height / (float)height, 0f, 0f, (0f - num2) / 2f, (float)rectangle.Y / (float)height, 1f, 0f, 0f, 0f, 0f, 1f);
				frame = (frame + 1) % CausticsAnimation.Timing.FrameTimings.Length;
				rectangle = CausticsAnimation.Offsets[frame];
				CausticsMesh.CustomData = new Matrix(num2 * (float)rectangle.Width / (float)width, 0f, 0f, 0f, 0f, (float)rectangle.Height / (float)height, 0f, 0f, (0f - num2) / 2f, (float)rectangle.Y / (float)height, CausticsAnimation.Timing.NextFrameContribution, 0f, 0f, 0f, 0f, 1f);
				base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			}
			else
			{
				base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
				Vector3 vector3 = LevelManager.Size * 1.5f;
				float num4 = Math.Max(vector3.X, vector3.Z);
				CausticsMesh.SamplerState = SamplerState.LinearWrap;
				CausticsHeight = 3f;
				CausticsMesh.Scale = new Vector3(num4, CausticsHeight, num4);
				float num5 = num4 / (CausticsHeight / 2f);
				int width2 = CausticsAnimation.Texture.Width;
				int height2 = CausticsAnimation.Texture.Height;
				int frame2 = CausticsAnimation.Timing.Frame;
				Rectangle rectangle2 = CausticsAnimation.Offsets[frame2];
				CausticsMesh.TextureMatrix = new Matrix(num5 * (float)rectangle2.Width / (float)width2, 0f, 0f, 0f, 0f, (float)rectangle2.Height / (float)height2, 0f, 0f, (0f - num5) / 2f, (float)rectangle2.Y / (float)height2, 1f, 0f, 0f, 0f, 0f, 1f);
				frame2 = (frame2 + 1) % CausticsAnimation.Timing.FrameTimings.Length;
				rectangle2 = CausticsAnimation.Offsets[frame2];
				CausticsMesh.CustomData = new Matrix(num5 * (float)rectangle2.Width / (float)width2, 0f, 0f, 0f, 0f, (float)rectangle2.Height / (float)height2, 0f, 0f, (0f - num5) / 2f, (float)rectangle2.Y / (float)height2, CausticsAnimation.Timing.NextFrameContribution, 0f, 0f, 0f, 0f, 1f);
			}
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.LessEqual, StencilMask.Level);
			CausticsMesh.Draw();
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		}
		if (FoamMesh.Groups.Count > 1)
		{
			FoamMesh.Groups[0].Enabled = false;
			FoamMesh.Groups[1].Material.Diffuse = diffuse;
			FoamMesh.Draw();
			FoamMesh.Groups[1].Material.Diffuse = liquidColorScheme.EmergedFoam.ToVector3();
			FoamMesh.Groups[0].Enabled = true;
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (!GameState.Loading && TransitionRenderer == null && !GameState.StereoMode)
		{
			DoDraw();
		}
	}

	public void DoDraw(bool skipUnderwater = false)
	{
		bool flag = CameraManager.Viewpoint == Viewpoint.Perspective || CameraManager.ProjectionTransition;
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		LiquidColorScheme liquidColorScheme = ColorSchemes[LevelManager.WaterType];
		Vector3 vector = ((LevelManager.WaterType == LiquidType.Sewer || GameState.StereoMode) ? Vector3.One : LevelManager.ActualDiffuse.ToVector3());
		graphicsDevice.GetDssCombiner().StencilEnable = true;
		if (!CameraManager.ViewTransitionReached && GameState.InFpsMode)
		{
			flag = ((!CameraManager.Viewpoint.IsOrthographic()) ? (flag & (CameraManager.ViewTransitionStep > 0.1f)) : (flag & (CameraManager.ViewTransitionStep < 0.87f)));
		}
		LiquidMesh.AlwaysOnTop = !flag;
		graphicsDevice.GetDssCombiner().StencilPass = StencilOperation.Keep;
		LiquidMesh.Groups[0].Enabled = true;
		LiquidMesh.Groups[1].Enabled = false;
		LiquidMesh.Groups[0].Material.Diffuse *= vector;
		LiquidMesh.Draw();
		LiquidMesh.Groups[0].Material.Diffuse = liquidColorScheme.LiquidBody.ToVector3();
		if (!skipUnderwater)
		{
			graphicsDevice.PrepareStencilRead(CompareFunction.LessEqual, StencilMask.Level);
			LiquidMesh.Groups[0].Enabled = false;
			LiquidMesh.Groups[1].Enabled = true;
			if (GameState.FarawaySettings.InTransition)
			{
				if ((double)GameState.FarawaySettings.TransitionStep < 0.5)
				{
					LiquidMesh.Groups[1].Material.Opacity = 1f - GameState.FarawaySettings.OriginFadeOutStep;
				}
				else if (GameState.FarawaySettings.DestinationCrossfadeStep > 0f)
				{
					LiquidMesh.Groups[1].Material.Opacity = GameState.FarawaySettings.DestinationCrossfadeStep;
				}
			}
			else
			{
				LiquidMesh.Groups[1].Material.Opacity = 1f;
			}
			LiquidMesh.Groups[1].Material.Diffuse *= vector;
			LiquidMesh.Draw();
			LiquidMesh.Groups[1].Material.Diffuse = liquidColorScheme.SolidOverlay.ToVector3();
		}
		graphicsDevice.PrepareStencilWrite(StencilMask.Water);
		graphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
		LiquidMesh.Groups[0].Enabled = true;
		LiquidMesh.Draw();
		graphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
		float opacity = 1f;
		if (!GameState.FarawaySettings.InTransition)
		{
			if (CameraManager.Viewpoint == Viewpoint.Perspective)
			{
				opacity = 0f;
			}
			else if (CameraManager.ProjectionTransition && CameraManager.Viewpoint == Viewpoint.Perspective)
			{
				opacity = 1f - CameraManager.ViewTransitionStep;
			}
			else if (CameraManager.ProjectionTransition && CameraManager.Viewpoint.IsOrthographic())
			{
				opacity = CameraManager.ViewTransitionStep;
			}
		}
		FoamMesh.Groups[0].Material.Opacity = opacity;
		if (FoamMesh.Groups.Count > 1)
		{
			FoamMesh.Groups[1].Material.Opacity = opacity;
		}
		FoamMesh.Groups[0].Material.Diffuse *= vector;
		if (FoamMesh.Groups.Count > 1)
		{
			FoamMesh.Groups[1].Material.Diffuse *= vector;
		}
		float radius = CameraManager.Radius;
		if (48f < radius)
		{
			FoamMesh.Position -= new Vector3(48f, 0f, 48f);
			FoamMesh.Draw();
			FoamMesh.Position += new Vector3(96f, 0f, 96f);
			FoamMesh.Draw();
			FoamMesh.Position -= new Vector3(48f, 0f, 48f);
		}
		FoamMesh.Draw();
		FoamMesh.Groups[0].Material.Diffuse = liquidColorScheme.SubmergedFoam.ToVector3();
		if (FoamMesh.Groups.Count > 1)
		{
			FoamMesh.Groups[1].Material.Diffuse = liquidColorScheme.EmergedFoam.ToVector3();
		}
		RaysMesh.AlwaysOnTop = !flag;
		RaysMesh.Draw();
		graphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
	}
}
