using System;
using System.Collections.Concurrent;

namespace FezEngine.Tools;

public static class DrawActionScheduler
{
	private static readonly ConcurrentQueue<Action> DeferredDrawActions = new ConcurrentQueue<Action>();

	public static void Schedule(Action action)
	{
		if (!PersistentThreadPool.IsOnMainThread)
		{
			DeferredDrawActions.Enqueue(action);
		}
		else
		{
			action();
		}
	}

	public static void Process()
	{
		Action result;
		while (DeferredDrawActions.TryDequeue(out result))
		{
			result();
		}
	}
}
