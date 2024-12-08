using System.Collections.Generic;

namespace FezEngine.Components;

internal class LayerComparer : IEqualityComparer<Layer>
{
	public static readonly LayerComparer Default = new LayerComparer();

	public bool Equals(Layer x, Layer y)
	{
		return x == y;
	}

	public int GetHashCode(Layer obj)
	{
		return (int)obj;
	}
}
