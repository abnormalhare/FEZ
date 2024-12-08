using System;

namespace FezEngine.Structure.Input;

public struct MouseButtonState : IEquatable<MouseButtonState>
{
	private readonly MouseDragState dragState;

	private readonly MouseButtonStates state;

	public MouseButtonStates State => state;

	public MouseDragState DragState => dragState;

	internal MouseButtonState(MouseButtonStates state)
		: this(state, default(MouseDragState))
	{
	}

	internal MouseButtonState(MouseButtonStates state, MouseDragState dragState)
	{
		this.dragState = dragState;
		this.state = state;
	}

	public bool Equals(MouseButtonState other)
	{
		if (object.Equals(other.state, state))
		{
			return other.dragState.Equals(dragState);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj.GetType() != typeof(MouseButtonState))
		{
			return false;
		}
		return Equals((MouseButtonState)obj);
	}

	public override int GetHashCode()
	{
		return (state.GetHashCode() * 397) ^ dragState.GetHashCode();
	}

	public static bool operator ==(MouseButtonState left, MouseButtonState right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(MouseButtonState left, MouseButtonState right)
	{
		return !left.Equals(right);
	}
}
