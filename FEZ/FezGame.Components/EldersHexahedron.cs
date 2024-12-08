using System;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Components.Scripting;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Components.Actions;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class EldersHexahedron : DrawableGameComponent
{
	private enum Phase
	{
		ZoomOut,
		Talk1,
		Beam,
		MatrixSpin,
		Talk2,
		Disappear,
		FezBeamGrow,
		FezComeDown,
		Beamsplode,
		Yay,
		WaitSpin,
		HexaExplode,
		ThatsIt
	}

	private struct RayData
	{
		public int Sign;

		public float Speed;

		public float SinceAlive;
	}

	private struct ShardProjectionData
	{
		public Vector3 Direction;

		public Quaternion Spin;
	}

	private class StarfieldRenderer : DrawableGameComponent
	{
		private readonly EldersHexahedron Host;

		public StarfieldRenderer(Game game, EldersHexahedron host)
			: base(game)
		{
			Host = host;
			base.DrawOrder = 0;
			base.Visible = false;
		}

		public override void Draw(GameTime gameTime)
		{
			Host.TargetRenderer.DrawFullscreen(new Color(12f / 85f, 3f / 34f, 0.20588237f, Host.Starfield.Opacity));
			Host.Starfield.Draw();
		}
	}

	private static readonly string[] HexStrings = new string[8] { "HEX_A", "HEX_B", "HEX_C", "HEX_D", "HEX_E", "HEX_F", "HEX_G", "HEX_H" };

	private readonly ArtObjectInstance AoInstance;

	private Vector3 Origin;

	private Vector3 CameraOrigin;

	private Vector3 GomezOrigin;

	private Quaternion AoRotationOrigin;

	private StarfieldRenderer SfRenderer;

	private StarField Starfield;

	private PlaneParticleSystem Particles;

	private Mesh BeamMesh;

	private Mesh BeamMask;

	private Mesh SolidCubes;

	private Mesh SmallCubes;

	private Mesh MatrixMesh;

	private Mesh RaysMesh;

	private Mesh FlareMesh;

	private ArtObjectInstance TinyChapeau;

	private BackgroundPlane[] StatuePlanes;

	private NesGlitches Glitches;

	private readonly Texture2D[] MatrixWords = new Texture2D[8];

	private Mesh DealGlassesPlane;

	private Mesh TrialRaysMesh;

	private Mesh TrialFlareMesh;

	private float TrialTimeAccumulator;

	private SoundEffect sCollectFez;

	private SoundEffect sHexaTalk;

	private SoundEffect sAmbientHex;

	private SoundEffect sBeamGrow;

	private SoundEffect sExplode;

	private SoundEffect sGomezBeamUp;

	private SoundEffect sTinyBeam;

	private SoundEffect sMatrixRampUp;

	private SoundEffect sHexSlowDown;

	private SoundEffect sRayExplosion;

	private SoundEffect sHexRise;

	private SoundEffect sGomezBeamAppear;

	private SoundEffect sNightTransition;

	private SoundEffect sHexAlign;

	private SoundEffect sStarTrails;

	private SoundEffect sWhiteOut;

	private SoundEffect sHexDisappear;

	private SoundEffect sSparklyParticles;

	private SoundEffect sTrialWhiteOut;

	private SoundEffect[] sHexDrones;

	private SoundEmitter eAmbientHex;

	private SoundEmitter eSparklyParticles;

	private SoundEmitter eHexaTalk;

	private SoundEmitter eHexDrone;

	private int currentDroneIndex;

	private float SpinSpeed;

	private float DestinationSpinSpeed;

	private float SincePhaseStarted;

	private float LastPhaseRadians;

	private float OriginalSpin;

	private float CameraSpinSpeed;

	private float CameraSpins;

	private float DestinationSpins;

	private float ExplodeSpeed;

	private Phase CurrentPhase;

	private Quaternion RotationFrom;

	private float WhiteOutFactor;

	private bool playedRise1;

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public IPlaneParticleSystems PlaneParticleSystems { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IArtObjectService ArtObjectService { get; set; }

	[ServiceDependency]
	public IGameService GameService { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IInputManager InputManager { get; set; }

	[ServiceDependency]
	public ISpeechBubbleManager SpeechBubble { get; set; }

	[ServiceDependency(Optional = true)]
	public IWalkToService WalkTo { protected get; set; }

	public EldersHexahedron(Game game, ArtObjectInstance aoInstance)
		: base(game)
	{
		base.UpdateOrder = 20;
		base.DrawOrder = 101;
		AoInstance = aoInstance;
	}

	public override void Initialize()
	{
		base.Initialize();
		StarField obj = new StarField(base.Game)
		{
			Opacity = 0f,
			HasHorizontalTrails = true,
			FollowCamera = true
		};
		StarField component = obj;
		Starfield = obj;
		ServiceHelper.AddComponent(component);
		ServiceHelper.AddComponent(SfRenderer = new StarfieldRenderer(base.Game, this));
		StatuePlanes = LevelManager.BackgroundPlanes.Values.Where((BackgroundPlane x) => x.Id >= 0).ToArray();
		DealGlassesPlane = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = false,
			SamplerState = SamplerState.PointClamp
		};
		DealGlassesPlane.AddFace(new Vector3(1f, 0.25f, 1f), Vector3.Zero, FaceOrientation.Right, centeredOnOrigin: true, doublesided: true);
		ArtObject artObject = CMProvider.CurrentLevel.Load<ArtObject>("Art Objects/TINY_CHAPEAUAO");
		int num = IdentifierPool.FirstAvailable(LevelManager.ArtObjects);
		TinyChapeau = new ArtObjectInstance(artObject)
		{
			Id = num
		};
		LevelManager.ArtObjects.Add(num, TinyChapeau);
		TinyChapeau.Initialize();
		TinyChapeau.Hidden = true;
		TinyChapeau.ArtObject.Group.Position = new Vector3(-0.125f, 0.375f, -0.125f);
		TinyChapeau.ArtObject.Group.BakeTransformInstanced<VertexPositionNormalTextureInstance, Matrix>();
		BeamMesh = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = false,
			Material = 
			{
				Diffuse = new Vector3(221f, 178f, 255f) / 255f
			}
		};
		Group vg = BeamMesh.AddFace(new Vector3(1f, 1f, 1f), Vector3.Zero, FaceOrientation.Right, centeredOnOrigin: true, doublesided: true);
		Group hg = BeamMesh.AddFace(new Vector3(2f, 1f, 2f), Vector3.Zero, FaceOrientation.Right, centeredOnOrigin: true, doublesided: true);
		hg.Material = new Material
		{
			Opacity = 0.4f,
			Diffuse = new Vector3(221f, 178f, 255f) / 255f
		};
		hg.Enabled = false;
		BeamMask = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = false
		};
		BeamMask.AddFace(new Vector3(1f, 1f, 1f), Vector3.Zero, FaceOrientation.Right, centeredOnOrigin: true, doublesided: true);
		MatrixMesh = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = false,
			Blending = BlendingMode.Multiply2X
		};
		RaysMesh = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = false,
			Blending = BlendingMode.Alphablending
		};
		FlareMesh = new Mesh
		{
			DepthWrites = false,
			AlwaysOnTop = false,
			SamplerState = SamplerState.LinearClamp,
			Blending = BlendingMode.Alphablending
		};
		FlareMesh.AddFace(new Vector3(1f, 1f, 1f), Vector3.Zero, FaceOrientation.Right, centeredOnOrigin: true, doublesided: true);
		TrialRaysMesh = new Mesh
		{
			Blending = BlendingMode.Additive,
			SamplerState = SamplerState.AnisotropicClamp,
			DepthWrites = false,
			AlwaysOnTop = true
		};
		TrialFlareMesh = new Mesh
		{
			Blending = BlendingMode.Alphablending,
			SamplerState = SamplerState.AnisotropicClamp,
			DepthWrites = false,
			AlwaysOnTop = true
		};
		TrialFlareMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Right, centeredOnOrigin: true);
		LoadSounds();
		AoInstance.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 2f) * Quaternion.CreateFromAxisAngle(Vector3.Right, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 4f);
		Origin = AoInstance.Position;
		AoRotationOrigin = Quaternion.CreateFromAxisAngle(Vector3.Forward, (float)Math.PI / 4f) * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 4f);
		AoInstance.Material = new Material();
		PlayerManager.Position = new Vector3(18.5f, 27.46875f, 34.5f);
		PlayerManager.Position = PlayerManager.Position * Vector3.UnitY + FezMath.XZMask * AoInstance.Position;
		GomezOrigin = PlayerManager.Position;
		CameraManager.Center = new Vector3(18.5f, 27.46875f, 34.5f);
		CameraManager.Center = CameraManager.Center * Vector3.UnitY + FezMath.XZMask * AoInstance.Position + new Vector3(0f, 4f, 0f);
		CameraManager.SnapInterpolation();
		CameraOrigin = CameraManager.Center;
		while (!PlayerManager.CanControl)
		{
			PlayerManager.CanControl = true;
		}
		PlayerManager.CanControl = false;
		CameraManager.Constrained = true;
		PlayerManager.HideFez = true;
		GenerateCubes();
		SpinSpeed = 225f;
		sHexSlowDown.Emit();
		eAmbientHex = sAmbientHex.Emit(loop: true, 0f, 0.5f);
		Vector3 vector = -CameraManager.Viewpoint.ForwardVector() * 4f;
		PlaneParticleSystems.Add(Particles = new PlaneParticleSystem(base.Game, 200, new PlaneParticleSystemSettings
		{
			SpawnVolume = new BoundingBox
			{
				Min = PlayerManager.Position + new Vector3(-1f, 5f, -1f) + vector,
				Max = PlayerManager.Position + new Vector3(1f, 20f, 1f) + vector
			},
			Velocity = 
			{
				Base = new Vector3(0f, -0.5f, 0f),
				Variation = new Vector3(0f, -0.25f, 0.1f)
			},
			SpawningSpeed = 12f,
			ParticleLifetime = 15f,
			Acceleration = -0.1f,
			SizeBirth = new Vector3(0.125f, 0.125f, 0.125f),
			ColorBirth = Color.Black,
			ColorLife = 
			{
				Base = new Color(0.6f, 0.6f, 0.6f, 1f),
				Variation = new Color(0.1f, 0.1f, 0.1f, 0f)
			},
			ColorDeath = Color.Black,
			FullBright = true,
			RandomizeSpawnTime = true,
			FadeInDuration = 0.25f,
			FadeOutDuration = 0.5f,
			BlendingMode = BlendingMode.Additive
		}));
		Particles.Enabled = false;
		DrawActionScheduler.Schedule(delegate
		{
			DealGlassesPlane.Effect = new DefaultEffect.Textured();
			DealGlassesPlane.Texture = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures" + (GameState.SaveData.Finished64 ? "/deal_with_3d" : "/deal_with_it"));
			BeamMesh.Effect = new DefaultEffect.Textured();
			vg.Texture = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/VerticalGradient");
			hg.Texture = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/HorizontalGradient");
			BeamMask.Effect = new DefaultEffect.Textured();
			MatrixMesh.Effect = new MatrixEffect();
			RaysMesh.Effect = new DefaultEffect.VertexColored();
			FlareMesh.Effect = new DefaultEffect.Textured();
			FlareMesh.Texture = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/flare_alpha");
			TrialRaysMesh.Effect = new DefaultEffect.VertexColored();
			TrialFlareMesh.Effect = new DefaultEffect.Textured();
			TrialFlareMesh.Texture = FlareMesh.Texture;
			for (int i = 1; i < 9; i++)
			{
				MatrixWords[i - 1] = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/zuish_matrix/" + i);
			}
			Particles.Settings.Texture = CMProvider.Global.Load<Texture2D>("Background Planes/dust_particle");
			Particles.RefreshTexture();
		});
		LevelManager.LevelChanged += Kill;
	}

	private void LoadSounds()
	{
		sCollectFez = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Collects/CollectFez");
		sHexaTalk = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Npc/HexahedronTalk");
		sAmbientHex = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/AmbientHex");
		sBeamGrow = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/BeamGrow");
		sExplode = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/Explode");
		sGomezBeamUp = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/GomezBeamUp");
		sHexSlowDown = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/HexSlowDown");
		sMatrixRampUp = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/MatrixRampUp");
		sRayExplosion = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/RayExplosion");
		sTinyBeam = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/TinyBeam");
		sHexRise = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/HexRise");
		sGomezBeamAppear = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/GomezBeamAppear");
		sNightTransition = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/NightTransition");
		sHexAlign = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/HexAlign");
		sStarTrails = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/StarTrails");
		sWhiteOut = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/WhiteOut");
		sHexDisappear = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/HexDisappear");
		sSparklyParticles = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/SparklyParticles");
		sTrialWhiteOut = CMProvider.Global.Load<SoundEffect>("Sounds/Ending/Pyramid/WhiteOut");
		sHexDrones = new SoundEffect[5]
		{
			CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/HexDrones/HexDrone1"),
			CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/HexDrones/HexDrone2"),
			CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/HexDrones/HexDrone3"),
			CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/HexDrones/HexDrone4"),
			CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Intro/Elders/HexDrones/HexDrone5")
		};
	}

	private void GenerateCubes()
	{
		Vector3[] array = new Vector3[64];
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				for (int k = 0; k < 4; k++)
				{
					array[i * 16 + j * 4 + k] = new Vector3((float)i - 1.5f, (float)j - 1.5f, (float)k - 1.5f);
				}
			}
		}
		SolidCubes = new Mesh
		{
			Blending = BlendingMode.Opaque
		};
		SmallCubes = new Mesh
		{
			Blending = BlendingMode.Opaque
		};
		Mesh smallCubes = SmallCubes;
		Quaternion rotation = (SolidCubes.Rotation = AoRotationOrigin);
		smallCubes.Rotation = rotation;
		Trile trile = LevelManager.ActorTriles(ActorType.CubeShard).FirstOrDefault();
		Trile trile2 = LevelManager.ActorTriles(ActorType.GoldenCube).FirstOrDefault();
		Vector3[] array2 = array;
		foreach (Vector3 vector in array2)
		{
			Group group = SolidCubes.AddGroup();
			group.Geometry = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(trile.Geometry.Vertices.ToArray(), trile.Geometry.Indices, trile.Geometry.PrimitiveType);
			group.Position = vector;
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)RandomHelper.Random.Next(0, 4) * ((float)Math.PI / 2f));
			group.BakeTransformWithNormal<VertexPositionNormalTextureInstance>();
			group.CustomData = new ShardProjectionData
			{
				Direction = vector * RandomHelper.Between(0.5, 5.0),
				Spin = Quaternion.CreateFromAxisAngle(RandomHelper.NormalizedVector(), RandomHelper.Between(0.0, 0.0031415929552167654))
			};
			Group group2 = SmallCubes.AddGroup();
			group2.Geometry = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(trile2.Geometry.Vertices.ToArray(), trile2.Geometry.Indices, trile2.Geometry.PrimitiveType);
			group2.Position = vector * RandomHelper.Between(0.5, 1.0);
			group2.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)RandomHelper.Random.Next(0, 4) * ((float)Math.PI / 2f));
			group2.BakeTransformWithNormal<VertexPositionNormalTextureInstance>();
			group2.CustomData = new ShardProjectionData
			{
				Direction = vector * RandomHelper.Between(0.5, 5.0),
				Spin = Quaternion.CreateFromAxisAngle(RandomHelper.NormalizedVector(), RandomHelper.Between(0.0, 0.0031415929552167654))
			};
		}
		DrawActionScheduler.Schedule(delegate
		{
			SolidCubes.Effect = new DefaultEffect.LitTextured
			{
				Specular = true,
				Emissive = 0.5f,
				AlphaIsEmissive = true
			};
			SmallCubes.Effect = new DefaultEffect.LitTextured
			{
				Specular = true
			};
			Mesh solidCubes = SolidCubes;
			Dirtyable<Texture> texture = (SmallCubes.Texture = LevelManager.TrileSet.TextureAtlas);
			solidCubes.Texture = texture;
		});
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		ServiceHelper.RemoveComponent(Starfield);
		ServiceHelper.RemoveComponent(SfRenderer);
		ServiceHelper.RemoveComponent(Glitches);
		SolidCubes.Dispose();
		SmallCubes.Dispose();
		MatrixMesh.Dispose();
		BeamMask.Dispose();
		BeamMesh.Dispose();
		TrialRaysMesh.Dispose();
		TrialFlareMesh.Dispose();
		FlareMesh.Dispose();
		RaysMesh.Dispose();
		DealGlassesPlane.Dispose();
		GameState.SkyOpacity = 1f;
		bool visible = (base.Enabled = false);
		base.Visible = visible;
		LevelManager.LevelChanged -= Kill;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap || CameraManager.Viewpoint == Viewpoint.Perspective || (CurrentPhase == Phase.WaitSpin && GameState.InCutscene))
		{
			return;
		}
		AoInstance.Visible = true;
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		SincePhaseStarted += num;
		switch (CurrentPhase)
		{
		case Phase.ZoomOut:
		{
			float num2 = FezMath.Saturate((SincePhaseStarted - 1.5f) / 5f);
			float amount = Easing.EaseInOut(num2, EasingType.Quadratic);
			if (num2 > 0f && !playedRise1)
			{
				sHexRise.Emit();
				playedRise1 = true;
			}
			CameraManager.PixelsPerTrixel = MathHelper.Lerp(3f, 2f, amount);
			CameraManager.Center = Vector3.Lerp(CameraOrigin, CameraOrigin + new Vector3(0f, 4f, 0f), amount);
			CameraManager.SnapInterpolation();
			AoInstance.Position = Vector3.Lerp(Origin + new Vector3(0f, 3.5f, 0f), Origin + new Vector3(0f, 8f, 0f), amount);
			if (SpinSpeed > 0.5f)
			{
				SpinSpeed *= 0.98f;
			}
			AoInstance.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, SpinSpeed * 0.01f) * AoInstance.Rotation;
			if (num2 == 1f)
			{
				PlayerManager.CanControl = true;
				CurrentPhase = Phase.Talk1;
				SincePhaseStarted = 0f;
				eHexaTalk = sHexaTalk.Emit(loop: true);
				Talk1();
			}
			break;
		}
		case Phase.Talk1:
			LastPhaseRadians = FezMath.WrapAngle(SincePhaseStarted / 2f);
			AoInstance.Position = Origin + new Vector3(0f, 8f + (float)Math.Sin(LastPhaseRadians) * 0.25f, 0f);
			AoInstance.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, SpinSpeed * 0.01f) * AoInstance.Rotation;
			break;
		case Phase.Beam:
		{
			float opacity = Easing.EaseInOut(FezMath.Saturate(SincePhaseStarted / 5f), EasingType.Sine);
			Starfield.Opacity = opacity;
			opacity = Easing.EaseInOut(FezMath.Saturate((SincePhaseStarted - 1f) / 3.5f), EasingType.Sine);
			if (opacity > 0f && sHexAlign != null)
			{
				sHexAlign.Emit();
				sHexAlign = null;
			}
			if (SincePhaseStarted < 1f)
			{
				AoInstance.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, SpinSpeed * 0.01f) * AoInstance.Rotation;
				RotationFrom = AoInstance.Rotation;
			}
			else
			{
				RotationFrom = Quaternion.CreateFromAxisAngle(Vector3.UnitY, SpinSpeed * 0.01f) * RotationFrom;
				AoInstance.Rotation = Quaternion.Slerp(RotationFrom, AoRotationOrigin, opacity);
			}
			AoInstance.Position = Origin + new Vector3(0f, 8f + (float)Math.Sin(SincePhaseStarted / 2f + LastPhaseRadians) * 0.25f, 0f);
			opacity = Easing.EaseOut(FezMath.Saturate((SincePhaseStarted - 5f) / 2f), EasingType.Quadratic);
			BeamMesh.Material.Opacity = opacity * 0.425f;
			if (opacity > 0f && RandomHelper.Probability(Math.Pow(1f - opacity, 5.0)))
			{
				BeamMesh.Material.Opacity = RandomHelper.Between(0.0, 0.5);
			}
			BeamMesh.Position = new Vector3(Origin.X, (AoInstance.Position.Y + 26f) / 2f, Origin.Z) + CameraManager.InverseView.Forward * 5f;
			BeamMesh.Scale = new Vector3(5.5625f, AoInstance.Position.Y - 26f, 5.5625f);
			if (opacity > 0f && sGomezBeamAppear != null)
			{
				sGomezBeamAppear.Emit();
				sGomezBeamAppear = null;
			}
			float num11 = PlayerManager.Position.Y + 0.1875f;
			BeamMask.Position = new Vector3(Origin.X, (26f + num11) / 2f, Origin.Z) + CameraManager.InverseView.Forward * 5f + CameraManager.InverseView.Right / 32f;
			BeamMask.Rotation = BeamMesh.Rotation;
			BeamMask.Scale = new Vector3(PlayerManager.Size.X + 7f / 32f, num11 - 26f, PlayerManager.Size.Z + 7f / 32f);
			if (SincePhaseStarted > 5.5f)
			{
				opacity = Easing.EaseOut(FezMath.Saturate((SincePhaseStarted - 5.5f) / 3f), EasingType.Sine);
				if (sGomezBeamUp != null)
				{
					sGomezBeamUp.Emit();
					sGomezBeamUp = null;
				}
				PlayerManager.Action = ActionType.Jumping;
				PlayerManager.Velocity = Vector3.Zero;
				PlayerManager.Position = Vector3.Lerp(GomezOrigin, GomezOrigin + new Vector3(0f, 3f, 0f), opacity);
			}
			if (opacity == 1f)
			{
				Starfield.Enabled = true;
				CurrentPhase = Phase.MatrixSpin;
				LastPhaseRadians = FezMath.WrapAngle(SincePhaseStarted / 2f + LastPhaseRadians);
				SincePhaseStarted = 0f;
				Vector3 position = CameraManager.Position;
				Vector3 center = CameraManager.Center;
				OriginalSpin = (float)Math.Atan2(position.X - center.X, position.Z - center.Z);
				CameraSpinSpeed = 5E-05f;
				sMatrixRampUp.Emit();
			}
			break;
		}
		case Phase.MatrixSpin:
		{
			PlayerManager.Action = ActionType.Jumping;
			PlayerManager.Velocity = Vector3.Zero;
			PlayerManager.Position = GomezOrigin + new Vector3(0f, 3f, 0f);
			CameraManager.ForceInterpolation = true;
			AoInstance.Position = Origin + new Vector3(0f, 8f + (float)Math.Sin(SincePhaseStarted / 2f + LastPhaseRadians) * 0.25f, 0f);
			if ((double)SincePhaseStarted > 6.5 && sStarTrails != null)
			{
				sStarTrails.Emit();
				sStarTrails = null;
			}
			if (SincePhaseStarted > 8f && sWhiteOut != null)
			{
				sWhiteOut.Emit();
				sWhiteOut = null;
			}
			float num12 = 0.05f + 2f * Easing.EaseIn(SincePhaseStarted / 10f, EasingType.Quintic);
			if (SincePhaseStarted > 10f)
			{
				num12 = 0.05f + (1f - (SincePhaseStarted - 10f) / 3f);
			}
			if (RandomHelper.Probability(num12))
			{
				for (int i = 0; (double)i <= Math.Floor(num12); i++)
				{
					int num13 = RandomHelper.Random.Next(0, 8);
					Texture2D texture2D = MatrixWords[num13];
					Vector3 size = new Vector3(texture2D.Width, texture2D.Height, texture2D.Width) / 16f;
					Group group2 = MatrixMesh.AddFace(size, new Vector3(0f, (0f - size.Y) / 2f, 0f), FaceOrientation.Right, centeredOnOrigin: true, doublesided: true);
					group2.Texture = texture2D;
					float num14 = RandomHelper.Between(BeamMesh.Scale.X * -0.5f + size.X / 2f, BeamMesh.Scale.X * 0.5f - size.X / 2f);
					group2.Position += new Vector3(num14, BeamMesh.Scale.Y - 1f, num14);
					group2.CustomData = RandomHelper.Between(Math.Max(1f, SincePhaseStarted / 3f), SincePhaseStarted * 3f);
				}
			}
			(MatrixMesh.Effect as MatrixEffect).MaxHeight = MatrixMesh.Position.Y + BeamMesh.Scale.Y / 2f;
			for (int num15 = MatrixMesh.Groups.Count - 1; num15 >= 0; num15--)
			{
				Group group3 = MatrixMesh.Groups[num15];
				group3.Position -= new Vector3(0f, num * (float)group3.CustomData, 0f);
				group3.CustomData = (float)group3.CustomData * (1f + num / 2f);
				if (group3.Position.Y < 0f - BeamMesh.Scale.Y)
				{
					MatrixMesh.RemoveGroupAt(num15);
				}
			}
			if (SincePhaseStarted > 8f && SincePhaseStarted < 10f)
			{
				WhiteOutFactor = (float)Math.Pow((SincePhaseStarted - 8f) / 2f, 6.0);
			}
			else if (SincePhaseStarted > 10f && SincePhaseStarted < 13f)
			{
				WhiteOutFactor = (float)Math.Pow(1f - (SincePhaseStarted - 10f) / 3f, 2.0);
			}
			else
			{
				WhiteOutFactor = 0f;
			}
			if (SincePhaseStarted < 10f)
			{
				if ((double)CameraSpinSpeed < 0.07)
				{
					CameraSpinSpeed *= 1.0175f;
				}
				CameraSpins += CameraSpinSpeed;
			}
			else
			{
				Starfield.ReverseTiming = true;
				float num16 = Easing.EaseOut(FezMath.Saturate((SincePhaseStarted - 10f) / 3f), EasingType.Quadratic);
				CameraSpins = MathHelper.Lerp(DestinationSpins, FezMath.Round(DestinationSpins + CameraSpinSpeed * 60f), num16);
				MatrixMesh.Material.Diffuse = new Vector3(1f - num16);
				if (SincePhaseStarted > 14f)
				{
					CameraManager.ForceInterpolation = false;
					PlayerManager.CanControl = true;
					LastPhaseRadians = FezMath.WrapAngle(SincePhaseStarted / 2f + LastPhaseRadians);
					SincePhaseStarted = 0f;
					CurrentPhase = Phase.Talk2;
					eHexaTalk = sHexaTalk.Emit(loop: true);
					Talk2();
				}
			}
			CameraManager.Direction = Vector3.Normalize(new Vector3((float)Math.Sin(OriginalSpin + CameraSpins * ((float)Math.PI * 2f)), 0f, (float)Math.Cos(OriginalSpin + CameraSpins * ((float)Math.PI * 2f))));
			CameraManager.SnapInterpolation();
			BeamMesh.Position = new Vector3(Origin.X, (AoInstance.Position.Y + 26f) / 2f, Origin.Z) + CameraManager.InverseView.Forward * 5f;
			BeamMesh.Rotation = CameraManager.Rotation * Quaternion.CreateFromAxisAngle(Vector3.Up, -(float)Math.PI / 2f);
			float num17 = PlayerManager.Position.Y + 0.1875f;
			BeamMask.Position = new Vector3(Origin.X, (26f + num17) / 2f, Origin.Z) + CameraManager.InverseView.Forward * 5f + CameraManager.InverseView.Right / 32f;
			BeamMask.Rotation = BeamMesh.Rotation;
			MatrixMesh.Rotation = BeamMask.Rotation;
			MatrixMesh.Position = BeamMask.Position;
			AoInstance.Rotation = BeamMesh.Rotation * AoRotationOrigin;
			break;
		}
		case Phase.Talk2:
			AoInstance.Position = Origin + new Vector3(0f, 8f + (float)Math.Sin(SincePhaseStarted / 2f + LastPhaseRadians) * 0.25f, 0f);
			if (InputManager.CancelTalk == FezButtonState.Pressed)
			{
				SpeechBubble.Hide();
			}
			PlayerManager.Action = ActionType.Jumping;
			PlayerManager.Velocity = Vector3.Zero;
			PlayerManager.Position = GomezOrigin + new Vector3(0f, 3f, 0f);
			break;
		case Phase.Disappear:
		{
			float num9 = Easing.EaseInOut(FezMath.Saturate(SincePhaseStarted / 2f), EasingType.Sine);
			AoInstance.Material.Opacity = 1f - num9;
			BeamMesh.Material.Opacity = AoInstance.Material.Opacity * 0.425f;
			AoInstance.MarkDirty();
			PlayerManager.Action = ActionType.Falling;
			PlayerManager.Velocity = Vector3.Zero;
			PlayerManager.Position = Vector3.Lerp(GomezOrigin, GomezOrigin + new Vector3(0f, 3f, 0f), 1f - num9);
			float num10 = PlayerManager.Position.Y + 0.1875f;
			BeamMask.Position = new Vector3(Origin.X, (26f + num10) / 2f, Origin.Z) + CameraManager.InverseView.Forward * 5f + CameraManager.InverseView.Right / 32f;
			BeamMask.Rotation = BeamMesh.Rotation;
			BeamMask.Scale = new Vector3(PlayerManager.Size.X + 7f / 32f, num10 - 26f, PlayerManager.Size.Z + 7f / 32f);
			if (num9 >= 0.5f)
			{
				if (!Particles.Enabled)
				{
					eSparklyParticles = sSparklyParticles.Emit(loop: true);
				}
				Particles.Enabled = true;
				eSparklyParticles.VolumeFactor = num9 - 0.5f;
			}
			if (num9 == 1f)
			{
				SincePhaseStarted = 0f;
				AoInstance.Hidden = true;
				AoInstance.Visible = false;
				AoInstance.MarkDirty();
				CurrentPhase = Phase.FezBeamGrow;
			}
			break;
		}
		case Phase.FezBeamGrow:
			if (SincePhaseStarted > 2f)
			{
				PlayerManager.Action = ActionType.LookingUp;
			}
			BeamMesh.Position = new Vector3(Origin.X, AoInstance.Position.Y, Origin.Z) + CameraManager.InverseView.Forward * 5f;
			BeamMesh.Groups[1].Enabled = true;
			if (SincePhaseStarted < 2f)
			{
				if (sTinyBeam != null)
				{
					sTinyBeam.Emit();
					sTinyBeam = null;
				}
				float num4 = Easing.EaseIn(FezMath.Saturate(SincePhaseStarted), EasingType.Quintic);
				BeamMesh.Material.Opacity = num4 * 0.6f;
				BeamMesh.Scale = new Vector3(0.0625f * num4, CameraManager.Radius * 1.5f, 0.0625f * num4);
			}
			else if (SincePhaseStarted > 3f && SincePhaseStarted < 3.5f)
			{
				if (sBeamGrow != null)
				{
					ScheduleFades();
					sBeamGrow.Emit();
					sBeamGrow = null;
				}
				float num5 = (float)Math.Pow(FezMath.Saturate((SincePhaseStarted - 3f) / 0.5f), 5.0);
				BeamMesh.Scale = new Vector3(0.0625f + CameraManager.Radius * num5, CameraManager.Radius, 0.0625f + CameraManager.Radius * num5);
				BeamMesh.Material.Opacity = 0.6f + num5 * 0.4f;
			}
			else if ((double)SincePhaseStarted > 3.5)
			{
				BeamMesh.Scale = new Vector3(2f, CameraManager.Radius, 2f);
				BeamMesh.Material.Opacity = 0.5f;
			}
			if (SincePhaseStarted > 6f)
			{
				SincePhaseStarted = 0f;
				CurrentPhase = Phase.FezComeDown;
			}
			break;
		case Phase.FezComeDown:
		{
			if (!GameState.SaveData.IsNewGamePlus)
			{
				PlayerManager.Action = ActionType.LookingUp;
				TinyChapeau.Hidden = false;
				TinyChapeau.Visible = true;
				TinyChapeau.ArtObject.Group.Enabled = true;
			}
			Vector3 value = PlayerManager.Position + new Vector3(0f, 20f, 0f);
			Vector3 value2 = PlayerManager.Position + new Vector3(0f, PlayerManager.Size.Y / 2f + 0.1875f, 0f) + PlayerManager.LookingDirection.Sign() * CameraManager.Viewpoint.RightVector() * -4f / 16f;
			if (GameState.SaveData.IsNewGamePlus)
			{
				value2 += new Vector3(0f, -21f / 64f, -7f / 32f);
			}
			float num3 = MathHelper.Lerp(Easing.EaseOut(FezMath.Saturate(SincePhaseStarted / 20f), EasingType.Sine), Easing.EaseOut(FezMath.Saturate(SincePhaseStarted / 20f), EasingType.Quadratic), 0.5f);
			if (GameState.SaveData.IsNewGamePlus)
			{
				DealGlassesPlane.Position = Vector3.Lerp(value, value2, num3) - CameraManager.Viewpoint.ForwardVector() * 4f;
			}
			else
			{
				TinyChapeau.Position = Vector3.Lerp(value, value2, num3) - CameraManager.Viewpoint.ForwardVector() * 4f;
				TinyChapeau.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (1f - num3) * 20f);
			}
			if (SincePhaseStarted > 4f)
			{
				CameraManager.PixelsPerTrixel = 15f;
				CameraManager.Center = (GameState.SaveData.IsNewGamePlus ? DealGlassesPlane.Position : TinyChapeau.Position) + new Vector3(0f, -0.25f, 0f);
				CameraManager.SnapInterpolation();
				eSparklyParticles.VolumeFactor = 1f;
				if (GameState.SaveData.IsNewGamePlus)
				{
					PlayerManager.Action = ActionType.Standing;
				}
			}
			else if (GameState.SaveData.IsNewGamePlus)
			{
				PlayerManager.Action = ActionType.LookingUp;
			}
			if (SincePhaseStarted >= 19.75f)
			{
				TinyChapeau.Hidden = true;
				TinyChapeau.Visible = false;
				PlayerManager.HideFez = false;
				Particles.FadeOutAndDie(0.1f);
				eSparklyParticles.FadeOutAndDie(1.25f);
				sRayExplosion.Emit();
				if (GameState.SaveData.Finished64)
				{
					GameState.StereoMode = true;
				}
				SincePhaseStarted = 0f;
				CurrentPhase = Phase.Beamsplode;
				RaysMesh.Position = TinyChapeau.Position - new Vector3(0f, 0.0625f, 0f) + CameraManager.Viewpoint.ForwardVector() * 10f;
				ServiceHelper.AddComponent(new ScreenFade(base.Game)
				{
					FromColor = Color.White,
					ToColor = ColorEx.TransparentWhite,
					Duration = 0.125f,
					EaseOut = true
				});
			}
			break;
		}
		case Phase.Beamsplode:
		{
			bool flag = false;
			if (SincePhaseStarted > 2.5f)
			{
				CameraManager.PixelsPerTrixel = 10f;
				CameraManager.Center = CameraOrigin - new Vector3(0f, 3f, 0f);
				CameraManager.SnapInterpolation();
			}
			else if (SincePhaseStarted > 0.25f)
			{
				flag = true;
				CameraManager.PixelsPerTrixel = 1f;
				CameraManager.Center = CameraOrigin + new Vector3(0f, 15f, 0f);
				CameraManager.SnapInterpolation();
				BeamMesh.Material.Opacity = (BeamMesh.Groups[1].Material.Opacity = 0f);
			}
			RaysMesh.Position = PlayerManager.Position + new Vector3(0f, PlayerManager.Size.Y / 2f + 0.1875f, 0f) + PlayerManager.LookingDirection.Sign() * CameraManager.Viewpoint.RightVector() * -4f / 16f - new Vector3(0f, 0.0625f, 0f) + CameraManager.Viewpoint.ForwardVector() * 10f;
			if (SincePhaseStarted < 2.5f && RandomHelper.Probability(flag ? 0.5f : 0.25f))
			{
				AddSplodeBeam(flag);
			}
			for (int num6 = RaysMesh.Groups.Count - 1; num6 >= 0; num6--)
			{
				Group group = RaysMesh.Groups[num6];
				RayData rayData = (RayData)group.CustomData;
				group.Rotation *= Quaternion.CreateFromAxisAngle(CameraManager.Viewpoint.ForwardVector(), rayData.Speed * 0.01f * (float)rayData.Sign);
				FezVertexPositionColor fezVertexPositionColor = (group.Geometry as IndexedUserPrimitives<FezVertexPositionColor>).Vertices[1];
				Vector3 vector = Vector3.Transform(fezVertexPositionColor.Position, group.Rotation);
				bool flag2 = group.Geometry.VertexCount > 2;
				Vector3 vector2 = Vector3.Zero;
				if (flag2)
				{
					FezVertexPositionColor fezVertexPositionColor2 = (group.Geometry as IndexedUserPrimitives<FezVertexPositionColor>).Vertices[2];
					vector2 = Vector3.Transform(fezVertexPositionColor2.Position, group.Rotation);
				}
				rayData.SinceAlive += num;
				rayData.Speed *= 0.975f;
				group.CustomData = rayData;
				double num7 = Math.Atan2(vector.Y, vector.Z);
				double num8 = Math.Atan2(vector2.Y, vector2.Z);
				if (num7 < 0.10000000149011612 || num7 > 3.041592652099677 || (flag2 && (num8 < 0.10000000149011612 || num8 > 3.041592652099677)))
				{
					group.Material.Opacity *= 0.8f;
				}
				else if (rayData.SinceAlive > 1f)
				{
					group.Material.Opacity *= 0.9f;
				}
				else
				{
					group.Material.Opacity = 1f;
				}
				if (SincePhaseStarted > 2.5f)
				{
					group.Material.Opacity *= 0.6f;
					group.Scale *= 0.6f;
				}
				if (num7 < 0.0 || group.Material.Opacity < 0.004f)
				{
					RaysMesh.RemoveGroupAt(num6);
				}
			}
			if (SincePhaseStarted > 3f)
			{
				RaysMesh.Groups.Clear();
				SincePhaseStarted = 0f;
				CurrentPhase = Phase.Yay;
				ServiceHelper.AddComponent(Glitches = new NesGlitches(base.Game));
			}
			break;
		}
		case Phase.Yay:
			DealGlassesPlane.Material.Opacity = FezMath.Saturate(1f - SincePhaseStarted);
			if (SincePhaseStarted > 1f)
			{
				PlayerManager.Action = ActionType.Victory;
				sCollectFez.Emit();
				PlayerManager.CanControl = true;
				PlayerManager.CanRotate = true;
				if (GameState.SaveData.Finished32)
				{
					GameState.SaveData.HasFPView = true;
				}
				if (GameState.SaveData.Finished64)
				{
					GameState.SaveData.HasFPView = (GameState.SaveData.HasStereo3D = true);
				}
				CurrentPhase = Phase.WaitSpin;
				SincePhaseStarted = 0f;
				eHexDrone = sHexDrones[0].Emit(loop: true, 0f, 0f);
				AoInstance.Visible = false;
				CameraManager.ViewpointChanged += AddSpin;
				ScheduleText();
			}
			break;
		case Phase.WaitSpin:
			WaitSpin(num);
			break;
		case Phase.HexaExplode:
			HexaExplode(num);
			break;
		case Phase.ThatsIt:
			Glitches.ActiveGlitches = 0;
			Glitches.FreezeProbability = 0f;
			Kill();
			break;
		}
	}

	private void WaitSpin(float elapsedTime)
	{
		if (SincePhaseStarted < 3f && SincePhaseStarted > 1f)
		{
			float amount = Easing.EaseInOut(FezMath.Saturate((SincePhaseStarted - 1f) / 2f), EasingType.Sine);
			CameraManager.PixelsPerTrixel = MathHelper.Lerp(10f, 2f, amount);
			CameraManager.Center = Vector3.Lerp(CameraOrigin - new Vector3(0f, 3f, 0f), CameraOrigin + new Vector3(0f, 4f, 0f), Easing.EaseIn(FezMath.Saturate((SincePhaseStarted - 1f) / 2f), EasingType.Sine));
			AoInstance.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 2f) * Quaternion.CreateFromAxisAngle(Vector3.Right, (float)Math.Asin(Math.Sqrt(2.0) / Math.Sqrt(3.0))) * Quaternion.CreateFromAxisAngle(Vector3.Up, (float)Math.PI / 4f);
			CameraManager.SnapInterpolation();
			AoInstance.Visible = false;
			PlayerManager.CanRotate = true;
		}
		if (SincePhaseStarted < 2f)
		{
			return;
		}
		float num = Easing.EaseInOut(FezMath.Saturate(SincePhaseStarted - 3f), EasingType.Sine);
		AoInstance.Material.Opacity = num;
		if (eHexDrone != null)
		{
			eHexDrone.VolumeFactor = num;
		}
		AoInstance.Hidden = false;
		AoInstance.Visible = true;
		AoInstance.MarkDirty();
		if (Math.Abs(SpinSpeed) < Math.Abs(DestinationSpinSpeed))
		{
			SpinSpeed *= 1f + elapsedTime;
			if (Math.Abs(SpinSpeed) > Math.Abs(DestinationSpinSpeed))
			{
				SpinSpeed = DestinationSpinSpeed;
			}
		}
		if (Math.Abs(SpinSpeed) > 32f)
		{
			DestinationSpinSpeed *= 1f + elapsedTime;
			if (!GameState.IsTrialMode)
			{
				UpdateRays(elapsedTime * 2f);
				if (Math.Abs(SpinSpeed) > 50f)
				{
					UpdateRays(elapsedTime);
					UpdateRays(elapsedTime);
				}
			}
		}
		float num2 = Easing.EaseIn(SpinSpeed / 100f, EasingType.Quadratic);
		CameraManager.InterpolatedCenter += new Vector3(RandomHelper.Between(0f - num2, num2), RandomHelper.Between(0f - num2, num2), RandomHelper.Between(0f - num2, num2));
		if (!GameState.IsTrialMode && Math.Abs(SpinSpeed) > 30f)
		{
			Glitches.DisappearProbability = FezMath.Saturate(1f - (Math.Abs(SpinSpeed) - 30f) / 98f) * 0.1f;
			Glitches.ActiveGlitches = FezMath.Round(FezMath.Saturate((Math.Abs(SpinSpeed) - 30f) / 98f) * 75f);
			Glitches.FreezeProbability = Easing.EaseIn(FezMath.Saturate((Math.Abs(SpinSpeed) - 30f) / 98f), EasingType.Cubic) * 0.01f;
		}
		if (CameraManager.Viewpoint != Viewpoint.Perspective && !CameraManager.ProjectionTransition)
		{
			AoInstance.Position = CameraManager.Center + new Vector3(0f, (float)Math.Sin(SincePhaseStarted / 2f + LastPhaseRadians) * 0.25f, 0f);
			AoInstance.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, SpinSpeed * 0.01f) * AoInstance.Rotation;
		}
		if (Math.Abs(SpinSpeed) >= 105f)
		{
			if (PlayerManager.CanControl)
			{
				ServiceHelper.AddComponent(new ScreenFade(base.Game)
				{
					FromColor = Color.White,
					ToColor = ColorEx.TransparentWhite,
					Duration = 0.75f
				});
			}
			PlayerManager.CanControl = false;
			CameraManager.ViewpointChanged -= AddSpin;
			ExplodeSpeed = 0.0625f;
			CameraSpinSpeed = 1E-05f;
			CameraSpins = 0f;
			Vector3 position = CameraManager.Position;
			Vector3 center = CameraManager.Center;
			OriginalSpin = (float)Math.Atan2(position.X - center.X, position.Z - center.Z);
			ScheduleExplode();
		}
	}

	private void HexaExplode(float elapsedTime)
	{
		AoInstance.Hidden = true;
		AoInstance.Visible = false;
		Mesh smallCubes = SmallCubes;
		Vector3 position2 = (SolidCubes.Position = AoInstance.Position);
		smallCubes.Position = position2;
		if (SincePhaseStarted > 0.25f)
		{
			_ = SincePhaseStarted / 13f;
			if (sExplode != null)
			{
				sExplode.Emit();
				sExplode = null;
			}
			ExplodeSpeed *= 0.95f;
			foreach (Group group2 in SolidCubes.Groups)
			{
				group2.Position += ((ShardProjectionData)group2.CustomData).Direction * (0.005f + ExplodeSpeed + CameraSpinSpeed / 3f);
				group2.Rotation *= ((ShardProjectionData)group2.CustomData).Spin;
			}
			foreach (Group group3 in SmallCubes.Groups)
			{
				group3.Position += ((ShardProjectionData)group3.CustomData).Direction * (0.005f + ExplodeSpeed + CameraSpinSpeed / 3f);
				group3.Rotation *= ((ShardProjectionData)group3.CustomData).Spin;
			}
		}
		CameraSpinSpeed *= 1.01f;
		if (SincePhaseStarted < 10f)
		{
			CameraSpinSpeed = MathHelper.Min(CameraSpinSpeed, 0.004f);
		}
		CameraSpins += CameraSpinSpeed;
		CameraSpins += ExplodeSpeed / 5f;
		CameraManager.Direction = Vector3.Normalize(new Vector3((float)Math.Sin(OriginalSpin + CameraSpins * ((float)Math.PI * 2f)), 0f, (float)Math.Cos(OriginalSpin + CameraSpins * ((float)Math.PI * 2f))));
		CameraManager.SnapInterpolation();
		if (!GameState.IsTrialMode)
		{
			Glitches.ActiveGlitches = FezMath.Round(Easing.EaseIn(FezMath.Saturate(SincePhaseStarted / 13f), EasingType.Decic) * 400f + 2f);
			Glitches.FreezeProbability = ((SincePhaseStarted < 8f) ? 0f : ((SincePhaseStarted < 10f) ? 0.001f : ((SincePhaseStarted < 11f) ? 0.1f : 0.01f)));
			if (SincePhaseStarted > 13f)
			{
				Glitches.FreezeProbability = 1f;
			}
		}
		if (GameState.IsTrialMode)
		{
			UpdateRays(elapsedTime * CameraSpinSpeed);
			UpdateRays(elapsedTime * CameraSpinSpeed);
		}
		else
		{
			for (int num = TrialRaysMesh.Groups.Count - 1; num >= 0; num--)
			{
				Group group = TrialRaysMesh.Groups[num];
				group.Material.Diffuse = new Vector3(FezMath.Saturate(1f - SincePhaseStarted));
				group.Scale *= new Vector3(1.5f, 1f, 1f);
				if (FezMath.AlmostEqual(group.Material.Diffuse.X, 0f))
				{
					TrialRaysMesh.RemoveGroupAt(num);
				}
			}
		}
		if (!GameState.IsTrialMode && SincePhaseStarted > 15f)
		{
			CurrentPhase = Phase.ThatsIt;
			ServiceHelper.AddComponent(new Reboot(base.Game, "GOMEZ_HOUSE"));
		}
		else
		{
			if (!GameState.IsTrialMode || !(TrialTimeAccumulator > 7.5f))
			{
				return;
			}
			CurrentPhase = Phase.ThatsIt;
			GameState.SkipLoadScreen = true;
			ServiceHelper.AddComponent(new ScreenFade(base.Game)
			{
				FromColor = Color.White,
				ToColor = ColorEx.TransparentWhite,
				Duration = 2f
			});
			LevelManager.ChangeLevel("ARCH");
			Waiters.Wait(() => !GameState.Loading, delegate
			{
				GameState.SkipLoadScreen = false;
				base.Visible = false;
				while (!PlayerManager.CanControl)
				{
					PlayerManager.CanControl = true;
				}
				SoundManager.MusicVolumeFactor = 1f;
			});
		}
	}

	private void UpdateRays(float elapsedSeconds)
	{
		if (GameState.IsTrialMode)
		{
			if (TrialRaysMesh.Groups.Count < 50 && RandomHelper.Probability(0.25))
			{
				float num = 6f + RandomHelper.Centered(4.0);
				float num2 = RandomHelper.Between(0.5, num / 2.5f);
				Group group = TrialRaysMesh.AddGroup();
				group.Geometry = new IndexedUserPrimitives<FezVertexPositionTexture>(new FezVertexPositionTexture[6]
				{
					new FezVertexPositionTexture(new Vector3(0f, num2 / 2f * 0.1f, 0f), new Vector2(0f, 0f)),
					new FezVertexPositionTexture(new Vector3(num, num2 / 2f, 0f), new Vector2(1f, 0f)),
					new FezVertexPositionTexture(new Vector3(num, num2 / 2f * 0.1f, 0f), new Vector2(1f, 0.45f)),
					new FezVertexPositionTexture(new Vector3(num, (0f - num2) / 2f * 0.1f, 0f), new Vector2(1f, 0.55f)),
					new FezVertexPositionTexture(new Vector3(num, (0f - num2) / 2f, 0f), new Vector2(1f, 1f)),
					new FezVertexPositionTexture(new Vector3(0f, (0f - num2) / 2f * 0.1f, 0f), new Vector2(0f, 1f))
				}, new int[12]
				{
					0, 1, 2, 0, 2, 5, 5, 2, 3, 5,
					3, 4
				}, PrimitiveType.TriangleList);
				group.CustomData = new DotHost.RayState();
				group.Material = new Material
				{
					Diffuse = new Vector3(0f)
				};
				group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Forward, RandomHelper.Between(0.0, 6.2831854820251465));
			}
			for (int num3 = TrialRaysMesh.Groups.Count - 1; num3 >= 0; num3--)
			{
				Group group2 = TrialRaysMesh.Groups[num3];
				DotHost.RayState rayState = group2.CustomData as DotHost.RayState;
				rayState.Age += elapsedSeconds * 0.15f;
				float num4 = (float)Math.Sin(rayState.Age * ((float)Math.PI * 2f) - (float)Math.PI / 2f) * 0.5f + 0.5f;
				num4 = Easing.EaseOut(num4, EasingType.Quintic);
				num4 = Easing.EaseOut(num4, EasingType.Quintic);
				group2.Material.Diffuse = Vector3.Lerp(Vector3.One, rayState.Tint.ToVector3(), 0.05f) * 0.15f * num4;
				float speed = rayState.Speed;
				group2.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.Forward, elapsedSeconds * speed * (0.1f + Easing.EaseIn(TrialTimeAccumulator / 3f, EasingType.Quadratic) * 0.2f));
				group2.Scale = new Vector3(num4 * 0.75f + 0.25f, num4 * 0.5f + 0.5f, 1f);
				if (rayState.Age > 1f)
				{
					TrialRaysMesh.RemoveGroupAt(num3);
				}
			}
			Mesh trialFlareMesh = TrialFlareMesh;
			Vector3 position2 = (TrialRaysMesh.Position = AoInstance.Position);
			trialFlareMesh.Position = position2;
			Mesh trialFlareMesh2 = TrialFlareMesh;
			Quaternion rotation2 = (TrialRaysMesh.Rotation = CameraManager.Rotation);
			trialFlareMesh2.Rotation = rotation2;
			float num5 = Easing.EaseIn(TrialTimeAccumulator / 2f, EasingType.Quadratic);
			TrialRaysMesh.Scale = new Vector3(num5 + 1f);
			TrialFlareMesh.Material.Opacity = 0.125f + Easing.EaseIn(FezMath.Saturate((TrialTimeAccumulator - 2f) / 3f), EasingType.Cubic) * 0.875f;
			TrialFlareMesh.Scale = Vector3.One + TrialRaysMesh.Scale * Easing.EaseIn(Math.Max(TrialTimeAccumulator - 2.5f, 0f) / 1.5f, EasingType.Cubic) * 4f;
		}
		else
		{
			MakeRay();
			for (int num6 = TrialRaysMesh.Groups.Count - 1; num6 >= 0; num6--)
			{
				Group group3 = TrialRaysMesh.Groups[num6];
				DotHost.RayState rayState2 = group3.CustomData as DotHost.RayState;
				rayState2.Age += elapsedSeconds * 0.15f;
				group3.Material.Diffuse = Vector3.One * FezMath.Saturate(rayState2.Age * 8f);
				group3.Scale *= new Vector3(2f, 1f, 1f);
			}
			TrialRaysMesh.AlwaysOnTop = false;
			TrialRaysMesh.Position = AoInstance.Position;
			TrialRaysMesh.Rotation = CameraManager.Rotation;
			float num7 = Easing.EaseIn(TrialTimeAccumulator / 2f, EasingType.Quadratic);
			TrialRaysMesh.Scale = new Vector3(num7 + 1f);
		}
	}

	private void MakeRay()
	{
		if (TrialRaysMesh.Groups.Count < 150 && RandomHelper.Probability(0.25))
		{
			float num = (RandomHelper.Probability(0.75) ? 0.1f : 0.5f);
			Group group = TrialRaysMesh.AddGroup();
			group.Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(new FezVertexPositionColor[6]
			{
				new FezVertexPositionColor(new Vector3(0f, num / 2f * 0.5f, 0f), Color.White),
				new FezVertexPositionColor(new Vector3(1f, num / 2f, 0f), Color.White),
				new FezVertexPositionColor(new Vector3(1f, num / 2f * 0.5f, 0f), Color.White),
				new FezVertexPositionColor(new Vector3(1f, (0f - num) / 2f * 0.5f, 0f), Color.White),
				new FezVertexPositionColor(new Vector3(1f, (0f - num) / 2f, 0f), Color.White),
				new FezVertexPositionColor(new Vector3(0f, (0f - num) / 2f * 0.5f, 0f), Color.White)
			}, new int[12]
			{
				0, 1, 2, 0, 2, 5, 5, 2, 3, 5,
				3, 4
			}, PrimitiveType.TriangleList);
			group.CustomData = new DotHost.RayState();
			group.Material = new Material
			{
				Diffuse = new Vector3(0f)
			};
			group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, RandomHelper.Between(0.0, -3.1415927410125732)) * Quaternion.CreateFromAxisAngle(Vector3.Forward, RandomHelper.Between(0.0, 6.2831854820251465));
		}
	}

	private void ScheduleFades()
	{
		ServiceHelper.AddComponent(new ScreenFade(base.Game)
		{
			FromColor = ColorEx.TransparentWhite,
			ToColor = Color.White,
			Duration = 0.5f,
			EasingType = EasingType.Quintic,
			Faded = delegate
			{
				ServiceHelper.AddComponent(new ScreenFade(base.Game)
				{
					FromColor = Color.White,
					ToColor = ColorEx.TransparentWhite,
					Duration = 4f,
					EaseOut = true
				});
			}
		});
	}

	private void AddSpin()
	{
		if (CameraManager.Viewpoint == Viewpoint.Perspective || CameraManager.ProjectionTransition)
		{
			return;
		}
		DestinationSpinSpeed = Math.Abs(DestinationSpinSpeed);
		SpinSpeed = Math.Abs(SpinSpeed);
		if (SpinSpeed < 3f)
		{
			SpinSpeed += 3f;
			DestinationSpinSpeed += 4f;
		}
		else
		{
			DestinationSpinSpeed *= 3f;
		}
		currentDroneIndex++;
		if (currentDroneIndex <= 4)
		{
			eHexDrone.FadeOutAndDie(0.5f);
			eHexDrone = sHexDrones[currentDroneIndex].Emit(loop: true, 0f, 0f);
			Waiters.Interpolate(0.5, delegate(float s)
			{
				if (eHexDrone != null && !eHexDrone.Dead)
				{
					eHexDrone.VolumeFactor = s;
				}
			});
		}
		else
		{
			currentDroneIndex = 4;
		}
		DestinationSpinSpeed *= CameraManager.LastViewpoint.GetDistance(CameraManager.Viewpoint);
		SpinSpeed *= CameraManager.LastViewpoint.GetDistance(CameraManager.Viewpoint);
	}

	private void AddSplodeBeam(bool fullForce)
	{
		AddSplodeBeamInternal(fullForce);
		if (fullForce)
		{
			AddSplodeBeamInternal(fullForce: true);
		}
	}

	private void AddSplodeBeamInternal(bool fullForce)
	{
		float num = RandomHelper.Between(0.0, 3.1415927410125732);
		float num2 = RandomHelper.Between(0.0, 3.0 / 32.0);
		Group group = RaysMesh.AddColoredTriangle(Vector3.Zero, new Vector3(0f, (float)Math.Sin(num - num2) * 55f, (float)Math.Cos(num - num2) * 55f), new Vector3(0f, (float)Math.Sin(num + num2) * 55f, (float)Math.Cos(num + num2) * 55f), new Color(241, 23, 101), new Color(37, 22, 53), new Color(37, 22, 53));
		group.Material = new Material
		{
			Opacity = 1f
		};
		group.CustomData = new RayData
		{
			Sign = RandomHelper.Sign(),
			Speed = RandomHelper.Between(0.5, 1.5) * (float)(fullForce ? 6 : 3)
		};
	}

	private void ScheduleText()
	{
		IWaiter waiter = Waiters.Wait(3.0, delegate
		{
			GameService.ShowScroll((!GameState.SaveData.IsNewGamePlus) ? "ROTATE_INSTRUCTIONS" : (GameState.SaveData.HasStereo3D ? "STEREO_INSTRUCTIONS" : "FPVIEW_INSTRUCTIONS"), 0f, onTop: true, onVolume: false);
			PlayerManager.Action = ActionType.Idle;
		});
		waiter.CustomPause = () => GameState.InCutscene;
		waiter.AutoPause = true;
	}

	private void ScheduleExplode()
	{
		Waiters.Wait(() => PlayerManager.Grounded, delegate
		{
			WalkTo.Destination = () => PlayerManager.Position * Vector3.UnitY + Origin * FezMath.XZMask;
			WalkTo.NextAction = ActionType.LookingUp;
			PlayerManager.Action = ActionType.WalkingTo;
			CurrentPhase = Phase.HexaExplode;
			SincePhaseStarted = 0f;
			if (GameState.IsTrialMode)
			{
				Waiters.Wait(1.0, delegate
				{
					sTrialWhiteOut.Emit();
				});
			}
			Waiters.Interpolate(0.5, delegate(float s)
			{
				eHexDrone.Pitch = FezMath.Saturate(s);
			}, delegate
			{
				eHexDrone.FadeOutAndDie(0.1f);
			});
		}).AutoPause = true;
	}

	private void Talk1()
	{
		Waiters.Interpolate(0.5, delegate(float s)
		{
			eAmbientHex.VolumeFactor = FezMath.Saturate(0.25f * (1f - s) + 0.25f) * 0.85f;
		});
		Say(0, 4, delegate
		{
			Waiters.Interpolate(0.5, delegate(float s)
			{
				eAmbientHex.VolumeFactor = FezMath.Saturate(0.25f * s + 0.25f) * 0.85f;
			});
			sNightTransition.Emit();
			SincePhaseStarted = 0f;
			CurrentPhase = Phase.Beam;
			eHexaTalk.FadeOutAndDie(0.1f);
			eHexaTalk = null;
			PlayerManager.CanControl = false;
		});
	}

	private void Talk2()
	{
		Waiters.Interpolate(0.5, delegate(float s)
		{
			eAmbientHex.VolumeFactor = FezMath.Saturate(0.25f * (1f - s) + 0.25f) * 0.85f;
		});
		Say(5, 7, delegate
		{
			Waiters.Interpolate(0.5, delegate(float s)
			{
				eAmbientHex.VolumeFactor = FezMath.Saturate(0.25f * (1f - s)) * 0.85f;
			});
			sHexDisappear.Emit();
			SincePhaseStarted = 0f;
			CurrentPhase = Phase.Disappear;
			eHexaTalk.FadeOutAndDie(0.1f);
			eHexaTalk = null;
			PlayerManager.CanControl = false;
		});
	}

	private void Say(int current, int stopAt, Action onEnded)
	{
		if (eHexaTalk == null || eHexaTalk.Dead)
		{
			return;
		}
		IWaiter w = Waiters.Wait(0.25, eHexaTalk.Cue.Resume);
		w.AutoPause = true;
		string stringRaw = GameText.GetStringRaw(HexStrings[current]);
		IWaiter w2 = Waiters.Wait(0.25f + 0.1f * (float)stringRaw.Length, eHexaTalk.Cue.Pause);
		w2.AutoPause = true;
		LongRunningAction longRunningAction = ArtObjectService.Say(AoInstance.Id, stringRaw, zuish: true);
		longRunningAction.Ended = (Action)Delegate.Combine(longRunningAction.Ended, (Action)delegate
		{
			if (w.Alive)
			{
				w.Cancel();
			}
			if (w2.Alive)
			{
				if (eHexaTalk != null && !eHexaTalk.Dead && eHexaTalk.Cue.State != SoundState.Paused)
				{
					eHexaTalk.Cue.Pause();
				}
				w2.Cancel();
			}
			if (current == stopAt)
			{
				onEnded();
			}
			else
			{
				Say(current + 1, stopAt, onEnded);
			}
		});
	}

	private void Kill()
	{
		ServiceHelper.RemoveComponent(this);
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading)
		{
			return;
		}
		if (CurrentPhase > Phase.Talk1)
		{
			GameState.SkyOpacity = 1f - Starfield.Opacity;
			BackgroundPlane[] statuePlanes = StatuePlanes;
			for (int i = 0; i < statuePlanes.Length; i++)
			{
				statuePlanes[i].Opacity = Easing.EaseIn(1f - Starfield.Opacity, EasingType.Quadratic);
			}
			SfRenderer.Visible = true;
		}
		switch (CurrentPhase)
		{
		case Phase.Beam:
			DrawBeamWithMask();
			break;
		case Phase.MatrixSpin:
			DrawBeamWithMask();
			base.GraphicsDevice.SetBlendingMode(BlendingMode.Additive);
			TargetRenderer.DrawFullscreen(new Color(WhiteOutFactor * 0.6f, WhiteOutFactor * 0.6f, WhiteOutFactor * 0.6f));
			base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			break;
		case Phase.Talk2:
			DrawBeamWithMask();
			break;
		case Phase.Disappear:
			DrawBeamWithMask();
			break;
		case Phase.FezBeamGrow:
		case Phase.FezComeDown:
			base.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
			BeamMesh.Draw();
			if (GameState.SaveData.IsNewGamePlus)
			{
				DealGlassesPlane.Draw();
			}
			base.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
			break;
		case Phase.Beamsplode:
			if (GameState.SaveData.IsNewGamePlus)
			{
				DealGlassesPlane.Draw();
			}
			base.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
			BeamMesh.Draw();
			base.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
			if (SincePhaseStarted > 0.25f)
			{
				RaysMesh.Draw();
			}
			break;
		case Phase.Yay:
			if (GameState.SaveData.IsNewGamePlus)
			{
				DealGlassesPlane.Draw();
			}
			break;
		case Phase.WaitSpin:
			TrialRaysMesh.Draw();
			break;
		case Phase.HexaExplode:
			SolidCubes.Draw();
			SmallCubes.Draw();
			if (GameState.IsTrialMode)
			{
				float alpha = FezMath.Saturate(Easing.EaseIn((TrialTimeAccumulator - 6f) / 1f, EasingType.Quintic));
				TargetRenderer.DrawFullscreen(new Color(1f, 1f, 1f, alpha));
				TrialFlareMesh.Draw();
			}
			TrialRaysMesh.Draw();
			break;
		case Phase.ThatsIt:
			if (GameState.IsTrialMode)
			{
				TargetRenderer.DrawFullscreen(Color.White);
			}
			break;
		}
	}

	private void DrawBeamWithMask()
	{
		base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.None);
		base.GraphicsDevice.PrepareStencilWrite(StencilMask.LightShaft);
		BeamMask.Draw();
		base.GraphicsDevice.SetColorWriteChannels(ColorWriteChannels.All);
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.NotEqual, StencilMask.LightShaft);
		base.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
		BeamMesh.Draw();
		base.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
		MatrixMesh.Draw();
		base.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
	}
}
