using System;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure.Input;

public struct MouseDragState : IEquatable<MouseDragState>
{
	private readonly Point start;

	private readonly Point movement;

	private readonly bool preDrag;

	public Point Start => start;

	public Point Movement => movement;

	internal bool PreDrag => preDrag;

	internal MouseDragState(Point start, Point current)
		: this(start, current, preDrag: false)
	{
	}

	internal MouseDragState(Point start, Point current, bool preDrag)
	{
		this.start = start;
		this.preDrag = preDrag;
		movement = new Point(current.X - start.X, current.Y - start.Y);
	}

	public bool Equals(MouseDragState other)
	{
		if (other.start.Equals(start) && other.movement.Equals(movement))
		{
			return other.preDrag.Equals(preDrag);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (obj.GetType() != typeof(MouseDragState))
		{
			return false;
		}
		return Equals((MouseDragState)obj);
	}

	public override int GetHashCode()
	{
		return (((start.GetHashCode() * 397) ^ movement.GetHashCode()) * 397) ^ preDrag.GetHashCode();
	}

	public static bool operator ==(MouseDragState left, MouseDragState right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(MouseDragState left, MouseDragState right)
	{
		return !left.Equals(right);
	}
}
