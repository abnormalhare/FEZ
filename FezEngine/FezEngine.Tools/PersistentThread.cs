using System;
using System.Threading;
using Common;

namespace FezEngine.Tools;

internal class PersistentThread : IDisposable
{
	private readonly Thread thread;

	private readonly ManualResetEventSlim startEvent;

	private readonly ManualResetEventSlim joinEvent;

	public bool Started { get; private set; }

	public bool Disposed { get; private set; }

	public IWorker CurrentWorker { private get; set; }

	public ThreadPriority Priority
	{
		set
		{
			if (!PersistentThreadPool.SingleThreaded)
			{
				thread.Priority = value;
			}
		}
	}

	public PersistentThread()
	{
		if (!PersistentThreadPool.SingleThreaded)
		{
			startEvent = new ManualResetEventSlim(initialState: false);
			joinEvent = new ManualResetEventSlim(initialState: false);
			thread = new Thread(DoWork)
			{
				Priority = ThreadPriority.Lowest
			};
			thread.Start();
		}
	}

	public void Start()
	{
		Started = true;
		if (PersistentThreadPool.SingleThreaded)
		{
			CurrentWorker.Act();
			CurrentWorker.OnFinished();
		}
		else
		{
			startEvent.Set();
		}
	}

	public void Join()
	{
		if (!PersistentThreadPool.SingleThreaded && thread != Thread.CurrentThread)
		{
			joinEvent.Wait();
			joinEvent.Reset();
		}
		Started = false;
	}

	private void DoWork()
	{
		Logger.Try(DoActualWork);
	}

	private void DoActualWork()
	{
		if (!PersistentThreadPool.SingleThreaded)
		{
			startEvent.Wait();
			startEvent.Reset();
			while (!Disposed)
			{
				CurrentWorker.Act();
				CurrentWorker.OnFinished();
				joinEvent.Set();
				startEvent.Wait();
				startEvent.Reset();
			}
		}
	}

	public void Dispose()
	{
		if (!Disposed)
		{
			GC.SuppressFinalize(this);
		}
		DisposeInternal();
	}

	private void DisposeInternal()
	{
		if (!Disposed)
		{
			Disposed = true;
			if (!PersistentThreadPool.SingleThreaded)
			{
				startEvent.Set();
			}
		}
	}

	~PersistentThread()
	{
		DisposeInternal();
	}
}
