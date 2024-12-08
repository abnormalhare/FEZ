using System;

namespace FezEngine.Tools;

public interface IThreadPool
{
	Worker<TContext> Take<TContext>(Action<TContext> task);

	Worker<TContext> TakeShared<TContext>(Action<TContext> task);

	void Return<TContext>(Worker<TContext> thread);
}
