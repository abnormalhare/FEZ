using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.EndCutscene64;

internal class WhiteNoise : DrawableGameComponent
{
	private enum State
	{
		Wait,
		TVOff,
		ToCredits
	}

	private Texture2D NoiseTexture;

	private readonly EndCutscene64Host Host;

	private float StepTime;

	private State ActiveState;

	private VignetteEffect VignetteEffect;

	private ScanlineEffect ScanlineEffect;

	private SoundEffect sTvOff;

	private SoundEmitter eNoise;

	private Matrix NoiseOffset;

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public ITargetRenderingManager TargetRenderer { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency(Optional = true)]
	public IKeyboardStateManager KeyboardState { private get; set; }

	public WhiteNoise(Game game, EndCutscene64Host host)
		: base(game)
	{
		Host = host;
		base.DrawOrder = 1000;
	}

	public override void Initialize()
	{
		base.Initialize();
		DrawActionScheduler.Schedule(delegate
		{
			VignetteEffect = new VignetteEffect();
			ScanlineEffect = new ScanlineEffect();
			NoiseTexture = CMProvider.Get(CM.EndCutscene).Load<Texture2D>("Other Textures/noise");
		});
		sTvOff = CMProvider.Get(CM.EndCutscene).Load<SoundEffect>("Sounds/Ending/Cutscene64/TVOff");
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		Waiters.Wait(2.0, delegate
		{
			CMProvider.Dispose(CM.EndCutscene);
		});
	}

	private void Reset()
	{
		eNoise = Host.eNoise;
		StepTime = 0f;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			return;
		}
		float num = (float)gameTime.ElapsedGameTime.TotalSeconds;
		if (num == 0f)
		{
			Reset();
		}
		StepTime += num;
		switch (ActiveState)
		{
		case State.Wait:
			VignetteEffect.SinceStarted = 0f;
			if (StepTime > 3f)
			{
				sTvOff.Emit();
				if (eNoise != null)
				{
					eNoise.FadeOutAndDie(0.15f);
				}
				ChangeState();
			}
			break;
		case State.TVOff:
			VignetteEffect.SinceStarted = StepTime;
			if (StepTime > 6.5f)
			{
				ChangeState();
				GameState.SkyOpacity = 1f;
			}
			break;
		case State.ToCredits:
			eNoise = null;
			Host.Cycle();
			Waiters.Interpolate(1.0, delegate(float s)
			{
				PauseMenu.Starfield.Opacity = s;
			});
			PauseMenu.Starfield.Opacity = 0f;
			GameState.Pause(toCredits: true);
			break;
		}
	}

	private void ChangeState()
	{
		StepTime = 0f;
		ActiveState++;
		Update(new GameTime());
	}

	public override void Draw(GameTime gameTime)
	{
		if (GameState.Loading)
		{
			return;
		}
		switch (ActiveState)
		{
		case State.Wait:
		case State.TVOff:
		{
			base.GraphicsDevice.Clear(Color.Black);
			int width = base.GraphicsDevice.Viewport.Width;
			int height = base.GraphicsDevice.Viewport.Height;
			NoiseOffset = new Matrix
			{
				M11 = (float)width / 1024f,
				M22 = (float)height / 512f,
				M33 = 1f,
				M44 = 1f,
				M31 = RandomHelper.Unit(),
				M32 = RandomHelper.Unit()
			};
			base.GraphicsDevice.SamplerStates[0] = SamplerState.PointWrap;
			TargetRenderer.DrawFullscreen(ScanlineEffect, NoiseTexture, NoiseOffset, Color.White);
			if (VignetteEffect.SinceStarted > 0f)
			{
				TargetRenderer.DrawFullscreen(new Color(1f, 1f, 1f, Easing.EaseOut(FezMath.Saturate(VignetteEffect.SinceStarted * 2f), EasingType.Quadratic)));
			}
			base.GraphicsDevice.SetBlendingMode(BlendingMode.Multiply);
			TargetRenderer.DrawFullscreen(VignetteEffect, new Color(1f, 1f, 1f, (ActiveState == State.Wait) ? 0.425f : 1f));
			base.GraphicsDevice.SetBlendingMode(BlendingMode.Alphablending);
			if (VignetteEffect.SinceStarted > 1f)
			{
				TargetRenderer.DrawFullscreen(new Color(0f, 0f, 0f, FezMath.Saturate(VignetteEffect.SinceStarted - 1f)));
			}
			break;
		}
		case State.ToCredits:
			base.GraphicsDevice.Clear(Color.Black);
			break;
		}
	}
}
