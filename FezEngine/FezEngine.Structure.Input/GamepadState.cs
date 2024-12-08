using System;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FezEngine.Structure.Input;

public class GamepadState
{
	public enum GamepadLayout
	{
		Xbox360,
		PlayStation3,
		PlayStation4
	}

	private struct VibrationMotorState
	{
		public readonly float MaximumAmount;

		public readonly TimeSpan Duration;

		public readonly EasingType EasingType;

		public bool Active;

		public TimeSpan ElapsedTime;

		private float currentAmount;

		public float LastAmount { get; private set; }

		public float CurrentAmount
		{
			get
			{
				return currentAmount;
			}
			set
			{
				LastAmount = currentAmount;
				currentAmount = value;
			}
		}

		public VibrationMotorState(double maximumAmount, TimeSpan duration, EasingType easingType)
		{
			this = default(VibrationMotorState);
			Active = true;
			float lastAmount = (CurrentAmount = 0f);
			LastAmount = lastAmount;
			ElapsedTime = TimeSpan.Zero;
			MaximumAmount = (float)FezMath.Saturate(maximumAmount);
			Duration = duration;
			EasingType = easingType;
		}
	}

	public static EventHandler OnLayoutChanged;

	private static GamepadLayout? INTERNAL_forcedLayout = null;

	private static GamepadLayout INTERNAL_layout = GamepadLayout.Xbox360;

	private static readonly TimeSpan ConnectedCheckFrequency = TimeSpan.FromSeconds(1.0);

	private VibrationMotorState leftMotor;

	private VibrationMotorState rightMotor;

	private TimeSpan sinceCheckedConnected = ConnectedCheckFrequency;

	public readonly PlayerIndex PlayerIndex;

	public static GamepadLayout? ForcedLayout
	{
		get
		{
			return INTERNAL_forcedLayout;
		}
		set
		{
			INTERNAL_forcedLayout = value;
			UpdateLayout();
		}
	}

	public static GamepadLayout Layout
	{
		get
		{
			return INTERNAL_layout;
		}
		private set
		{
			if (value != INTERNAL_layout)
			{
				INTERNAL_layout = value;
				if (OnLayoutChanged != null)
				{
					OnLayoutChanged(null, null);
				}
			}
		}
	}

	public DirectionalState DPad { get; private set; }

	public ThumbstickState LeftStick { get; private set; }

	public ThumbstickState RightStick { get; private set; }

	public FezButtonState ExactUp { get; private set; }

	public TimedButtonState A { get; private set; }

	public TimedButtonState B { get; private set; }

	public TimedButtonState X { get; private set; }

	public TimedButtonState Y { get; private set; }

	public TimedButtonState RightShoulder { get; private set; }

	public TimedButtonState LeftShoulder { get; private set; }

	public TimedAnalogButtonState RightTrigger { get; private set; }

	public TimedAnalogButtonState LeftTrigger { get; private set; }

	public FezButtonState Start { get; private set; }

	public FezButtonState Back { get; private set; }

	public static bool AnyConnected { get; set; }

	public bool Connected { get; set; }

	public bool NewlyDisconnected { get; set; }

	private static void UpdateLayout()
	{
		if (ForcedLayout.HasValue)
		{
			Layout = ForcedLayout.Value;
			return;
		}
		string gUIDEXT = GamePad.GetGUIDEXT(PlayerIndex.One);
		if (gUIDEXT.Equals("4c05c405"))
		{
			Layout = GamepadLayout.PlayStation4;
		}
		else if (gUIDEXT.Equals("4c056802") || gUIDEXT.Equals("88880803") || gUIDEXT.Equals("25090500"))
		{
			Layout = GamepadLayout.PlayStation3;
		}
		else if (!string.IsNullOrEmpty(gUIDEXT))
		{
			Layout = GamepadLayout.Xbox360;
		}
	}

	public GamepadState(PlayerIndex playerIndex)
	{
		PlayerIndex = playerIndex;
	}

	public void Update(TimeSpan elapsed)
	{
		sinceCheckedConnected += elapsed;
		if (sinceCheckedConnected >= ConnectedCheckFrequency)
		{
			bool connected = Connected;
			Connected = GamePad.GetState(PlayerIndex).IsConnected;
			if (connected && !Connected)
			{
				NewlyDisconnected = true;
			}
			else if (Connected && !connected)
			{
				AnyConnected = true;
				UpdateLayout();
			}
		}
		if (!Connected)
		{
			return;
		}
		AnyConnected = true;
		GamePadState state;
		try
		{
			state = GamePad.GetState(PlayerIndex, GamePadDeadZone.IndependentAxes);
		}
		catch
		{
			return;
		}
		Connected = state.IsConnected;
		if (!Connected)
		{
			return;
		}
		if (SettingsManager.Settings.Vibration)
		{
			if (leftMotor.Active)
			{
				leftMotor = UpdateMotor(leftMotor, elapsed);
			}
			if (rightMotor.Active)
			{
				rightMotor = UpdateMotor(rightMotor, elapsed);
			}
			if (leftMotor.LastAmount != leftMotor.CurrentAmount || rightMotor.LastAmount != rightMotor.CurrentAmount)
			{
				GamePad.SetVibration(PlayerIndex, leftMotor.CurrentAmount, rightMotor.CurrentAmount);
			}
		}
		UpdateFromState(state, elapsed);
		if (sinceCheckedConnected >= ConnectedCheckFrequency)
		{
			sinceCheckedConnected = TimeSpan.Zero;
		}
	}

	private void UpdateFromState(GamePadState gamepadState, TimeSpan elapsed)
	{
		LeftShoulder = LeftShoulder.NextState(gamepadState.Buttons.LeftShoulder == ButtonState.Pressed, elapsed);
		RightShoulder = RightShoulder.NextState(gamepadState.Buttons.RightShoulder == ButtonState.Pressed, elapsed);
		LeftTrigger = LeftTrigger.NextState(gamepadState.Triggers.Left, elapsed);
		RightTrigger = RightTrigger.NextState(gamepadState.Triggers.Right, elapsed);
		Start = Start.NextState(gamepadState.Buttons.Start == ButtonState.Pressed);
		Back = Back.NextState(gamepadState.Buttons.Back == ButtonState.Pressed);
		A = A.NextState(gamepadState.Buttons.A == ButtonState.Pressed, elapsed);
		B = B.NextState(gamepadState.Buttons.B == ButtonState.Pressed, elapsed);
		X = X.NextState(gamepadState.Buttons.X == ButtonState.Pressed, elapsed);
		Y = Y.NextState(gamepadState.Buttons.Y == ButtonState.Pressed, elapsed);
		DPad = DPad.NextState(gamepadState.DPad.Up == ButtonState.Pressed, gamepadState.DPad.Down == ButtonState.Pressed, gamepadState.DPad.Left == ButtonState.Pressed, gamepadState.DPad.Right == ButtonState.Pressed, elapsed);
		LeftStick = LeftStick.NextState(gamepadState.ThumbSticks.Left, gamepadState.Buttons.LeftStick == ButtonState.Pressed, elapsed);
		RightStick = RightStick.NextState(gamepadState.ThumbSticks.Right, gamepadState.Buttons.RightStick == ButtonState.Pressed, elapsed);
		ExactUp = ExactUp.NextState(LeftStick.Position.Y > 0.9f || (DPad.Up.State.IsDown() && DPad.Left.State == FezButtonState.Up && DPad.Right.State == FezButtonState.Up));
	}

	private static VibrationMotorState UpdateMotor(VibrationMotorState motorState, TimeSpan elapsedTime)
	{
		if (motorState.ElapsedTime <= motorState.Duration)
		{
			float num = Easing.EaseIn(1.0 - motorState.ElapsedTime.TotalSeconds / motorState.Duration.TotalSeconds, motorState.EasingType);
			motorState.CurrentAmount = num * motorState.MaximumAmount;
		}
		else
		{
			motorState.CurrentAmount = 0f;
			motorState.Active = false;
		}
		motorState.ElapsedTime += elapsedTime;
		return motorState;
	}

	public void Vibrate(VibrationMotor motor, double amount, TimeSpan duration)
	{
		Vibrate(motor, amount, duration, EasingType.Linear);
	}

	public void Vibrate(VibrationMotor motor, double amount, TimeSpan duration, EasingType easingType)
	{
		VibrationMotorState vibrationMotorState = new VibrationMotorState(amount, duration, easingType);
		switch (motor)
		{
		case VibrationMotor.LeftLow:
			leftMotor = vibrationMotorState;
			break;
		case VibrationMotor.RightHigh:
			rightMotor = vibrationMotorState;
			break;
		}
	}
}
