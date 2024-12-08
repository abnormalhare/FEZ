using System;
using Common;

namespace FezEngine.Structure.Input;

public struct TimedButtonState : IEquatable<TimedButtonState>
{
	public readonly FezButtonState State;

	public readonly TimeSpan TimePressed;

	private TimedButtonState(FezButtonState state, TimeSpan timePressed)
	{
		State = state;
		TimePressed = timePressed;
	}

	internal TimedButtonState NextState(bool down, TimeSpan elapsed)
	{
		return new TimedButtonState(State.NextState(down), down ? (TimePressed + elapsed) : TimeSpan.Zero);
	}

	public bool Equals(TimedButtonState other)
	{
		if (other.State == State)
		{
			return other.TimePressed == TimePressed;
		}
		return false;
	}

	public override string ToString()
	{
		return Util.ReflectToString(this);
	}
}
