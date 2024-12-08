using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools;

public static class TimeInterpolation
{
	private struct OrderedCallback
	{
		public int Order;

		public Action<GameTime> Callback;
	}

	public const double TimestepMS = 17.0;

	public static readonly TimeSpan UpdateTimestep = TimeSpan.FromTicks(170000L);

	public static TimeSpan LastUpdate;

	public static bool NeedsInterpolation;

	private static readonly List<OrderedCallback> interpolationCallbacks = new List<OrderedCallback>();

	public static void RegisterCallback(Action<GameTime> callback, int order)
	{
		lock (interpolationCallbacks)
		{
			interpolationCallbacks.Add(new OrderedCallback
			{
				Callback = callback,
				Order = order
			});
			interpolationCallbacks.Sort((OrderedCallback a, OrderedCallback b) => a.Order.CompareTo(b.Order));
		}
	}

	public static void ProcessCallbacks(GameTime gameTime)
	{
		lock (interpolationCallbacks)
		{
			foreach (OrderedCallback interpolationCallback in interpolationCallbacks)
			{
				interpolationCallback.Callback(gameTime);
			}
		}
	}
}
