using System;

namespace FezEngine.Services;

[Flags]
public enum QueryOptions
{
	None = 0,
	Background = 1,
	Simple = 2
}
