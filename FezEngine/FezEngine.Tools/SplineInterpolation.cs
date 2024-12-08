using System;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools;

public abstract class SplineInterpolation<T>
{
	public static EasingType EaseInType = EasingType.None;

	public static EasingType EaseOutType = EasingType.Quadratic;

	public static bool LongScreenshot;

	private TimeSpan totalElapsed;

	private TimeSpan duration;

	private static readonly GameTime EmptyGameTime = new GameTime();

	public T[] Points { get; private set; }

	public T Current { get; protected set; }

	public bool Reached { get; private set; }

	public bool Paused { get; private set; }

	public float TotalStep { get; private set; }

	protected SplineInterpolation(TimeSpan duration, params T[] points)
	{
		Paused = true;
		this.duration = duration;
		Points = points;
	}

	public void Start()
	{
		Paused = false;
		Update(EmptyGameTime);
	}

	public void Update(GameTime gameTime)
	{
		if (!Reached && !Paused)
		{
			totalElapsed += gameTime.ElapsedGameTime;
			int num = Points.Length - 1;
			if (totalElapsed >= duration)
			{
				TotalStep = 1f;
				Current = Points[num];
				totalElapsed = duration;
				Reached = true;
				Paused = true;
				return;
			}
			float num2 = ((EaseInType == EasingType.None) ? Easing.EaseOut((double)totalElapsed.Ticks / (double)duration.Ticks, EaseOutType) : ((EaseOutType == EasingType.None) ? Easing.EaseIn((double)totalElapsed.Ticks / (double)duration.Ticks, EaseInType) : ((EaseInType != EaseOutType) ? Easing.EaseInOut((double)totalElapsed.Ticks / (double)duration.Ticks, EaseInType, EaseOutType) : Easing.EaseInOut((double)totalElapsed.Ticks / (double)duration.Ticks, EaseInType))));
			int num3 = (int)MathHelper.Clamp((float)num * num2 - 1f, 0f, num);
			int num4 = (int)MathHelper.Clamp((float)num * num2, 0f, num);
			int num5 = (int)MathHelper.Clamp((float)num * num2 + 1f, 0f, num);
			int num6 = (int)MathHelper.Clamp((float)num * num2 + 2f, 0f, num);
			double num7 = (double)num4 / (double)num;
			double num8 = (double)num5 / (double)num - num7;
			float t = (float)FezMath.Saturate(((double)num2 - num7) / ((num8 == 0.0) ? 1.0 : num8));
			TotalStep = num2;
			Interpolate(Points[num3], Points[num4], Points[num5], Points[num6], t);
		}
	}

	protected abstract void Interpolate(T p0, T p1, T p2, T p3, float t);
}
