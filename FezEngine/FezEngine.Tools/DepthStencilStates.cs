using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Tools;

public static class DepthStencilStates
{
	public static readonly DepthStencilState DefaultWithStencil = new DepthStencilState
	{
		StencilEnable = true
	};
}
