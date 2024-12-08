using FezEngine.Structure.Input;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public interface IGamepadsManager
{
	GamepadState this[PlayerIndex index] { get; }
}
