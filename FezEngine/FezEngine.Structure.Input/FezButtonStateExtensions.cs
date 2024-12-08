namespace FezEngine.Structure.Input;

public static class FezButtonStateExtensions
{
	public static bool IsDown(this FezButtonState state)
	{
		if (state != FezButtonState.Pressed)
		{
			return state == FezButtonState.Down;
		}
		return true;
	}

	public static FezButtonState NextState(this FezButtonState state, bool pressed)
	{
		switch (state)
		{
		case FezButtonState.Up:
			if (!pressed)
			{
				return FezButtonState.Up;
			}
			return FezButtonState.Pressed;
		case FezButtonState.Pressed:
			if (!pressed)
			{
				return FezButtonState.Released;
			}
			return FezButtonState.Down;
		case FezButtonState.Released:
			if (!pressed)
			{
				return FezButtonState.Up;
			}
			return FezButtonState.Pressed;
		default:
			if (!pressed)
			{
				return FezButtonState.Released;
			}
			return FezButtonState.Down;
		}
	}
}
