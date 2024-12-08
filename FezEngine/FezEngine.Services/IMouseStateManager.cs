using System;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public interface IMouseStateManager
{
	MouseButtonState LeftButton { get; }

	MouseButtonState MiddleButton { get; }

	MouseButtonState RightButton { get; }

	int WheelTurns { get; }

	FezButtonState WheelTurnedUp { get; }

	FezButtonState WheelTurnedDown { get; }

	Point Position { get; }

	Point Movement { get; }

	IntPtr RenderPanelHandle { set; }

	IntPtr ParentFormHandle { set; }

	void Update(GameTime time);
}
