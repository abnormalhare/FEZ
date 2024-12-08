using System;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

public class GomezHost : DrawableGameComponent
{
	private const float InvincibilityBlinkSpeed = 5f;

	private GomezEffect effect;

	public Vector3 InterpolatedPosition;

	public static GomezHost Instance;

	private TimeSpan sinceBackgroundChanged;

	private bool lastBackground;

	private bool lastHideFez;

	private AnimatedTexture lastAnimation;

	private readonly PlayerManager dummyPlayer = new PlayerManager();

	public Mesh PlayerMesh { get; private set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ILightingPostProcess LightingPostProcess { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { private get; set; }

	[ServiceDependency]
	public IPhysicsManager PhysicsManager { private get; set; }

	public GomezHost(Game game)
		: base(game)
	{
		PlayerMesh = new Mesh
		{
			SamplerState = SamplerState.PointClamp
		};
		base.UpdateOrder = 11;
		base.DrawOrder = 9;
		ServiceHelper.InjectServices(dummyPlayer);
		Instance = this;
	}

	public override void Initialize()
	{
		PlayerMesh.AddFace(new Vector3(1f), new Vector3(0f, 0.25f, 0f), FaceOrientation.Front, centeredOnOrigin: true, doublesided: true);
		PlayerManager.MeshHost = this;
		LevelManager.LevelChanged += delegate
		{
			effect.ColorSwapMode = ((LevelManager.WaterType == LiquidType.Sewer) ? ColorSwapMode.Gameboy : ((LevelManager.WaterType == LiquidType.Lava) ? ColorSwapMode.VirtualBoy : (LevelManager.BlinkingAlpha ? ColorSwapMode.Cmyk : ColorSwapMode.None)));
		};
		LightingPostProcess.DrawGeometryLights += PreDraw;
		TimeInterpolation.RegisterCallback(InterpolatePosition, 25);
		base.Initialize();
	}

	protected override void LoadContent()
	{
		DrawActionScheduler.Schedule(delegate
		{
			PlayerMesh.Effect = (effect = new GomezEffect());
		});
	}

	private Vector3 GetPositionOffset(IPlayerManager playerManager)
	{
		if (lastAnimation == null)
		{
			return Vector3.Zero;
		}
		float num = playerManager.Size.Y + (float)((playerManager.CarriedInstance != null || playerManager.Action == ActionType.ThrowingHeavy) ? (-2) : 0);
		Vector3 vector = (1f - num) / 2f * Vector3.UnitY;
		Vector2 vector2 = playerManager.Action.GetOffset() / 16f;
		vector2.Y -= lastAnimation.PotOffset.Y / 64f;
		Viewpoint view = ((CameraManager.Viewpoint.IsOrthographic() || !CameraManager.ActionRunning) ? CameraManager.Viewpoint : CameraManager.LastViewpoint);
		return vector + (vector2.X * view.RightVector() * playerManager.LookingDirection.Sign() + vector2.Y * Vector3.UnitY);
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.InMap || PlayerManager.Animation == null)
		{
			return;
		}
		if (lastAnimation != PlayerManager.Animation)
		{
			effect.Animation = PlayerManager.Animation.Texture;
			lastAnimation = PlayerManager.Animation;
		}
		int width = lastAnimation.Texture.Width;
		int height = lastAnimation.Texture.Height;
		int frame = lastAnimation.Timing.Frame;
		Rectangle rectangle = lastAnimation.Offsets[frame];
		PlayerMesh.FirstGroup.TextureMatrix.Set(new Matrix((float)rectangle.Width / (float)width, 0f, 0f, 0f, 0f, (float)rectangle.Height / (float)height, 0f, 0f, (float)rectangle.X / (float)width, (float)rectangle.Y / (float)height, 1f, 0f, 0f, 0f, 0f, 0f));
		if (lastBackground != PlayerManager.Background && !PlayerManager.Action.NoBackgroundDarkening())
		{
			sinceBackgroundChanged = TimeSpan.Zero;
			lastBackground = PlayerManager.Background;
			if (!LevelManager.LowPass && EndCutscene32Host.Instance == null && EndCutscene64Host.Instance == null)
			{
				SoundManager.FadeFrequencies(PlayerManager.Background);
			}
		}
		if (sinceBackgroundChanged.TotalSeconds < 1.0)
		{
			sinceBackgroundChanged += gameTime.ElapsedGameTime;
		}
		effect.Background = (PlayerManager.Action.NoBackgroundDarkening() ? 0f : FezMath.Saturate(PlayerManager.Background ? ((float)sinceBackgroundChanged.TotalSeconds * 2f) : (1f - (float)sinceBackgroundChanged.TotalSeconds * 2f)));
		PlayerMesh.Scale = new Vector3((float)PlayerManager.Animation.FrameWidth / 16f, (float)PlayerManager.Animation.FrameHeight / 16f * (float)Math.Sign(CollisionManager.GravityFactor), 1f);
		PlayerMesh.Position = PlayerManager.Position + GetPositionOffset(PlayerManager);
		InterpolatedPosition = PlayerManager.Position;
		bool flag = PlayerManager.HideFez && !GameState.SaveData.IsNewGamePlus && !PlayerManager.Animation.NoHat && !PlayerManager.Action.IsCarry();
		if (lastHideFez != flag)
		{
			lastHideFez = flag;
			effect.NoMoreFez = lastHideFez;
		}
	}

	public void InterpolatePosition(GameTime gameTime)
	{
		if (GameState.Loading || PlayerManager.Hidden || GameState.InMap || FezMath.AlmostEqual(PlayerManager.GomezOpacity, 0f))
		{
			InterpolatedPosition = PlayerManager.Position;
		}
		else if (TimeInterpolation.NeedsInterpolation && !DefaultCameraManager.NoInterpolation && CameraManager.ViewTransitionReached && CameraManager.Viewpoint.IsOrthographic())
		{
			PlayerManager.CopyTo(dummyPlayer);
			Vector3 vector = 3.15f * CollisionManager.GravityFactor * 0.15f * (float)gameTime.ElapsedGameTime.TotalSeconds * -Vector3.UnitY;
			dummyPlayer.Velocity += vector;
			PhysicsManager.Update(dummyPlayer);
			double num = (gameTime.TotalGameTime - TimeInterpolation.LastUpdate).TotalSeconds / TimeInterpolation.UpdateTimestep.TotalSeconds;
			Vector3 position = PlayerManager.Position;
			Vector3 positionOffset = GetPositionOffset(PlayerManager);
			Vector3 value = position + positionOffset;
			Vector3 position2 = dummyPlayer.Position;
			Vector3 positionOffset2 = GetPositionOffset(dummyPlayer);
			Vector3 value2 = position2 + positionOffset2;
			PlayerMesh.Position = Vector3.Lerp(value, value2, (float)num);
			InterpolatedPosition = Vector3.Lerp(position, position2, (float)num);
		}
	}

	private void PreDraw(GameTime gameTime)
	{
		if (!GameState.Loading && !PlayerManager.Hidden && !GameState.InFpsMode)
		{
			effect.Pass = LightingEffectPass.Pre;
			if (!PlayerManager.FullBright)
			{
				base.GraphicsDevice.PrepareStencilWrite(StencilMask.Level);
			}
			else
			{
				base.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
			}
			PlayerMesh.Draw();
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
			effect.Pass = LightingEffectPass.Main;
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (!GameState.StereoMode || GameState.FarawaySettings.InTransition)
		{
			DoDraw_Internal(gameTime);
		}
	}

	public void DoDraw_Internal(GameTime gameTime)
	{
		if (GameState.Loading || PlayerManager.Hidden || GameState.InMap || FezMath.AlmostEqual(PlayerManager.GomezOpacity, 0f))
		{
			return;
		}
		if (GameState.StereoMode || LevelManager.Quantum)
		{
			if (!CameraManager.Viewpoint.IsOrthographic() && CameraManager.LastViewpoint != 0)
			{
				PlayerMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, CameraManager.LastViewpoint.ToPhi());
			}
			else
			{
				PlayerMesh.Rotation = CameraManager.Rotation;
			}
			if (PlayerManager.LookingDirection == HorizontalDirection.Left)
			{
				PlayerMesh.Rotation *= FezMath.QuaternionFromPhi((float)Math.PI);
			}
		}
		if (PlayerManager.Action == ActionType.Suffering || PlayerManager.Action == ActionType.Sinking)
		{
			PlayerMesh.Material.Opacity = (float)FezMath.Saturate((Math.Sin(PlayerManager.BlinkSpeed * ((float)Math.PI * 2f) * 5f) + 0.5 - (double)(PlayerManager.BlinkSpeed * 1.25f)) * 2.0);
		}
		else
		{
			PlayerMesh.Material.Opacity = PlayerManager.GomezOpacity;
		}
		GraphicsDevice graphicsDevice = base.GraphicsDevice;
		if (!PlayerManager.Action.SkipSilhouette())
		{
			graphicsDevice.PrepareStencilRead(CompareFunction.Greater, StencilMask.NoSilhouette);
			PlayerMesh.DepthWrites = false;
			PlayerMesh.AlwaysOnTop = true;
			effect.Silhouette = true;
			PlayerMesh.Draw();
		}
		if (!PlayerManager.Background)
		{
			graphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.Hole);
			PlayerMesh.AlwaysOnTop = true;
			PlayerMesh.DepthWrites = false;
			effect.Silhouette = false;
			PlayerMesh.Draw();
		}
		graphicsDevice.PrepareStencilWrite(StencilMask.Gomez);
		PlayerMesh.AlwaysOnTop = PlayerManager.Action.NeedsAlwaysOnTop();
		PlayerMesh.DepthWrites = !GameState.InFpsMode;
		effect.Silhouette = false;
		PlayerMesh.Draw();
		graphicsDevice.PrepareStencilWrite(StencilMask.None);
	}
}
