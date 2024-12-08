using System;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public interface IFogManager
{
	FogType Type { get; set; }

	Color Color { get; set; }

	float Density { get; set; }

	float Start { get; set; }

	float End { get; set; }

	event Action FogSettingsChanged;
}
