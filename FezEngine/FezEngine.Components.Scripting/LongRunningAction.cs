using System;
using Common;
using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components.Scripting;

public class LongRunningAction : GameComponent
{
	public Func<float, float, bool> OnUpdate;

	public Action OnDispose;

	public Action Ended;

	private bool disposed;

	private float sinceStarted;

	[ServiceDependency]
	public IEngineStateManager EngineState { protected get; set; }

	public LongRunningAction()
		: this(Util.NullFunc<float, float, bool>)
	{
	}

	public LongRunningAction(Action onDispose)
		: this(Util.NullFunc<float, float, bool>, onDispose)
	{
	}

	public LongRunningAction(Func<float, float, bool> onUpdate)
		: this(onUpdate, Util.NullAction)
	{
	}

	public LongRunningAction(Func<float, float, bool> onUpdate, Action onDispose)
		: base(ServiceHelper.Game)
	{
		ServiceHelper.AddComponent(this);
		OnDispose = onDispose;
		OnUpdate = onUpdate;
	}

	public override void Update(GameTime gameTime)
	{
		if (!EngineState.Paused && !EngineState.InMap && !EngineState.Loading && !disposed)
		{
			sinceStarted += (float)gameTime.ElapsedGameTime.TotalSeconds;
			if (OnUpdate((float)gameTime.ElapsedGameTime.TotalSeconds, sinceStarted))
			{
				ServiceHelper.RemoveComponent(this);
			}
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (!disposed)
		{
			disposed = true;
			base.Dispose(disposing);
			if (OnDispose != null)
			{
				OnDispose();
			}
			if (Ended != null)
			{
				Ended();
			}
			OnUpdate = null;
			OnDispose = null;
			Ended = null;
		}
	}
}
