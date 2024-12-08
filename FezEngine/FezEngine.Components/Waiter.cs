using System;
using Common;
using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

internal class Waiter : IGameComponent, IUpdateable, IWaiter
{
	private readonly IEngineStateManager EngineState;

	private readonly IDefaultCameraManager Camera;

	private Func<bool> condition;

	private Action<TimeSpan> whileWaiting;

	private Action onValid;

	private int updateOrder;

	public bool Alive { get; private set; }

	public object Tag { get; set; }

	public bool AutoPause { get; set; }

	public Func<bool> CustomPause { get; set; }

	public bool Enabled => true;

	public int UpdateOrder
	{
		get
		{
			return updateOrder;
		}
		set
		{
			updateOrder = value;
			if (this.UpdateOrderChanged != null)
			{
				this.UpdateOrderChanged(this, EventArgs.Empty);
			}
		}
	}

	public event EventHandler<EventArgs> EnabledChanged;

	public event EventHandler<EventArgs> UpdateOrderChanged;

	internal Waiter(Func<bool> condition, Action onValid)
		: this(condition, Util.NullAction, onValid)
	{
	}

	internal Waiter(Func<bool> condition, Action<TimeSpan> whileWaiting)
		: this(condition, whileWaiting, Util.NullAction)
	{
	}

	internal Waiter(Func<bool> condition, Action<TimeSpan> whileWaiting, Action onValid)
	{
		this.condition = condition;
		this.whileWaiting = whileWaiting;
		this.onValid = onValid;
		Alive = true;
		EngineState = ServiceHelper.Get<IEngineStateManager>();
		Camera = ServiceHelper.Get<IDefaultCameraManager>();
	}

	public void Update(GameTime gameTime)
	{
		if ((!AutoPause || (!EngineState.Paused && Camera.ActionRunning && Camera.Viewpoint.IsOrthographic() && (CustomPause == null || !CustomPause()))) && Alive)
		{
			if (condition())
			{
				onValid();
				Kill();
			}
			else
			{
				whileWaiting(gameTime.ElapsedGameTime);
			}
		}
	}

	public void Cancel()
	{
		if (Alive)
		{
			Kill();
		}
	}

	private void Kill()
	{
		Alive = false;
		condition = null;
		onValid = null;
		whileWaiting = null;
		ServiceHelper.RemoveComponent(this);
	}

	public void Initialize()
	{
	}
}
internal class Waiter<T> : IGameComponent, IWaiter where T : class, new()
{
	protected readonly IEngineStateManager EngineState;

	protected readonly IDefaultCameraManager Camera;

	protected Func<T, bool> condition;

	protected Action<TimeSpan, T> whileWaiting;

	protected Action onValid;

	protected readonly T state;

	public bool Alive { get; private set; }

	public object Tag { get; set; }

	public bool AutoPause { get; set; }

	public Func<bool> CustomPause { get; set; }

	internal Waiter(Func<T, bool> condition, Action onValid)
		: this(condition, (Action<TimeSpan, T>)Util.NullAction, onValid, new T())
	{
	}

	internal Waiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting)
		: this(condition, whileWaiting, (Action)Util.NullAction, new T())
	{
	}

	internal Waiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting, Action onValid)
		: this(condition, whileWaiting, onValid, new T())
	{
	}

	internal Waiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting, T state)
		: this(condition, whileWaiting, (Action)Util.NullAction, state)
	{
	}

	internal Waiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting, Action onValid, T state)
	{
		this.condition = condition;
		this.whileWaiting = whileWaiting;
		this.onValid = onValid;
		this.state = state;
		Alive = true;
		EngineState = ServiceHelper.Get<IEngineStateManager>();
		Camera = ServiceHelper.Get<IDefaultCameraManager>();
	}

	public void Cancel()
	{
		if (Alive)
		{
			Kill();
		}
	}

	protected void Kill()
	{
		Alive = false;
		whileWaiting = null;
		onValid = null;
		condition = null;
		ServiceHelper.RemoveComponent(this);
	}

	public void Initialize()
	{
	}
}
