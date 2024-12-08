using System;

namespace FezEngine.Structure.Input;

[Flags]
public enum CodeInput
{
	None = 0,
	Up = 1,
	Down = 2,
	Left = 4,
	Right = 8,
	SpinLeft = 0x10,
	SpinRight = 0x20,
	Jump = 0x40
}
