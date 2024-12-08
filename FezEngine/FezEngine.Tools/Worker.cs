using System;
using System.Threading;

namespace FezEngine.Tools;

public class Worker<TContext> : WorkerInternal
{
	internal Action<TContext> task;

	private readonly PersistentThread thread;

	public Action OnAbort;

	private TContext cachedContext;

	public bool Aborted { get; private set; }

	public ThreadPriority Priority
	{
		set
		{
			thread.Priority = value;
		}
	}

	internal override PersistentThread UnderlyingThread => thread;

	public event Action Finished;

	public override void OnFinished()
	{
		if (this.Finished != null)
		{
			this.Finished();
		}
	}

	internal Worker(PersistentThread thread, Action<TContext> task)
	{
		this.task = task;
		this.thread = thread;
	}

	public override void Act()
	{
		task(cachedContext);
	}

	public void Start(TContext context)
	{
		if (thread.Started)
		{
			throw new InvalidOperationException("Thread is already started");
		}
		if (thread.Disposed)
		{
			throw new ObjectDisposedException("PersistentThread");
		}
		cachedContext = context;
		thread.CurrentWorker = this;
		thread.Start();
	}

	public void Join()
	{
		if (thread.Started)
		{
			if (thread.Disposed)
			{
				throw new ObjectDisposedException("PersistentThread");
			}
			thread.Join();
		}
	}

	internal override void Dispose()
	{
		if (thread.Started)
		{
			thread.Join();
		}
		thread.Priority = ThreadPriority.Lowest;
		this.Finished = null;
		task = null;
	}

	internal override void Abort()
	{
		Aborted = true;
		if (OnAbort != null)
		{
			OnAbort();
		}
	}
}
