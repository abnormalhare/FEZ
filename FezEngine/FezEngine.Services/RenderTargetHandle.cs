using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Services;

public class RenderTargetHandle
{
	public RenderTarget2D Target { get; set; }

	public bool Locked { get; set; }
}
