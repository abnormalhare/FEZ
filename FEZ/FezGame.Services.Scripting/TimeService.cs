using System;
using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Services.Scripting;

public class TimeService : ITimeService, IScriptingBase
{
	public int Hour => TimeManager.CurrentTime.Hour;

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	public LongRunningAction SetHour(int hour, bool immediate)
	{
		DateTime currentTime = new DateTime(1985, 12, 23, hour, 0, 0);
		if (immediate)
		{
			TimeManager.CurrentTime = currentTime;
			return null;
		}
		long num = TimeManager.CurrentTime.Ticks;
		long destinationTicks;
		for (destinationTicks = currentTime.Ticks; num - destinationTicks > 432000000000L; destinationTicks += 864000000000L)
		{
		}
		for (; destinationTicks - num > 432000000000L; num += 864000000000L)
		{
		}
		int direction = Math.Sign(destinationTicks - num);
		destinationTicks -= direction * 36000000000L / 2;
		return new LongRunningAction(delegate(float elapsedSeconds, float totalSeconds)
		{
			bool num2 = direction != Math.Sign(destinationTicks - TimeManager.CurrentTime.Ticks);
			if (num2)
			{
				TimeManager.TimeFactor = MathHelper.Lerp(TimeManager.TimeFactor, TimeManager.DefaultTimeFactor, elapsedSeconds);
			}
			else if (totalSeconds < 1f)
			{
				TimeManager.TimeFactor = TimeManager.DefaultTimeFactor * Easing.EaseIn(FezMath.Saturate(totalSeconds), EasingType.Quadratic) * 100f * (float)direction;
			}
			return num2 && FezMath.AlmostEqual(TimeManager.TimeFactor, 360f);
		}, delegate
		{
			TimeManager.TimeFactor = TimeManager.DefaultTimeFactor;
		});
	}

	public void SetTimeFactor(int factor)
	{
		TimeManager.TimeFactor = factor;
	}

	public LongRunningAction IncrementTimeFactor(float secondsUntilDouble)
	{
		return new LongRunningAction(delegate(float elapsedSeconds, float _)
		{
			TimeManager.TimeFactor = FezMath.DoubleIter(TimeManager.TimeFactor, elapsedSeconds, secondsUntilDouble);
			return false;
		});
	}

	public void ResetEvents()
	{
	}
}
