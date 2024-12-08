using System;
using Common;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure.Input;

public struct ThumbstickState
{
	private const double PressThreshold = 0.5;

	public readonly Vector2 Position;

	public readonly Vector2 Movement;

	public readonly TimedButtonState Clicked;

	public readonly TimedButtonState Up;

	public readonly TimedButtonState Down;

	public readonly TimedButtonState Left;

	public readonly TimedButtonState Right;

	private ThumbstickState(Vector2 position, Vector2 movement, TimedButtonState clicked, TimedButtonState up, TimedButtonState down, TimedButtonState left, TimedButtonState right)
	{
		Position = position;
		Movement = movement;
		Clicked = clicked;
		Up = up;
		Down = down;
		Left = left;
		Right = right;
	}

	internal ThumbstickState NextState(Vector2 position, bool clicked, TimeSpan elapsed)
	{
		return new ThumbstickState(position, position - Position, Clicked.NextState(clicked, elapsed), Up.NextState((double)FezMath.Saturate(position.Y) > 0.5, elapsed), Down.NextState((double)FezMath.Saturate(0f - position.Y) > 0.5, elapsed), Left.NextState((double)FezMath.Saturate(0f - position.X) > 0.5, elapsed), Right.NextState((double)FezMath.Saturate(position.X) > 0.5, elapsed));
	}

	public override string ToString()
	{
		return Util.ReflectToString(this);
	}

	public static Vector2 CircleToSquare(Vector2 point)
	{
		double num = Math.Atan2(point.Y, point.X) + 3.1415927410125732;
		if (num <= 0.7853981852531433 || num > 5.4977874755859375)
		{
			return point * (float)(1.0 / Math.Cos(num));
		}
		if (num > 0.7853981852531433 && num <= 2.356194496154785)
		{
			return point * (float)(1.0 / Math.Sin(num));
		}
		if (num > 2.356194496154785 && num <= 3.9269909858703613)
		{
			return point * (float)(-1.0 / Math.Cos(num));
		}
		if (num > 3.9269909858703613 && num <= 5.4977874755859375)
		{
			return point * (float)(-1.0 / Math.Sin(num));
		}
		throw new InvalidOperationException("Invalid angle...?");
	}
}
