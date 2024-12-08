using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public struct VirtualTrile
{
	public int VerticalOffset;

	public TrileInstance Instance { get; set; }

	public Vector3 Position => Instance.Position + new Vector3(0f, VerticalOffset, 0f);
}
