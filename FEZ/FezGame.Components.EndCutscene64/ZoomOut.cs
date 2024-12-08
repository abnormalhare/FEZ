using FezEngine.Services;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FezGame.Components.EndCutscene64;

internal class ZoomOut : DrawableGameComponent
{
	private enum State
	{
		Wait,
		Zooming
	}

	private readonly EndCutscene64Host Host;

	private float StepTime;

	private State ActiveState;

	private float OldSfxVol;

	private Vector3 OriginalCenter;

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	public ZoomOut(Game game, EndCutscene64Host host)
		: base(game)
	{
		Host = host;
		base.DrawOrder = 1000;
		base.UpdateOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.ActualAmbient = new Color(0.25f, 0.25f, 0.25f);
		LevelManager.ActualDiffuse = Color.White;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			return;
		}
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		StepTime += num;
		if (ActiveState == State.Wait)
		{
			if (StepTime > 5f)
			{
				OldSfxVol = SoundManager.SoundEffectVolume;
				CameraManager.Constrained = true;
				OriginalCenter = CameraManager.Center;
				ChangeState();
			}
		}
		else if (ActiveState == State.Zooming)
		{
			CameraManager.Radius *= MathHelper.Lerp(1f, 1.05f, Easing.EaseIn(FezMath.Saturate(StepTime / 35f), EasingType.Quadratic));
			CameraManager.Center = Vector3.Lerp(OriginalCenter, LevelManager.Size / 2f, Easing.EaseInOut(FezMath.Saturate(StepTime / 15f), EasingType.Sine));
			SoundManager.SoundEffectVolume = 1f - FezMath.Saturate(StepTime / 33f) * 0.9f;
			if (StepTime > 33f)
			{
				ChangeState();
			}
		}
		if (num != 0f && Keyboard.GetState().IsKeyDown(Keys.R))
		{
			ActiveState = State.Zooming;
			ChangeState();
		}
	}

	private void ChangeState()
	{
		if (ActiveState == State.Zooming)
		{
			SoundManager.KillSounds();
			SoundManager.SoundEffectVolume = OldSfxVol;
			Host.Cycle();
		}
		else
		{
			StepTime = 0f;
			ActiveState++;
			Update(new GameTime());
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (ActiveState != 0 && !GameState.Loading && StepTime > 25f)
		{
			TargetRenderer.DrawFullscreen(new Color(23f / 85f, 83f / 85f, 1f, Easing.EaseInOut(FezMath.Saturate((StepTime - 25f) / 7f), EasingType.Sine)));
		}
	}
}
