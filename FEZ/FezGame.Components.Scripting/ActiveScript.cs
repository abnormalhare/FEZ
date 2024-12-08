using System;
using System.Collections.Generic;
using Common;
using FezEngine.Components;
using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;

namespace FezGame.Components.Scripting;

internal class ActiveScript : IDisposable
{
	private readonly List<LongRunningAction> runningActions = new List<LongRunningAction>();

	private readonly Queue<RunnableAction> queuedActions = new Queue<RunnableAction>();

	public readonly ScriptTrigger InitiatingTrigger;

	public readonly Script Script;

	private TimeSpan RunningTime;

	public bool IsDisposed { get; private set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IScriptingManager Scripting { private get; set; }

	public event Action Disposed = Util.NullAction;

	public ActiveScript(Script script, ScriptTrigger initiatingTrigger)
	{
		ServiceHelper.InjectServices(this);
		Script = script;
		InitiatingTrigger = initiatingTrigger;
	}

	public void EnqueueAction(RunnableAction runnableAction)
	{
		queuedActions.Enqueue(runnableAction);
	}

	public void Update(TimeSpan elapsed)
	{
		if (IsDisposed)
		{
			return;
		}
		RunningTime += elapsed;
		while (queuedActions.Count > 0 && (runningActions.Count == 0 || !queuedActions.Peek().Action.Blocking))
		{
			StartAction(queuedActions.Dequeue());
		}
		if (runningActions.Count != 0)
		{
			if (!Script.Timeout.HasValue)
			{
				return;
			}
			TimeSpan runningTime = RunningTime;
			TimeSpan? timeout = Script.Timeout;
			if (!(runningTime > timeout))
			{
				return;
			}
		}
		Dispose();
	}

	private void StartAction(RunnableAction runnableAction)
	{
		LongRunningAction runningAction = runnableAction.Invocation() as LongRunningAction;
		runnableAction.Invocation = null;
		if (runningAction == null)
		{
			return;
		}
		LongRunningAction longRunningAction = runningAction;
		longRunningAction.Ended = (Action)Delegate.Combine(longRunningAction.Ended, (Action)delegate
		{
			runningActions.Remove(runningAction);
			if (runnableAction.Action.Killswitch)
			{
				Dispose();
			}
		});
		runningActions.Add(runningAction);
	}

	public void Dispose()
	{
		LongRunningAction[] array = runningActions.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			ServiceHelper.RemoveComponent(array[i]);
		}
		IsDisposed = true;
		this.Disposed();
	}
}
