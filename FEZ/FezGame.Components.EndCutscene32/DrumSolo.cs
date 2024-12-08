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
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.EndCutscene32;

internal class DrumSolo : DrawableGameComponent
{
	private enum State
	{
		ZoomOut,
		DrumSolo,
		JumpUp,
		TetraSplode,
		FezSplash
	}

	private const float ZoomOutDuration = 6f;

	private const float DrumSoloDuration = 15f;

	private const float JumpUpDuration = 4.5f;

	private const float JumpDistance = 8f;

	private float DestinationRadius;

	private readonly EndCutscene32Host Host;

	private float Time;

	private State ActiveState;

	private float StarRotationTimeKeeper;

	private Mesh StarMesh;

	private Mesh Flare;

	private FezLogo FezLogo;

	private SoundEffect sTitleBassHit;

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public DrumSolo(Game game, EndCutscene32Host host)
		: base(game)
	{
		Host = host;
		base.DrawOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		float num = (float)base.GraphicsDevice.Viewport.Width / (1280f * base.GraphicsDevice.GetViewScale());
		DestinationRadius = 20f * base.GraphicsDevice.GetViewScale() * num;
		Flare = new Mesh
		{
			Blending = BlendingMode.Additive,
			SamplerState = SamplerState.LinearClamp,
			DepthWrites = false
		};
		Flare.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, centeredOnOrigin: true, doublesided: true);
		StarMesh = new Mesh
		{
			Blending = BlendingMode.Alphablending,
			DepthWrites = false
		};
		Color color = new Color(1f, 1f, 0.3f, 0f);
		for (int i = 0; i < 8; i++)
		{
			float num2 = (float)i * ((float)Math.PI * 2f) / 8f;
			float num3 = ((float)i + 0.5f) * ((float)Math.PI * 2f) / 8f;
			StarMesh.AddColoredTriangle(Vector3.Zero, new Vector3((float)Math.Sin(num2), (float)Math.Cos(num2), 0f), new Vector3((float)Math.Sin(num3), (float)Math.Cos(num3), 0f), new Color(1f, 1f, 0.5f, 0.7f), color, color);
		}
		DrawActionScheduler.Schedule(delegate
		{
			Flare.Effect = new DefaultEffect.Textured();
			StarMesh.Effect = new DefaultEffect.VertexColored();
			Flare.Texture = CMProvider.Global.Load<Texture2D>("Background Planes/flare");
		});
		ServiceHelper.AddComponent(FezLogo = new FezLogo(base.Game));
		FezLogo.Enabled = false;
		sTitleBassHit = CMProvider.Global.Load<SoundEffect>("Sounds/Intro/LogoZoom");
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		if (StarMesh != null)
		{
			StarMesh.Dispose();
		}
		if (Flare != null)
		{
			Flare.Dispose();
		}
		Flare = (StarMesh = null);
		DefaultCameraManager.NoInterpolation = false;
		Waiters.Wait(2.0, delegate
		{
			CMProvider.Dispose(CM.EndCutscene);
		});
	}

	private void Reset()
	{
		DefaultCameraManager.NoInterpolation = true;
		GameState.SkipRendering = false;
		GameState.InCutscene = false;
		GameState.SkyOpacity = 1f;
		LevelManager.ChangeLevel("DRUM");
		CameraManager.ChangeViewpoint(Viewpoint.Back, 0f);
		CameraManager.Center = new Vector3(15f, 13f, 18f);
		CameraManager.SnapInterpolation();
		PlayerManager.Action = ActionType.DrumsIdle;
		LevelManager.ActualAmbient = Color.White;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			return;
		}
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		Time += num;
		switch (ActiveState)
		{
		case State.ZoomOut:
		{
			if (Time == 0f)
			{
				Reset();
			}
			float num2 = FezMath.Saturate(Time / 6f);
			PlayerManager.Position = new Vector3(15.09375f, 12.3125f, 18.33866f);
			CameraManager.Center = new Vector3(15.25f, 12.65625f, 18.33866f);
			CameraManager.Radius = MathHelper.Lerp(0f, DestinationRadius, Easing.EaseIn(Easing.EaseOut(num2, EasingType.Sine), EasingType.Cubic));
			CameraManager.SnapInterpolation();
			if (Time > 6f)
			{
				ChangeState();
				PlayerManager.Action = (ActionType)(106 + RandomHelper.Random.Next(1, 7));
				SoundManager.PlayNewSong("gomez_drums", 0f);
			}
			break;
		}
		case State.DrumSolo:
		{
			float num6 = FezMath.Saturate(Time / 15f);
			PlayerManager.Position = new Vector3(15.09375f, 12.3125f, 18.33866f);
			CameraManager.Center = new Vector3(15.25f, 12.65625f, 18.33866f);
			PlayerManager.LeaveGroundPosition = PlayerManager.Position;
			CameraManager.Radius = DestinationRadius;
			CameraManager.SnapInterpolation();
			float num7 = Easing.EaseOut(FezMath.Saturate(num6 * 25f), EasingType.Cubic);
			StarMesh.Material.Opacity = num7;
			Flare.Material.Diffuse = new Vector3(num7 / 3f);
			Mesh flare3 = Flare;
			Vector3 position = (StarMesh.Position = PlayerManager.Position + Vector3.Normalize(-CameraManager.Direction) * 10f);
			flare3.Position = position;
			Vector3 vector5 = MathHelper.Lerp(CameraManager.Radius * 0.8f, CameraManager.Radius * 0.6f, (float)Math.Sin(Time * 5f) / 2f + 0.5f) * Vector3.One;
			Mesh flare4 = Flare;
			position = (StarMesh.Scale = vector5 * num7);
			flare4.Scale = position;
			StarRotationTimeKeeper += num * (float)Math.Sin(Time) * 0.75f + 1f;
			StarMesh.Rotation = CameraManager.Rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, StarRotationTimeKeeper * (0.025f + 0.025f * num6));
			if (PlayerManager.Animation.Timing.Ended)
			{
				PlayerManager.Action = (ActionType)(106 + RandomHelper.Random.Next(1, 7));
			}
			PlayerManager.Animation.Timing.Update(TimeSpan.FromSeconds(num), 1f + num6 * 3f);
			if (Time > 15f)
			{
				ChangeState();
			}
			break;
		}
		case State.JumpUp:
		{
			float num3 = FezMath.Saturate(Time / 4.5f);
			float num4 = 1f - Easing.EaseIn(FezMath.Saturate(num3 * 25f), EasingType.Cubic);
			StarMesh.Material.Opacity = num4;
			Flare.Material.Diffuse = new Vector3(num4 / 3f);
			if (num4 > 0f)
			{
				Mesh flare = Flare;
				Vector3 position = (StarMesh.Position = PlayerManager.Position + Vector3.Normalize(-CameraManager.Direction) * 10f);
				flare.Position = position;
				Vector3 vector2 = MathHelper.Lerp(CameraManager.Radius * 0.6f, CameraManager.Radius * 0.4f, (float)Math.Sin(Time * 5f) / 2f + 0.5f) * Vector3.One;
				Mesh flare2 = Flare;
				position = (StarMesh.Scale = vector2 * num4);
				flare2.Scale = position;
				StarRotationTimeKeeper += num * (float)Math.Sin(Time) * 0.75f + 1f;
				StarMesh.Rotation = CameraManager.Rotation * Quaternion.CreateFromAxisAngle(Vector3.UnitZ, StarRotationTimeKeeper * 0.05f);
			}
			PlayerManager.Action = ActionType.VictoryForever;
			float num5 = Easing.EaseOut(num3, EasingType.Quadratic);
			PlayerManager.Position = new Vector3(15.09375f, 12.3125f + num5 * 8f, 18.33866f);
			CameraManager.Center = new Vector3(15.25f, 12.65625f + num5 * 8f, 18.33866f);
			CameraManager.Radius = DestinationRadius;
			CameraManager.SnapInterpolation();
			if (Time > 4.5f)
			{
				ChangeState();
			}
			break;
		}
		case State.TetraSplode:
			ChangeState();
			FezLogo.Visible = true;
			FezLogo.Enabled = true;
			FezLogo.TransitionStarted = true;
			FezLogo.SinceStarted = 1f;
			FezLogo.Opacity = 1f;
			FezLogo.HalfSpeed = true;
			FezLogo.Update(new GameTime());
			sTitleBassHit.Emit().Persistent = true;
			break;
		case State.FezSplash:
			PlayerManager.Position = new Vector3(15.09375f, 20.3125f, 18.33866f);
			CameraManager.Center = new Vector3(15.25f, 20.65625f, 18.33866f);
			CameraManager.Radius = DestinationRadius;
			CameraManager.SnapInterpolation();
			if (FezLogo.IsFullscreen)
			{
				Host.Cycle();
				PauseMenu.Starfield = FezLogo.Starfield;
				ServiceHelper.RemoveComponent(FezLogo);
				GameState.Pause(toCredits: true);
			}
			break;
		}
	}

	private void ChangeState()
	{
		Time = 0f;
		ActiveState++;
		Update(new GameTime());
	}

	public override void Draw(GameTime gameTime)
	{
		if (!GameState.Loading)
		{
			switch (ActiveState)
			{
			case State.ZoomOut:
			{
				float num = FezMath.Saturate(Time / 6f);
				Vector3 vector = EndCutscene32Host.PurpleBlack.ToVector3();
				TargetRenderer.DrawFullscreen(new Color(vector.X, vector.Y, vector.Z, 1f - FezMath.Saturate(num * 10f)));
				break;
			}
			case State.DrumSolo:
			case State.JumpUp:
				StarMesh.Draw();
				Flare.Draw();
				break;
			case State.FezSplash:
				base.GraphicsDevice.Clear(Color.White);
				break;
			case State.TetraSplode:
				break;
			}
		}
	}
}
