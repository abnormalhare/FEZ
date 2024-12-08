using System;
using Common;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

internal class UpdateWaiter<T> : Waiter<T>, IUpdateable where T : class, new()
{
	private int updateOrder;

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

	internal UpdateWaiter(Func<T, bool> condition, Action onValid)
		: base(condition, (Action<TimeSpan, T>)Util.NullAction, onValid, new T())
	{
	}

	internal UpdateWaiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting)
		: base(condition, whileWaiting, (Action)Util.NullAction, new T())
	{
	}

	internal UpdateWaiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting, Action onValid)
		: base(condition, whileWaiting, onValid, new T())
	{
	}

	internal UpdateWaiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting, T state)
		: base(condition, whileWaiting, (Action)Util.NullAction, state)
	{
	}

	internal UpdateWaiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting, Action onValid, T state)
		: base(condition, whileWaiting, onValid, state)
	{
	}

	public void Update(GameTime gameTime)
	{
		if ((!base.AutoPause || (!EngineState.Paused && Camera.ActionRunning && Camera.Viewpoint.IsOrthographic() && (base.CustomPause == null || !base.CustomPause()))) && base.Alive)
		{
			whileWaiting(gameTime.ElapsedGameTime, state);
			if (condition(state))
			{
				onValid();
				Kill();
			}
		}
	}
}
