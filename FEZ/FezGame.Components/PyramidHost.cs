using System;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Geometry;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FezGame.Components;

internal class PyramidHost : DrawableGameComponent
{
	private static readonly Vector2 DoorCenter = new Vector2(25f, 168f);

	private ArtObjectInstance MotherCubeAo;

	private Vector3 OriginalPosition;

	private float TimeAccumulator;

	private bool DoCapture;

	private Vector3 OriginalCenter;

	private SoundEffect sRotationDrone;

	private SoundEffect sWhiteOut;

	private SoundEmitter eRotationDrone;

	private Mesh RaysMesh;

	private Mesh FlareMesh;

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardManager { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	public PyramidHost(Game game)
		: base(game)
	{
		base.DrawOrder = 500;
		base.Enabled = (base.Visible = false);
	}

	public override void Initialize()
	{
		base.Initialize();
		KeyboardManager.RegisterKey(Keys.R);
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		bool visible = (base.Enabled = LevelManager.Name == "PYRAMID");
		base.Visible = visible;
		Clear();
		if (base.Enabled)
		{
			MotherCubeAo = LevelManager.ArtObjects[217];
			OriginalPosition = MotherCubeAo.Position;
			RaysMesh = new Mesh
			{
				Blending = BlendingMode.Additive,
				SamplerState = SamplerState.AnisotropicClamp,
				DepthWrites = false,
				AlwaysOnTop = true
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
				RaysMesh.Effect = new DefaultEffect.Textured();
				RaysMesh.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/smooth_ray");
				FlareMesh.Effect = new DefaultEffect.Textured();
				FlareMesh.Texture = CMProvider.Global.Load<Texture2D>("Other Textures/flare_alpha");
			});
			sRotationDrone = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Ending/Pyramid/MothercubeRotateDrone");
			sWhiteOut = CMProvider.Global.Load<SoundEffect>("Sounds/Ending/Pyramid/WhiteOut");
			eRotationDrone = sRotationDrone.EmitAt(OriginalPosition, loop: true);
		}
	}

	private void Clear()
	{
		MotherCubeAo = null;
		DoCapture = false;
		TimeAccumulator = 0f;
		if (RaysMesh != null)
		{
			RaysMesh.Dispose();
		}
		if (FlareMesh != null)
		{
			FlareMesh.Dispose();
		}
		FlareMesh = (RaysMesh = null);
		sRotationDrone = (sWhiteOut = null);
		if (eRotationDrone != null && !eRotationDrone.Dead)
		{
			eRotationDrone.FadeOutAndDie(0f);
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMap || !CameraManager.ActionRunning || !CameraManager.Viewpoint.IsOrthographic() || GameState.Loading)
		{
			return;
		}
		if (DoCapture)
		{
			TimeAccumulator += (float)gameTime.ElapsedGameTime.TotalSeconds;
			MotherCubeAo.Position = Vector3.Lerp(MotherCubeAo.Position, OriginalPosition, 0.025f);
			Quaternion quaternion = Quaternion.CreateFromYawPitchRoll(CameraManager.Viewpoint.VisibleOrientation().ToPhi(), 0f, 0f);
			MotherCubeAo.Rotation = Quaternion.Slerp(MotherCubeAo.Rotation, quaternion, 0.025f);
			Vector3 value = PlayerManager.Position * CameraManager.Viewpoint.DepthMask() + DoorCenter.X * CameraManager.Viewpoint.SideMask() + DoorCenter.Y * Vector3.UnitY - Vector3.UnitY * 0.125f;
			PlayerManager.Position = Vector3.Lerp(PlayerManager.Position, value, 0.025f);
			GameState.SkipRendering = true;
			CameraManager.Center = Vector3.Lerp(OriginalCenter, PlayerManager.Position, 0.025f);
			GameState.SkipRendering = false;
			UpdateRays((float)gameTime.ElapsedGameTime.TotalSeconds);
			if (TimeAccumulator > 6f)
			{
				GameState.SkipLoadScreen = true;
				LevelManager.ChangeLevel("HEX_REBUILD");
				Waiters.Wait(() => !GameState.Loading, delegate
				{
					GameState.SkipLoadScreen = false;
					Clear();
					base.Visible = false;
				});
				base.Enabled = false;
			}
		}
		else
		{
			TimeAccumulator += (float)gameTime.ElapsedGameTime.TotalSeconds / 2f;
			TimeAccumulator = FezMath.WrapAngle(TimeAccumulator);
			MotherCubeAo.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, (0f - (float)gameTime.ElapsedGameTime.TotalSeconds) * 0.375f);
			Vector3 vector = new Vector3(0f, (float)Math.Sin(TimeAccumulator), 0f) / 2f;
			MotherCubeAo.Position = OriginalPosition + vector;
			Vector2 vector2 = new Vector2(PlayerManager.Center.Dot(CameraManager.Viewpoint.SideMask()), PlayerManager.Center.Y);
			if (Math.Abs(vector2.X - DoorCenter.X) < 1f && Math.Abs(vector2.Y - (DoorCenter.Y + vector.Y)) < 1f && FezMath.AngleBetween(Vector3.Transform(-Vector3.UnitZ, MotherCubeAo.Rotation), CameraManager.Viewpoint.ForwardVector()) < 0.25f)
			{
				DoCapture = true;
				TimeAccumulator = 0f;
				PlayerManager.CanControl = false;
				PlayerManager.Action = ActionType.Floating;
				PlayerManager.Velocity = Vector3.Zero;
				OriginalCenter = CameraManager.Center;
				eRotationDrone.FadeOutAndDie(1.5f);
				SoundManager.PlayNewSong(5f);
				sWhiteOut.Emit().Persistent = true;
			}
		}
	}

	private void UpdateRays(float elapsedSeconds)
	{
		if (RaysMesh.Groups.Count < 50 && RandomHelper.Probability(0.25))
		{
			float num = 6f + RandomHelper.Centered(4.0);
			float num2 = RandomHelper.Between(0.5, num / 2.5f);
			Group group = RaysMesh.AddGroup();
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
		for (int num3 = RaysMesh.Groups.Count - 1; num3 >= 0; num3--)
		{
			Group group2 = RaysMesh.Groups[num3];
			DotHost.RayState rayState = group2.CustomData as DotHost.RayState;
			rayState.Age += elapsedSeconds * 0.15f;
			float num4 = (float)Math.Sin(rayState.Age * ((float)Math.PI * 2f) - (float)Math.PI / 2f) * 0.5f + 0.5f;
			num4 = Easing.EaseOut(num4, EasingType.Quintic);
			num4 = Easing.EaseOut(num4, EasingType.Quintic);
			group2.Material.Diffuse = Vector3.Lerp(Vector3.One, rayState.Tint.ToVector3(), 0.05f) * 0.15f * num4;
			float speed = rayState.Speed;
			group2.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.Forward, elapsedSeconds * speed * (0.1f + Easing.EaseIn(TimeAccumulator / 3f, EasingType.Quadratic) * 0.2f));
			group2.Scale = new Vector3(num4 * 0.75f + 0.25f, num4 * 0.5f + 0.5f, 1f);
			if (rayState.Age > 1f)
			{
				RaysMesh.RemoveGroupAt(num3);
			}
		}
		Mesh flareMesh = FlareMesh;
		Vector3 position = (RaysMesh.Position = PlayerManager.Center);
		flareMesh.Position = position;
		Mesh flareMesh2 = FlareMesh;
		Quaternion rotation2 = (RaysMesh.Rotation = CameraManager.Rotation);
		flareMesh2.Rotation = rotation2;
		float num5 = Easing.EaseIn(TimeAccumulator / 2f, EasingType.Quadratic);
		RaysMesh.Scale = new Vector3(num5 + 1f);
		FlareMesh.Material.Opacity = 0.125f + Easing.EaseIn(FezMath.Saturate((TimeAccumulator - 2f) / 3f), EasingType.Cubic) * 0.875f;
		FlareMesh.Scale = Vector3.One + RaysMesh.Scale * Easing.EaseIn(Math.Max(TimeAccumulator - 2.5f, 0f) / 1.5f, EasingType.Cubic) * 4f;
		if (KeyboardManager.GetKeyState(Keys.R) == FezButtonState.Pressed)
		{
			TimeAccumulator = 0f;
			RaysMesh.ClearGroups();
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (!GameState.Paused && !GameState.InMap && !GameState.Loading)
		{
			base.Draw(gameTime);
			RaysMesh.Draw();
			FlareMesh.Draw();
			if (DoCapture)
			{
				float alpha = FezMath.Saturate(Easing.EaseIn((TimeAccumulator - 6f) / 1f, EasingType.Quintic));
				TargetRenderer.DrawFullscreen(new Color(1f, 1f, 1f, alpha));
			}
		}
	}
}
