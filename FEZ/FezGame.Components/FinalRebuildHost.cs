using System;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class FinalRebuildHost : DrawableGameComponent
{
	private enum Phases
	{
		ZoomInNega,
		FlickerIn,
		SpinFill,
		MotorStart1,
		MotorStart2,
		MotorStart3,
		Crash,
		SmoothStart,
		ShineReboot
	}

	private const float ZoomDuration = 10f;

	private const float FlickerDuration = 1.25f;

	private const float SpinFillDuration = 10f;

	private const float Start1Duration = 5f;

	private const float Start2Duration = 4f;

	private const float Start3Duration = 6f;

	private const float SmoothStartDuration = 10f;

	private readonly Vector3[] CubeOffsets;

	private RenderTargetHandle RtHandle;

	private InvertEffect InvertEffect;

	private Mesh SolidCubes;

	private Mesh WhiteCube;

	private Quaternion OriginalCubeRotation;

	private ArtObjectInstance HexahedronAo;

	private NesGlitches Glitches;

	private Mesh RaysMesh;

	private Mesh FlareMesh;

	private SoundEffect sHexAppear;

	private SoundEffect sCubeAppear;

	private SoundEffect sMotorSpin1;

	private SoundEffect sMotorSpin2;

	private SoundEffect sMotorSpinAOK;

	private SoundEffect sMotorSpinCrash;

	private SoundEffect sRayWhiteout;

	private SoundEffect sAku;

	private SoundEffect sAmbientDrone;

	private SoundEffect sZoomIn;

	private SoundEmitter eAku;

	private SoundEmitter eMotor;

	private SoundEmitter eAmbient;

	private Phases ActivePhase;

	private float PhaseTime;

	private bool FirstUpdate;

	private float lastStep;

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { get; set; }

	public FinalRebuildHost(Game game)
		: base(game)
	{
		CubeOffsets = new Vector3[64];
		for (int i = 0; i < 4; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				for (int k = 0; k < 4; k++)
				{
					CubeOffsets[i * 16 + j * 4 + k] = new Vector3((float)j - 1.5f, (float)i - 1.5f, (float)k - 1.5f);
				}
			}
		}
		base.DrawOrder = 750;
		base.Visible = (base.Enabled = false);
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		Destroy();
		bool visible = (base.Enabled = LevelManager.Name == "HEX_REBUILD");
		base.Visible = visible;
		if (base.Enabled)
		{
			DefaultCameraManager.NoInterpolation = true;
			GameState.HideHUD = true;
			CameraManager.ChangeViewpoint(Viewpoint.Right, 0f);
			PlayerManager.Background = false;
			PlayerManager.IgnoreFreefall = true;
			ArtObject artObject = CMProvider.CurrentLevel.Load<ArtObject>("Art Objects/NEW_HEXAO");
			int num = IdentifierPool.FirstAvailable(LevelManager.ArtObjects);
			HexahedronAo = new ArtObjectInstance(artObject)
			{
				Id = num
			};
			LevelManager.ArtObjects.Add(num, HexahedronAo);
			HexahedronAo.Initialize();
			HexahedronAo.Hidden = true;
			WhiteCube = new Mesh
			{
				Blending = BlendingMode.Additive,
				DepthWrites = false
			};
			WhiteCube.Rotation = CameraManager.Rotation * Quaternion.CreateFromRotationMatrix(Matrix.CreateLookAt(Vector3.One, Vector3.Zero, Vector3.Up));
			WhiteCube.AddColoredBox(new Vector3(4f), Vector3.Zero, Color.White, centeredOnOrigin: true);
			SolidCubes = new Mesh
			{
				Blending = BlendingMode.Opaque
			};
			Quaternion originalCubeRotation = (SolidCubes.Rotation = WhiteCube.Rotation);
			OriginalCubeRotation = originalCubeRotation;
			ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry = LevelManager.ActorTriles(ActorType.CubeShard).FirstOrDefault().Geometry;
			ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> geometry2 = LevelManager.ActorTriles(ActorType.SecretCube).FirstOrDefault().Geometry;
			sHexAppear = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/HexRebuild/HexAppear");
			sCubeAppear = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/HexRebuild/CubeAppear");
			sMotorSpin1 = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/HexRebuild/MotorStart1");
			sMotorSpin2 = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/HexRebuild/MotorStart2");
			sMotorSpinAOK = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/HexRebuild/MotorStartAOK");
			sMotorSpinCrash = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/HexRebuild/MotorStartCrash");
			sRayWhiteout = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/HexRebuild/RayWhiteout");
			sAku = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/HexRebuild/Aku");
			sZoomIn = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/HexRebuild/ZoomIn");
			sAmbientDrone = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/HexRebuild/AmbientDrone");
			for (int i = 0; i < Math.Min(GameState.SaveData.CubeShards + GameState.SaveData.SecretCubes, 64); i++)
			{
				Vector3 position = CubeOffsets[i];
				ShaderInstancedIndexedPrimitives<VertexPositionNormalTextureInstance, Vector4> shaderInstancedIndexedPrimitives = ((i < GameState.SaveData.CubeShards) ? geometry : geometry2);
				Group group = SolidCubes.AddGroup();
				group.Geometry = new IndexedUserPrimitives<VertexPositionNormalTextureInstance>(shaderInstancedIndexedPrimitives.Vertices.ToArray(), shaderInstancedIndexedPrimitives.Indices, shaderInstancedIndexedPrimitives.PrimitiveType);
				group.Position = position;
				group.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, (float)RandomHelper.Random.Next(0, 4) * ((float)Math.PI / 2f));
				group.Enabled = false;
				group.Material = new Material();
			}
			RaysMesh = new Mesh
			{
				Blending = BlendingMode.Additive,
				DepthWrites = false
			};
			FlareMesh = new Mesh
			{
				Blending = BlendingMode.Alphablending,
				SamplerState = SamplerState.AnisotropicClamp,
				DepthWrites = false,
				AlwaysOnTop = true
			};
			FlareMesh.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true);
			DrawActionScheduler.Schedule(delegate
			{
				WhiteCube.Effect = new DefaultEffect.VertexColored();
				SolidCubes.Effect = new DefaultEffect.LitTextured
				{
					Specular = true,
					Emissive = 0.5f,
					AlphaIsEmissive = true
				};
				SolidCubes.Texture = LevelMaterializer.TrilesMesh.Texture;
				InvertEffect = new InvertEffect();
				RaysMesh.Effect = new DefaultEffect.VertexColored();
				FlareMesh.Effect = new DefaultEffect.Textured();
				FlareMesh.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/flare_alpha");
				ServiceHelper.AddComponent(Glitches = new NesGlitches(base.Game));
				RtHandle = TargetRenderer.TakeTarget();
				TargetRenderer.ScheduleHook(base.DrawOrder, RtHandle.Target);
			});
		}
	}

	private void Destroy()
	{
		if (Glitches != null)
		{
			ServiceHelper.RemoveComponent(Glitches);
		}
		Glitches = null;
		if (RtHandle != null)
		{
			TargetRenderer.UnscheduleHook(RtHandle.Target);
			TargetRenderer.ReturnTarget(RtHandle);
		}
		RtHandle = null;
		if (SolidCubes != null)
		{
			SolidCubes.Dispose();
		}
		SolidCubes = null;
		if (WhiteCube != null)
		{
			DefaultCameraManager.NoInterpolation = false;
			PlayerManager.IgnoreFreefall = false;
			WhiteCube.Dispose();
			WhiteCube = null;
		}
		if (RaysMesh != null)
		{
			RaysMesh.Dispose();
		}
		if (FlareMesh != null)
		{
			FlareMesh.Dispose();
		}
		RaysMesh = (FlareMesh = null);
		if (InvertEffect != null)
		{
			InvertEffect.Dispose();
		}
		InvertEffect = null;
		HexahedronAo = null;
		FirstUpdate = true;
		sAmbientDrone = (sAku = (sZoomIn = (sHexAppear = (sCubeAppear = (sMotorSpin1 = (sMotorSpin2 = (sMotorSpinAOK = (sMotorSpinCrash = (sRayWhiteout = null)))))))));
		eAku = (eAmbient = (eMotor = null));
		ActivePhase = Phases.ZoomInNega;
		PhaseTime = 0f;
		GameState.SkipRendering = false;
		GameState.HideHUD = false;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			return;
		}
		if (FirstUpdate)
		{
			gameTime = new GameTime();
			FirstUpdate = false;
		}
		PhaseTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
		switch (ActivePhase)
		{
		case Phases.ZoomInNega:
		{
			GameState.SkipRendering = true;
			if (gameTime.ElapsedGameTime.Ticks == 0L)
			{
				Glitches.ActiveGlitches = 0;
				Glitches.FreezeProbability = 0f;
				CameraManager.PixelsPerTrixel = 0.5f;
				CameraManager.SnapInterpolation();
				PlayerManager.Position = Vector3.Zero;
				PlayerManager.LookingDirection = HorizontalDirection.Right;
				SetHexVisible(visible: false);
				CollisionManager.GravityFactor = 1f;
				sZoomIn.Emit();
				eAmbient = sAmbientDrone.Emit(loop: true, 0f, 0f);
			}
			float amount = Easing.EaseIn(FezMath.Saturate(PhaseTime / 10f), EasingType.Sine);
			if (PhaseTime > 0.25f)
			{
				CameraManager.Radius *= MathHelper.Lerp(0.99f, 1f, amount);
			}
			PlayerManager.Action = ((PhaseTime > 7f) ? ActionType.StandWinking : ActionType.Standing);
			PlayerManager.Velocity = Vector3.Zero;
			amount = Easing.EaseIn(FezMath.Saturate(PhaseTime / 11f), EasingType.Sine);
			CameraManager.Center = PlayerManager.Position + new Vector3(0f, 0.125f, 0f) + new Vector3(0f, (float)Math.Sin(PhaseTime) * 0.25f * (1f - amount), 0f);
			CameraManager.SnapInterpolation();
			GameState.SkipRendering = false;
			eAmbient.VolumeFactor = amount;
			if (PhaseTime > 11f)
			{
				Waiters.Wait(0.75, delegate
				{
					sHexAppear.Emit();
				}).AutoPause = true;
				ChangePhase();
			}
			break;
		}
		case Phases.FlickerIn:
			GameState.SkipRendering = true;
			if (gameTime.ElapsedGameTime.Ticks == 0L)
			{
				WhiteCube.Material.Diffuse = Vector3.Zero;
				WhiteCube.Rotation = OriginalCubeRotation;
				CameraManager.PixelsPerTrixel = 3f;
				CameraManager.SnapInterpolation();
				PlayerManager.Position = Vector3.Zero;
				if (eAmbient != null)
				{
					eAmbient.VolumeFactor = 0.625f;
				}
				PhaseTime = -1f;
			}
			PlayerManager.Action = ActionType.Standing;
			PlayerManager.Velocity = Vector3.Zero;
			CameraManager.Center = PlayerManager.Position + new Vector3(0f, 4.5f, 0f);
			CameraManager.SnapInterpolation();
			GameState.SkipRendering = false;
			WhiteCube.Position = PlayerManager.Position + new Vector3(0f, 6f, 0f);
			if (PhaseTime > 2.25f)
			{
				ChangePhase();
			}
			break;
		case Phases.SpinFill:
		{
			GameState.SkipRendering = true;
			if (gameTime.ElapsedGameTime.Ticks == 0L)
			{
				CameraManager.PixelsPerTrixel = 3f;
				CameraManager.SnapInterpolation();
				PlayerManager.Position = Vector3.Zero;
				WhiteCube.Material.Diffuse = Vector3.One;
				for (int i = 0; i < SolidCubes.Groups.Count; i++)
				{
					SolidCubes.Groups[i].CustomData = null;
				}
			}
			PlayerManager.Action = ActionType.Standing;
			PlayerManager.Velocity = Vector3.Zero;
			CameraManager.Center = PlayerManager.Position + new Vector3(0f, 4.5f, 0f);
			CameraManager.SnapInterpolation();
			GameState.SkipRendering = false;
			float num3 = Easing.EaseInOut(FezMath.Saturate(PhaseTime / 11f), EasingType.Sine);
			Mesh solidCubes2 = SolidCubes;
			Vector3 position = (WhiteCube.Position = PlayerManager.Position + new Vector3(0f, 6f, 0f));
			solidCubes2.Position = position;
			Mesh solidCubes3 = SolidCubes;
			Quaternion rotation = (WhiteCube.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num3 * ((float)Math.PI * 2f) * 3f) * OriginalCubeRotation);
			solidCubes3.Rotation = rotation;
			num3 = Easing.EaseInOut(FezMath.Saturate(PhaseTime / 10f), EasingType.Quadratic);
			float pitch = MathHelper.Clamp((num3 - lastStep) * 200f - 0.2f, -1f, 1f);
			float num4 = 1f / (float)SolidCubes.Groups.Count;
			for (int j = 0; j < SolidCubes.Groups.Count; j++)
			{
				float num5 = (float)j / (float)SolidCubes.Groups.Count;
				float num6 = Easing.EaseIn(FezMath.Saturate((num3 - num5) / num4), EasingType.Sine);
				if (num6 == 1f)
				{
					SolidCubes.Groups[j].Material.Diffuse = Vector3.One;
					SolidCubes.Groups[j].Enabled = true;
					continue;
				}
				if (num6 == 0f)
				{
					SolidCubes.Groups[j].Enabled = false;
					continue;
				}
				if ((double)num6 > 0.125 && SolidCubes.Groups[j].CustomData == null)
				{
					sCubeAppear.Emit(pitch);
					SolidCubes.Groups[j].CustomData = true;
				}
				SolidCubes.Groups[j].Material.Diffuse = new Vector3(RandomHelper.Probability(num6).AsNumeric(), RandomHelper.Probability(num6).AsNumeric(), RandomHelper.Probability(num6).AsNumeric());
				SolidCubes.Groups[j].Enabled = RandomHelper.Probability(num6);
			}
			lastStep = num3;
			if (PhaseTime > 12f)
			{
				eMotor = sMotorSpin1.Emit();
				eAku = sAku.Emit(loop: true, 0f, 0f);
				lastStep = 0f;
				ChangePhase();
			}
			break;
		}
		case Phases.MotorStart1:
		{
			GameState.SkipRendering = true;
			if (gameTime.ElapsedGameTime.Ticks == 0L)
			{
				HexahedronAo.Position = SolidCubes.Position;
				lastStep = 0f;
				for (int l = 0; l < SolidCubes.Groups.Count; l++)
				{
					SolidCubes.Groups[l].Enabled = true;
					SolidCubes.Groups[l].Material.Diffuse = Vector3.One;
				}
				SolidCubes.Rotation = Quaternion.Identity;
				SolidCubes.Position = Vector3.Zero;
				SolidCubes.CollapseToBufferWithNormal<VertexPositionNormalTextureInstance>();
				SolidCubes.Position = PlayerManager.Position + new Vector3(0f, 6f, 0f);
			}
			PlayerManager.Action = ActionType.Standing;
			PlayerManager.Velocity = Vector3.Zero;
			CameraManager.Center = PlayerManager.Position + new Vector3(0f, 4.5f, 0f);
			CameraManager.SnapInterpolation();
			GameState.SkipRendering = false;
			float num8 = Easing.EaseIn(Easing.EaseOut(FezMath.Saturate(PhaseTime / 5f), EasingType.Sine), EasingType.Sextic);
			SetHexVisible((double)(num8 - lastStep) > 0.00825);
			lastStep = num8;
			Mesh solidCubes5 = SolidCubes;
			Mesh whiteCube3 = WhiteCube;
			Quaternion quaternion2 = (HexahedronAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num8 * ((float)Math.PI * 2f) * 4f) * OriginalCubeRotation);
			Quaternion rotation = (whiteCube3.Rotation = quaternion2);
			solidCubes5.Rotation = rotation;
			if (PhaseTime > 5f)
			{
				eMotor = sMotorSpin2.Emit();
				ChangePhase();
			}
			break;
		}
		case Phases.MotorStart2:
		{
			GameState.SkipRendering = true;
			if (gameTime.ElapsedGameTime.Ticks == 0L)
			{
				HexahedronAo.Position = SolidCubes.Position;
				lastStep = 0f;
			}
			PlayerManager.Action = ActionType.Standing;
			PlayerManager.Velocity = Vector3.Zero;
			CameraManager.Center = PlayerManager.Position + new Vector3(0f, 4.5f, 0f);
			CameraManager.SnapInterpolation();
			GameState.SkipRendering = false;
			float num9 = Easing.EaseIn(Easing.EaseOut(FezMath.Saturate(PhaseTime / 4f), EasingType.Sine), EasingType.Sextic);
			float num10 = num9 - lastStep;
			SetHexVisible((double)num10 > 0.01);
			lastStep = num9;
			if (GameState.SaveData.SecretCubes + GameState.SaveData.CubeShards < 64)
			{
				Glitches.DisappearProbability = 0.05f;
				float num11 = Easing.EaseIn(num10 / 0.01f, EasingType.Quartic);
				Glitches.ActiveGlitches = FezMath.Round(num11 * 7f + (float)(int)RandomHelper.Between(0.0, num11 * 10f));
				Glitches.FreezeProbability = num10 / 0.01f * 0.0025f;
			}
			Mesh solidCubes6 = SolidCubes;
			Mesh whiteCube4 = WhiteCube;
			Quaternion quaternion2 = (HexahedronAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num9 * ((float)Math.PI * 2f) * 5f) * OriginalCubeRotation);
			Quaternion rotation = (whiteCube4.Rotation = quaternion2);
			solidCubes6.Rotation = rotation;
			if (PhaseTime > 4f + ((GameState.SaveData.SecretCubes + GameState.SaveData.CubeShards >= 64) ? 0.5f : 0f))
			{
				if (GameState.SaveData.SecretCubes + GameState.SaveData.CubeShards < 64)
				{
					eMotor = sMotorSpinCrash.Emit();
					ChangePhase();
				}
				else
				{
					eMotor = sMotorSpinAOK.Emit();
					ChangePhaseTo(Phases.SmoothStart);
				}
			}
			break;
		}
		case Phases.MotorStart3:
		{
			GameState.SkipRendering = true;
			if (gameTime.ElapsedGameTime.Ticks == 0L)
			{
				HexahedronAo.Position = SolidCubes.Position;
			}
			PlayerManager.Action = ActionType.Standing;
			PlayerManager.Velocity = Vector3.Zero;
			CameraManager.Center = PlayerManager.Position + new Vector3(0f, 4.5f, 0f);
			CameraManager.SnapInterpolation();
			GameState.SkipRendering = false;
			float num = Easing.EaseIn(PhaseTime / 6f, EasingType.Sextic);
			float num2 = Math.Min(num - lastStep, 0.05f);
			SetHexVisible((double)num2 > 0.0125);
			lastStep = num;
			Glitches.DisappearProbability = 0.0375f;
			float value = Easing.EaseIn(num2 / 0.0375f, EasingType.Quartic);
			Glitches.ActiveGlitches = FezMath.Round(FezMath.Saturate(value) * 500f + (float)(int)RandomHelper.Between(0.0, FezMath.Saturate(value) * 250f));
			Glitches.FreezeProbability = Easing.EaseIn(num2 / 0.05f, EasingType.Quadratic) * 0.15f;
			Mesh solidCubes = SolidCubes;
			Mesh whiteCube = WhiteCube;
			Quaternion quaternion2 = (HexahedronAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num * ((float)Math.PI * 2f) * 20f) * OriginalCubeRotation);
			Quaternion rotation = (whiteCube.Rotation = quaternion2);
			solidCubes.Rotation = rotation;
			if (PhaseTime > 8f)
			{
				ChangePhase();
			}
			break;
		}
		case Phases.Crash:
			Glitches.FreezeProbability = 1f;
			if (PhaseTime > 2f)
			{
				if (eAku != null)
				{
					eAku.FadeOutAndDie(0f);
				}
				if (eAmbient != null)
				{
					eAmbient.FadeOutAndDie(0f, autoPause: false);
				}
				Glitches.ActiveGlitches = 0;
				Glitches.FreezeProbability = 0f;
				Glitches.DisappearProbability = 0f;
				GlitchReboot();
			}
			break;
		case Phases.SmoothStart:
		{
			GameState.SkipRendering = true;
			if (gameTime.ElapsedGameTime.Ticks == 0L)
			{
				HexahedronAo.Position = SolidCubes.Position;
				lastStep = 0f;
				for (int k = 0; k < SolidCubes.Groups.Count; k++)
				{
					SolidCubes.Groups[k].Enabled = true;
				}
			}
			PlayerManager.Action = ActionType.Standing;
			PlayerManager.Velocity = Vector3.Zero;
			CameraManager.Center = PlayerManager.Position + new Vector3(0f, 4.5f, 0f);
			CameraManager.SnapInterpolation();
			GameState.SkipRendering = false;
			float num7 = Easing.EaseInOut(FezMath.Saturate(PhaseTime / 10f), EasingType.Quadratic);
			SetHexVisible(num7 > 0.425f);
			lastStep = num7;
			Mesh solidCubes4 = SolidCubes;
			Mesh whiteCube2 = WhiteCube;
			Quaternion quaternion2 = (HexahedronAo.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num7 * ((float)Math.PI * 2f) * 18f) * OriginalCubeRotation);
			Quaternion rotation = (whiteCube2.Rotation = quaternion2);
			solidCubes4.Rotation = rotation;
			if (PhaseTime > 10f)
			{
				eAku.FadeOutAndDie(2f);
				sRayWhiteout.Emit();
				ChangePhase();
			}
			break;
		}
		case Phases.ShineReboot:
			GameState.SkipRendering = true;
			if (gameTime.ElapsedGameTime.Ticks == 0L)
			{
				HexahedronAo.Position = SolidCubes.Position;
				SetHexVisible(visible: true);
				RaysMesh.ClearGroups();
				HexahedronAo.Rotation = OriginalCubeRotation;
				if (eAmbient != null)
				{
					eAmbient.FadeOutAndDie(0f, autoPause: false);
				}
			}
			PlayerManager.Action = ActionType.Standing;
			PlayerManager.Velocity = Vector3.Zero;
			CameraManager.Center = PlayerManager.Position + new Vector3(0f, 4.5f, 0f);
			CameraManager.SnapInterpolation();
			GameState.SkipRendering = false;
			UpdateRays((float)gameTime.ElapsedGameTime.TotalSeconds);
			if (PhaseTime > 4f)
			{
				SmoothReboot();
			}
			break;
		}
	}

	private void GlitchReboot()
	{
		ServiceHelper.AddComponent(new Reboot(base.Game, "GOMEZ_HOUSE_END_32"));
		Waiters.Wait(0.10000000149011612, delegate
		{
			Destroy();
			bool enabled = (base.Visible = false);
			base.Enabled = enabled;
		});
		base.Enabled = false;
	}

	private void SmoothReboot()
	{
		ServiceHelper.AddComponent(new Intro(base.Game)
		{
			Fake = true,
			FakeLevel = "GOMEZ_HOUSE_END_64",
			Glitch = false
		});
		Waiters.Wait(0.10000000149011612, delegate
		{
			Destroy();
			bool enabled = (base.Visible = false);
			base.Enabled = enabled;
		});
		base.Enabled = false;
	}

	private void SetHexVisible(bool visible)
	{
		if (eAku != null)
		{
			eAku.VolumeFactor = (visible ? 1 : 0);
		}
		if (eMotor != null)
		{
			eMotor.VolumeFactor = ((!visible) ? 1 : 0);
		}
		if (eAmbient != null)
		{
			eAmbient.VolumeFactor = (visible ? 0f : 0.625f);
		}
		HexahedronAo.Hidden = !visible;
		HexahedronAo.Visible = visible;
		HexahedronAo.ArtObject.Group.Enabled = visible;
	}

	private void ChangePhase()
	{
		PhaseTime = 0f;
		ActivePhase++;
		Update(new GameTime());
	}

	private void ChangePhaseTo(Phases phase)
	{
		PhaseTime = 0f;
		ActivePhase = phase;
		Update(new GameTime());
	}

	private void UpdateRays(float elapsedSeconds)
	{
		bool num = PhaseTime > 1.5f;
		MakeRay();
		if (num)
		{
			MakeRay();
		}
		for (int num2 = RaysMesh.Groups.Count - 1; num2 >= 0; num2--)
		{
			Group group = RaysMesh.Groups[num2];
			DotHost.RayState rayState = group.CustomData as DotHost.RayState;
			rayState.Age += elapsedSeconds * 0.15f;
			group.Material.Diffuse = Vector3.One * FezMath.Saturate(rayState.Age * 8f);
			group.Scale *= new Vector3(1.5f, 1f, 1f);
			if (rayState.Age > 1f)
			{
				RaysMesh.RemoveGroupAt(num2);
			}
		}
		RaysMesh.AlwaysOnTop = false;
		Mesh flareMesh = FlareMesh;
		Vector3 position2 = (RaysMesh.Position = HexahedronAo.Position);
		flareMesh.Position = position2;
		Mesh flareMesh2 = FlareMesh;
		Quaternion rotation2 = (RaysMesh.Rotation = CameraManager.Rotation);
		flareMesh2.Rotation = rotation2;
		FlareMesh.Material.Opacity = Easing.EaseIn(FezMath.Saturate(PhaseTime / 2.5f), EasingType.Cubic);
		FlareMesh.Scale = Vector3.One + RaysMesh.Scale * Easing.EaseIn((PhaseTime - 0.25f) / 1.75f, EasingType.Decic) * 4f;
	}

	private void MakeRay()
	{
		if (RaysMesh.Groups.Count < 150 && RandomHelper.Probability(0.1 + (double)Easing.EaseIn(FezMath.Saturate(PhaseTime / 1.75f), EasingType.Sine) * 0.9))
		{
			float num = (RandomHelper.Probability(0.75) ? 0.1f : 0.4f);
			Group group = RaysMesh.AddGroup();
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

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			if (RtHandle != null && TargetRenderer.IsHooked(RtHandle.Target))
			{
				TargetRenderer.Resolve(RtHandle.Target, reschedule: true);
				TargetRenderer.DrawFullscreen(Color.Black);
			}
			return;
		}
		switch (ActivePhase)
		{
		case Phases.ZoomInNega:
		{
			float num = Easing.EaseOut(FezMath.Saturate(PhaseTime / 10f), EasingType.Quadratic);
			TargetRenderer.DrawFullscreen(new Color(0f, 0f, 0f, 1f - num));
			TargetRenderer.Resolve(RtHandle.Target, reschedule: true);
			TargetRenderer.DrawFullscreen(InvertEffect, RtHandle.Target);
			break;
		}
		case Phases.FlickerIn:
		{
			float num2 = Easing.EaseIn(FezMath.Saturate(PhaseTime / 1.25f), EasingType.Quadratic);
			if (RandomHelper.Probability(0.5))
			{
				WhiteCube.Material.Diffuse = new Vector3(RandomHelper.Probability(num2).AsNumeric(), RandomHelper.Probability(num2).AsNumeric(), RandomHelper.Probability(num2).AsNumeric());
			}
			WhiteCube.Enabled = true;
			WhiteCube.Draw();
			TargetRenderer.Resolve(RtHandle.Target, reschedule: true);
			TargetRenderer.DrawFullscreen(InvertEffect, RtHandle.Target);
			break;
		}
		case Phases.SpinFill:
			WhiteCube.Draw();
			SolidCubes.Draw();
			TargetRenderer.Resolve(RtHandle.Target, reschedule: true);
			TargetRenderer.DrawFullscreen(InvertEffect, RtHandle.Target);
			break;
		case Phases.MotorStart1:
		case Phases.MotorStart2:
		case Phases.MotorStart3:
		case Phases.SmoothStart:
			if (!HexahedronAo.Visible)
			{
				WhiteCube.Draw();
				SolidCubes.Draw();
				if (TargetRenderer.IsHooked(RtHandle.Target))
				{
					TargetRenderer.Resolve(RtHandle.Target, reschedule: true);
					TargetRenderer.DrawFullscreen(InvertEffect, RtHandle.Target);
				}
				else
				{
					TargetRenderer.ScheduleHook(base.DrawOrder, RtHandle.Target);
				}
			}
			else if (TargetRenderer.IsHooked(RtHandle.Target))
			{
				TargetRenderer.Resolve(RtHandle.Target, reschedule: false);
				TargetRenderer.DrawFullscreen(RtHandle.Target);
			}
			break;
		case Phases.ShineReboot:
			RaysMesh.Draw();
			FlareMesh.Draw();
			break;
		case Phases.Crash:
			break;
		}
	}
}
