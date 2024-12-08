using System;
using Common;

namespace FezEngine.Structure.Input;

public struct TimedAnalogButtonState
{
	private const double TriggerThreshold = 0.5;

	public readonly float Value;

	public readonly FezButtonState State;

	public readonly TimeSpan TimePressed;

	private TimedAnalogButtonState(float value, FezButtonState state, TimeSpan timePressed)
	{
		Value = value;
		State = state;
		TimePressed = timePressed;
	}

	internal TimedAnalogButtonState NextState(float value, TimeSpan elapsed)
	{
		bool flag = (double)value > 0.5;
		return new TimedAnalogButtonState(value, State.NextState(flag), flag ? (TimePressed + elapsed) : TimeSpan.Zero);
	}

	public override string ToString()
	{
		return Util.ReflectToString(this);
	}
}
