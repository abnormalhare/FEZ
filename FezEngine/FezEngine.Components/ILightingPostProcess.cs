using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public interface ILightingPostProcess
{
	Texture2D LightmapTexture { get; }

	event Action<GameTime> DrawGeometryLights;

	event Action DrawOnTopLights;
}
