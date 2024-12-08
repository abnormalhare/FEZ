using System;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools;

public static class Easing
{
	private static class Sine
	{
		public static float EaseIn(double s)
		{
			return (float)Math.Sin(s * 1.5707963705062866 - 1.5707963705062866) + 1f;
		}

		public static float EaseOut(double s)
		{
			return (float)Math.Sin(s * 1.5707963705062866);
		}

		public static float EaseInOut(double s)
		{
			return (float)(Math.Sin(s * 3.1415927410125732 - 1.5707963705062866) + 1.0) / 2f;
		}
	}

	private static class Power
	{
		public static float EaseIn(double s, int power)
		{
			return (float)Math.Pow(s, power);
		}

		public static float EaseOut(double s, int power)
		{
			int num = ((power % 2 != 0) ? 1 : (-1));
			return (float)((double)num * (Math.Pow(s - 1.0, power) + (double)num));
		}

		public static float EaseInOut(double s, int power)
		{
			s *= 2.0;
			if (s < 1.0)
			{
				return EaseIn(s, power) / 2f;
			}
			int num = ((power % 2 != 0) ? 1 : (-1));
			return (float)((double)num / 2.0 * (Math.Pow(s - 2.0, power) + (double)(num * 2)));
		}
	}

	private static class Circular
	{
		public static float EaseIn(double s)
		{
			return (float)(0.0 - (Math.Sqrt(1.0 - s * s) - 1.0));
		}

		public static float EaseOut(double s)
		{
			return (float)Math.Sqrt(1.0 - (s - 1.0) * s);
		}

		public static float EaseInOut(double s)
		{
			s *= 2.0;
			if (s < 1.0)
			{
				return EaseIn(s) / 2f;
			}
			return (float)(Math.Sqrt(1.0 - (s - 2.0) * s) + 1.0) / 2f;
		}
	}

	public static float Ease(double linearStep, float acceleration, EasingType type)
	{
		float value = ((acceleration > 0f) ? EaseIn(linearStep, type) : ((acceleration < 0f) ? EaseOut(linearStep, type) : ((float)linearStep)));
		return MathHelper.Lerp((float)linearStep, value, Math.Abs(Math.Min(acceleration, 1f)));
	}

	public static float EaseIn(double linearStep, EasingType type)
	{
		return type switch
		{
			EasingType.None => 1f, 
			EasingType.Linear => (float)linearStep, 
			EasingType.Sine => Sine.EaseIn(linearStep), 
			EasingType.Quadratic => Power.EaseIn(linearStep, 2), 
			EasingType.Cubic => Power.EaseIn(linearStep, 3), 
			EasingType.Quartic => Power.EaseIn(linearStep, 4), 
			EasingType.Quintic => Power.EaseIn(linearStep, 5), 
			EasingType.Sextic => Power.EaseIn(linearStep, 6), 
			EasingType.Septic => Power.EaseIn(linearStep, 7), 
			EasingType.Octic => Power.EaseIn(linearStep, 8), 
			EasingType.Nonic => Power.EaseIn(linearStep, 9), 
			EasingType.Decic => Power.EaseIn(linearStep, 10), 
			EasingType.Circular => Circular.EaseIn(linearStep), 
			_ => throw new NotImplementedException(), 
		};
	}

	public static float EaseOut(double linearStep, EasingType type)
	{
		return type switch
		{
			EasingType.None => 1f, 
			EasingType.Linear => (float)linearStep, 
			EasingType.Sine => Sine.EaseOut(linearStep), 
			EasingType.Quadratic => Power.EaseOut(linearStep, 2), 
			EasingType.Cubic => Power.EaseOut(linearStep, 3), 
			EasingType.Quartic => Power.EaseOut(linearStep, 4), 
			EasingType.Quintic => Power.EaseOut(linearStep, 5), 
			EasingType.Sextic => Power.EaseOut(linearStep, 6), 
			EasingType.Septic => Power.EaseOut(linearStep, 7), 
			EasingType.Octic => Power.EaseOut(linearStep, 8), 
			EasingType.Nonic => Power.EaseOut(linearStep, 9), 
			EasingType.Decic => Power.EaseOut(linearStep, 10), 
			EasingType.Circular => Circular.EaseOut(linearStep), 
			_ => throw new NotImplementedException(), 
		};
	}

	public static float EaseInOut(double linearStep, EasingType easeInType, float acceleration, EasingType easeOutType, float deceleration)
	{
		if (!(linearStep < 0.5))
		{
			return MathHelper.Lerp((float)linearStep, EaseInOut(linearStep, easeOutType), deceleration);
		}
		return MathHelper.Lerp((float)linearStep, EaseInOut(linearStep, easeInType), acceleration);
	}

	public static float EaseInOut(double linearStep, EasingType easeInType, EasingType easeOutType)
	{
		if (!(linearStep < 0.5))
		{
			return EaseInOut(linearStep, easeOutType);
		}
		return EaseInOut(linearStep, easeInType);
	}

	public static float EaseInOut(double linearStep, EasingType type)
	{
		return type switch
		{
			EasingType.None => 1f, 
			EasingType.Linear => (float)linearStep, 
			EasingType.Sine => Sine.EaseInOut(linearStep), 
			EasingType.Quadratic => Power.EaseInOut(linearStep, 2), 
			EasingType.Cubic => Power.EaseInOut(linearStep, 3), 
			EasingType.Quartic => Power.EaseInOut(linearStep, 4), 
			EasingType.Quintic => Power.EaseInOut(linearStep, 5), 
			EasingType.Sextic => Power.EaseInOut(linearStep, 6), 
			EasingType.Septic => Power.EaseInOut(linearStep, 7), 
			EasingType.Octic => Power.EaseInOut(linearStep, 8), 
			EasingType.Nonic => Power.EaseInOut(linearStep, 9), 
			EasingType.Decic => Power.EaseInOut(linearStep, 10), 
			EasingType.Circular => Circular.EaseInOut(linearStep), 
			_ => throw new NotImplementedException(), 
		};
	}
}
