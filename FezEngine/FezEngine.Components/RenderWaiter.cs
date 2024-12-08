using System;
using Common;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

internal class RenderWaiter<T> : Waiter<T>, IDrawable where T : class, new()
{
	private int drawOrder;

	public bool Visible => true;

	public int DrawOrder
	{
		get
		{
			return drawOrder;
		}
		set
		{
			drawOrder = value;
			if (this.DrawOrderChanged != null)
			{
				this.DrawOrderChanged(this, EventArgs.Empty);
			}
		}
	}

	public event EventHandler<EventArgs> VisibleChanged;

	public event EventHandler<EventArgs> DrawOrderChanged;

	internal RenderWaiter(Func<T, bool> condition, Action onValid)
		: base(condition, (Action<TimeSpan, T>)Util.NullAction, onValid, new T())
	{
	}

	internal RenderWaiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting)
		: base(condition, whileWaiting, (Action)Util.NullAction, new T())
	{
	}

	internal RenderWaiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting, Action onValid)
		: base(condition, whileWaiting, onValid, new T())
	{
	}

	internal RenderWaiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting, T state)
		: base(condition, whileWaiting, (Action)Util.NullAction, state)
	{
	}

	internal RenderWaiter(Func<T, bool> condition, Action<TimeSpan, T> whileWaiting, Action onValid, T state)
		: base(condition, whileWaiting, onValid, state)
	{
	}

	public void Draw(GameTime gameTime)
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
