using System;
using FezEngine.Services;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

public class CamShake : GameComponent
{
	private TimeSpan SinceStarted;

	private float RemainingDistance;

	public static CamShake CurrentCamShake { get; private set; }

	public float Distance { private get; set; }

	public TimeSpan Duration { private get; set; }

	public bool IsDisposed { get; private set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	public CamShake(Game game)
		: base(game)
	{
		CurrentCamShake = this;
	}

	public void Reset()
	{
		SinceStarted = TimeSpan.Zero;
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		CurrentCamShake = null;
	}

	public override void Initialize()
	{
		base.Initialize();
		InputManager.ActiveGamepad.Vibrate(VibrationMotor.RightHigh, 1.0, Duration, EasingType.Linear);
		InputManager.ActiveGamepad.Vibrate(VibrationMotor.LeftLow, 1.0, Duration, EasingType.Linear);
	}

	public override void Update(GameTime gameTime)
	{
		if (!EngineState.Loading && !EngineState.Paused && !EngineState.InMap)
		{
			SinceStarted += gameTime.ElapsedGameTime;
			if (SinceStarted > Duration)
			{
				ServiceHelper.RemoveComponent(this);
				IsDisposed = true;
			}
			double d = SinceStarted.TotalSeconds / Duration.TotalSeconds;
			d = Math.Sqrt(d);
			RemainingDistance = MathHelper.Lerp(Distance, 0f, (float)d);
			CameraManager.InterpolatedCenter += new Vector3(RandomHelper.Centered(RemainingDistance * 2f), RandomHelper.Centered(RemainingDistance * 2f), RandomHelper.Centered(RemainingDistance * 2f));
		}
	}
}
