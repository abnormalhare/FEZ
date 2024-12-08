using System;
using Common;
using FezEngine.Tools;

namespace FezEngine.Components;

public static class Waiters
{
	private class TimeKeeper
	{
		public TimeSpan Elapsed;
	}

	public static IWaiter DoUntil(Func<bool> endCondition, Action<float> action)
	{
		return DoUntil(endCondition, action, Util.NullAction);
	}

	public static IWaiter DoUntil(Func<bool> endCondition, Action<float> action, Action onComplete)
	{
		Waiter waiter = new Waiter(endCondition, delegate(TimeSpan elapsed)
		{
			action((float)elapsed.TotalSeconds);
		}, onComplete);
		ServiceHelper.AddComponent(waiter);
		return waiter;
	}

	public static IWaiter Wait(Func<bool> endCondition, Action onValid)
	{
		Waiter waiter = new Waiter(endCondition, onValid);
		ServiceHelper.AddComponent(waiter);
		return waiter;
	}

	public static IWaiter Wait(double secondsToWait, Action onValid)
	{
		UpdateWaiter<TimeKeeper> updateWaiter = new UpdateWaiter<TimeKeeper>((TimeKeeper waited) => waited.Elapsed.TotalSeconds > secondsToWait, delegate(TimeSpan elapsed, TimeKeeper waited)
		{
			waited.Elapsed += elapsed;
		}, onValid);
		ServiceHelper.AddComponent(updateWaiter);
		return updateWaiter;
	}

	public static IWaiter Wait(double secondsToWait, Func<float, bool> earlyOutCondition, Action onValid)
	{
		UpdateWaiter<TimeKeeper> updateWaiter = new UpdateWaiter<TimeKeeper>((TimeKeeper waited) => earlyOutCondition((float)waited.Elapsed.TotalSeconds) || waited.Elapsed.TotalSeconds > secondsToWait, delegate(TimeSpan elapsed, TimeKeeper waited)
		{
			waited.Elapsed += elapsed;
		}, onValid);
		ServiceHelper.AddComponent(updateWaiter);
		return updateWaiter;
	}

	public static IWaiter Interpolate(double durationSeconds, Action<float> assignation)
	{
		return Interpolate(durationSeconds, assignation, Util.NullAction);
	}

	public static IWaiter Interpolate(double durationSeconds, Action<float> assignation, Action onComplete)
	{
		if (durationSeconds == 0.0)
		{
			onComplete();
			return null;
		}
		RenderWaiter<TimeKeeper> renderWaiter = new RenderWaiter<TimeKeeper>((TimeKeeper waited) => waited.Elapsed.TotalSeconds > durationSeconds, delegate(TimeSpan elapsed, TimeKeeper waited)
		{
			waited.Elapsed += elapsed;
			assignation((float)(waited.Elapsed.TotalSeconds / durationSeconds));
		}, onComplete);
		ServiceHelper.AddComponent(renderWaiter);
		return renderWaiter;
	}
}
