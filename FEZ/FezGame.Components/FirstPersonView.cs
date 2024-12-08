using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class FirstPersonView : DrawableGameComponent
{
	private float GomezOpacityOrigin;

	private float GomezOpacityDest;

	private Vector3 CenterOrigin;

	private Vector3 CenterDest;

	private FishEyeEffect fishEyeEffect;

	private RenderTargetHandle fishEyeRT;

	[ServiceDependency]
	public ITargetRenderingManager TRM { private get; set; }

	[ServiceDependency]
	public IDotManager DotManager { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	public FirstPersonView(Game game)
		: base(game)
	{
		base.UpdateOrder = -10;
		base.DrawOrder = 199;
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		DrawActionScheduler.Schedule(delegate
		{
			fishEyeEffect = new FishEyeEffect();
		});
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.Loading || GameState.InMap || GameState.InMenuCube || Fez.PublicDemo || GameState.FarawaySettings.InTransition || !GameState.SaveData.HasFPView)
		{
			return;
		}
		if (!PlayerManager.Grounded)
		{
			UnDotize();
			return;
		}
		if ((PlayerManager.Action.IsIdle() || PlayerManager.Action == ActionType.Sliding) && !GameState.InCutscene && !CameraManager.ProjectionTransition && CameraManager.ViewTransitionReached && !LevelManager.Flat && PlayerManager.CanControl && EndCutscene32Host.Instance == null && EndCutscene64Host.Instance == null)
		{
			if (InputManager.FpsToggle == FezButtonState.Pressed)
			{
				if (GameState.InFpsMode)
				{
					CameraManager.Direction = -CameraManager.LastViewpoint.ForwardVector();
					CameraManager.ChangeViewpoint(CameraManager.LastViewpoint);
					CenterDest = CenterOrigin;
					CenterOrigin = PlayerManager.Center + Vector3.UnitY * 0.3f;
					GomezOpacityOrigin = 0f;
					GomezOpacityDest = 1f;
				}
				else
				{
					GameState.InFpsMode = true;
					fishEyeRT = TRM.TakeTarget();
					TRM.ScheduleHook(base.DrawOrder, fishEyeRT.Target);
					UnDotize();
					CameraManager.ChangeViewpoint(Viewpoint.Perspective);
					CenterOrigin = CameraManager.Center;
					CenterDest = PlayerManager.Center + Vector3.UnitY * 0.3f;
					GomezOpacityOrigin = 1f;
					GomezOpacityDest = 0f;
				}
			}
			if (CameraManager.Viewpoint == Viewpoint.Perspective && (InputManager.CancelTalk == FezButtonState.Pressed || InputManager.Back == FezButtonState.Pressed || InputManager.Start == FezButtonState.Pressed || InputManager.Jump == FezButtonState.Pressed))
			{
				InputManager.PressedToDown();
				CameraManager.Direction = -CameraManager.LastViewpoint.ForwardVector();
				CameraManager.ChangeViewpoint(CameraManager.LastViewpoint);
				CenterDest = CenterOrigin;
				CenterOrigin = PlayerManager.Center + Vector3.UnitY * 0.3f;
				GomezOpacityOrigin = 0f;
				GomezOpacityDest = 1f;
			}
		}
		if (GameState.InFpsMode && CameraManager.ProjectionTransition)
		{
			CameraManager.Center = Vector3.Lerp(CenterOrigin, CenterDest, CameraManager.ViewTransitionStep);
			PlayerManager.GomezOpacity = MathHelper.Lerp(GomezOpacityOrigin, GomezOpacityDest, CameraManager.ViewTransitionStep);
			fishEyeEffect.Intensity = CameraManager.ViewTransitionStep;
		}
		if (GameState.InFpsMode && CameraManager.Viewpoint == Viewpoint.Perspective && !CameraManager.ProjectionTransition)
		{
			CameraManager.Center = PlayerManager.Center + Vector3.UnitY * 0.3f;
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (fishEyeRT != null && TRM.IsHooked(fishEyeRT.Target))
		{
			TRM.Resolve(fishEyeRT.Target, CameraManager.Viewpoint == Viewpoint.Perspective);
			base.GraphicsDevice.Clear(ClearOptions.Target, Color.Black, 1f, 0);
			base.GraphicsDevice.SetupViewport();
			TRM.DrawFullscreen(fishEyeEffect, fishEyeRT.Target);
			if (CameraManager.Viewpoint != Viewpoint.Perspective)
			{
				TRM.ReturnTarget(fishEyeRT);
				fishEyeRT = null;
			}
		}
	}

	private void UnDotize()
	{
		if (DotManager.Owner == this)
		{
			DotManager.Burrow();
		}
	}
}
