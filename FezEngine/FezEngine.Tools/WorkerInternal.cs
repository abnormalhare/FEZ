namespace FezEngine.Tools;

public abstract class WorkerInternal : IWorker
{
	internal abstract PersistentThread UnderlyingThread { get; }

	internal abstract void Abort();

	internal abstract void Dispose();

	public abstract void Act();

	public abstract void OnFinished();
}
