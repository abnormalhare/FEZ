using System;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FezEngine.Services;

public class MouseStateManager : IMouseStateManager
{
	private const int DraggingThreshold = 3;

	private IEngineStateManager EngineState;

	private MouseState lastState;

	private Point dragOffset;

	private MouseButtonState leftButton;

	private MouseButtonState middleButton;

	private MouseButtonState rightButton;

	private FezButtonState wheelTurnedUp;

	private FezButtonState wheelTurnedDown;

	private int wheelTurns;

	private Point position;

	private Point movement;

	private IntPtr renderPanelHandle;

	private IntPtr parentFormHandle;

	public MouseButtonState LeftButton => leftButton;

	public MouseButtonState MiddleButton => middleButton;

	public MouseButtonState RightButton => rightButton;

	public int WheelTurns => wheelTurns;

	public Point Position => position;

	public Point Movement => movement;

	public IntPtr RenderPanelHandle
	{
		set
		{
			renderPanelHandle = value;
		}
	}

	public IntPtr ParentFormHandle
	{
		set
		{
			parentFormHandle = value;
		}
	}

	public FezButtonState WheelTurnedUp => wheelTurnedUp;

	public FezButtonState WheelTurnedDown => wheelTurnedDown;

	public void Update(GameTime time)
	{
		MouseState state = Mouse.GetState();
		wheelTurns = state.ScrollWheelValue - lastState.ScrollWheelValue;
		wheelTurnedUp = wheelTurnedUp.NextState(wheelTurns > 0);
		wheelTurnedDown = wheelTurnedDown.NextState(wheelTurns < 0);
		if (renderPanelHandle != parentFormHandle)
		{
			state = Mouse.GetState();
		}
		movement = new Point(state.X - position.X - dragOffset.X, state.Y - position.Y - dragOffset.Y);
		position = new Point(state.X + dragOffset.X, state.Y + dragOffset.Y);
		if (ServiceHelper.Game.IsActive && state.LeftButton == ButtonState.Pressed && (EngineState ?? (EngineState = ServiceHelper.Get<IEngineStateManager>())).InFpsMode)
		{
			Rectangle rectangle = (ServiceHelper.Game.GraphicsDevice.PresentationParameters.IsFullScreen ? ServiceHelper.Game.GraphicsDevice.Viewport.Bounds : ServiceHelper.Game.Window.ClientBounds);
			Point point = default(Point);
			if (state.X <= 0)
			{
				point.X += rectangle.Width - 2;
			}
			if (state.X >= rectangle.Width - 1)
			{
				point.X -= rectangle.Width - 2;
			}
			if (state.Y <= 0)
			{
				point.Y += rectangle.Height - 2;
			}
			if (state.Y >= rectangle.Height - 1)
			{
				point.Y -= rectangle.Height - 2;
			}
			if (point.X != 0 || point.Y != 0)
			{
				MouseState state2 = Mouse.GetState();
				Point point2 = new Point(state2.X - state.X, state2.Y - state.Y);
				Mouse.SetPosition(point2.X + state.X + point.X, point2.Y + state.Y + point.Y);
				dragOffset.X -= point.X;
				dragOffset.Y -= point.Y;
			}
		}
		else
		{
			dragOffset = Point.Zero;
		}
		if (state != lastState)
		{
			bool hasMoved = movement.X != 0 || movement.Y != 0;
			leftButton = DeduceMouseButtonState(leftButton, lastState.LeftButton, state.LeftButton, hasMoved);
			middleButton = DeduceMouseButtonState(middleButton, lastState.MiddleButton, state.MiddleButton, hasMoved);
			rightButton = DeduceMouseButtonState(rightButton, lastState.RightButton, state.RightButton, hasMoved);
			lastState = state;
		}
		else
		{
			ResetButton(ref leftButton);
			ResetButton(ref middleButton);
			ResetButton(ref rightButton);
		}
	}

	private MouseButtonState DeduceMouseButtonState(MouseButtonState lastMouseButtonState, ButtonState lastButtonState, ButtonState buttonState, bool hasMoved)
	{
		if (lastButtonState == ButtonState.Released && buttonState == ButtonState.Released)
		{
			return new MouseButtonState(MouseButtonStates.Idle);
		}
		if (lastButtonState == ButtonState.Released && buttonState == ButtonState.Pressed)
		{
			return new MouseButtonState(MouseButtonStates.Pressed, new MouseDragState(position, position));
		}
		if (lastButtonState == ButtonState.Pressed && buttonState == ButtonState.Pressed)
		{
			if (hasMoved)
			{
				if (lastMouseButtonState.DragState.PreDrag && (Math.Abs(position.X - lastMouseButtonState.DragState.Start.X) > 3 || Math.Abs(position.Y - lastMouseButtonState.DragState.Start.Y) > 3))
				{
					if (lastMouseButtonState.State == MouseButtonStates.DragStarted || lastMouseButtonState.State == MouseButtonStates.Dragging)
					{
						return new MouseButtonState(MouseButtonStates.Dragging, new MouseDragState(lastMouseButtonState.DragState.Start, position, preDrag: true));
					}
					return new MouseButtonState(MouseButtonStates.DragStarted, new MouseDragState(lastMouseButtonState.DragState.Start, position, preDrag: true));
				}
				return new MouseButtonState(MouseButtonStates.Down, new MouseDragState(lastMouseButtonState.DragState.Start, position, preDrag: true));
			}
			return lastMouseButtonState;
		}
		if (lastButtonState == ButtonState.Pressed && buttonState == ButtonState.Released)
		{
			if ((lastMouseButtonState.State == MouseButtonStates.Pressed || lastMouseButtonState.State == MouseButtonStates.Down) && !hasMoved)
			{
				return new MouseButtonState(MouseButtonStates.Clicked);
			}
			return new MouseButtonState(MouseButtonStates.DragEnded);
		}
		throw new InvalidOperationException();
	}

	private void ResetButton(ref MouseButtonState button)
	{
		if (button.State == MouseButtonStates.Pressed)
		{
			button = new MouseButtonState(MouseButtonStates.Down, button.DragState);
		}
		if (button.State == MouseButtonStates.Clicked || button.State == MouseButtonStates.DragEnded)
		{
			button = new MouseButtonState(MouseButtonStates.Idle);
		}
		if (button.State == MouseButtonStates.DragStarted)
		{
			button = new MouseButtonState(MouseButtonStates.Dragging, button.DragState);
		}
	}
}
