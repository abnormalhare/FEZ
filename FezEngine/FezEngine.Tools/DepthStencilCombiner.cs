using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Tools;

public class DepthStencilCombiner
{
	private readonly Dictionary<int, DepthStencilState> stateObjectCache = new Dictionary<int, DepthStencilState>();

	public bool DepthBufferEnable { get; set; }

	public CompareFunction DepthBufferFunction { get; set; }

	public bool DepthBufferWriteEnable { get; set; }

	public bool StencilEnable { get; set; }

	public StencilOperation StencilPass { get; set; }

	public CompareFunction StencilFunction { get; set; }

	public int ReferenceStencil { get; set; }

	public DepthStencilState Current => FindOrCreateStateObject(CalculateNewHash());

	internal void Apply(GraphicsDevice device)
	{
		int hash = CalculateNewHash();
		device.DepthStencilState = FindOrCreateStateObject(hash);
	}

	private DepthStencilState FindOrCreateStateObject(int hash)
	{
		if (!stateObjectCache.TryGetValue(hash, out var value))
		{
			value = new DepthStencilState
			{
				DepthBufferEnable = DepthBufferEnable,
				DepthBufferWriteEnable = DepthBufferWriteEnable,
				DepthBufferFunction = DepthBufferFunction,
				StencilEnable = StencilEnable,
				StencilPass = StencilPass,
				StencilFunction = StencilFunction,
				ReferenceStencil = ReferenceStencil
			};
			stateObjectCache.Add(hash, value);
		}
		return value;
	}

	private int CalculateNewHash()
	{
		return (DepthBufferEnable ? 1 : 0) | ((byte)DepthBufferFunction << 1) | ((DepthBufferWriteEnable ? 1 : 0) << 5) | ((StencilEnable ? 1 : 0) << 6) | ((byte)StencilPass << 7) | ((byte)StencilFunction << 11) | ((byte)ReferenceStencil << 15);
	}
}
