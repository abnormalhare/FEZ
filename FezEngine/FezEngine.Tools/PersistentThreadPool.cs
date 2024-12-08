using System;
using System.Collections.Concurrent;
using System.Threading;
using Common;
using FezEngine.Services;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools;

public class PersistentThreadPool : GameComponent, IThreadPool
{
	private const int InitialMaxThreads = 1;

	private static int MainThreadId;

	private readonly ConcurrentStack<PersistentThread> stack;

	private readonly ConcurrentDictionary<IWorker, WorkerInternal> taken = new ConcurrentDictionary<IWorker, WorkerInternal>();

	private bool disposed;

	public static bool SingleThreaded;

	public static bool IsOnMainThread => MainThreadId == Thread.CurrentThread.ManagedThreadId;

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	public PersistentThreadPool(Game game)
		: base(game)
	{
		Logger.Log("Threading", LogSeverity.Information, "Multithreading is " + (SingleThreaded ? "disabled" : "enabled"));
		MainThreadId = Thread.CurrentThread.ManagedThreadId;
		stack = new ConcurrentStack<PersistentThread>();
		for (int i = 0; i < 1; i++)
		{
			stack.Push(CreateThread());
		}
	}

	private PersistentThread CreateThread()
	{
		return new PersistentThread();
	}

	public Worker<TContext> Take<TContext>(Action<TContext> task)
	{
		if (!stack.TryPop(out var result))
		{
			result = CreateThread();
		}
		Worker<TContext> worker = new Worker<TContext>(result, task);
		taken.TryAdd(worker, worker);
		return worker;
	}

	public Worker<TContext> TakeShared<TContext>(Action<TContext> task)
	{
		if (!stack.TryPeek(out var result))
		{
			result = CreateThread();
		}
		Worker<TContext> worker = new Worker<TContext>(result, task);
		taken.TryAdd(worker, worker);
		return worker;
	}

	public void Return<TContext>(Worker<TContext> worker)
	{
		if (worker != null && taken.TryRemove(worker, out var _))
		{
			worker.Dispose();
			if (disposed)
			{
				worker.UnderlyingThread.Dispose();
			}
			else
			{
				stack.Push(worker.UnderlyingThread);
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		foreach (WorkerInternal value in taken.Values)
		{
			value.Abort();
			value.Dispose();
			stack.Push(value.UnderlyingThread);
		}
		taken.Clear();
		PersistentThread result;
		while (stack.TryPop(out result))
		{
			result.Dispose();
		}
		disposed = true;
		base.Dispose(disposing);
	}
}
