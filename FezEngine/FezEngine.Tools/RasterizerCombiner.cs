using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Tools;

public class RasterizerCombiner
{
	private readonly Dictionary<long, RasterizerState> stateObjectCache = new Dictionary<long, RasterizerState>();

	public CullMode CullMode { get; set; }

	public FillMode FillMode { get; set; }

	public float DepthBias { get; set; }

	public float SlopeScaleDepthBias { get; set; }

	public RasterizerState Current => FindOrCreateStateObject(CalculateNewHash());

	internal void Apply(GraphicsDevice device)
	{
		long hash = CalculateNewHash();
		device.RasterizerState = FindOrCreateStateObject(hash);
	}

	private RasterizerState FindOrCreateStateObject(long hash)
	{
		if (!stateObjectCache.TryGetValue(hash, out var value))
		{
			value = new RasterizerState
			{
				CullMode = CullMode,
				FillMode = FillMode,
				DepthBias = DepthBias,
				SlopeScaleDepthBias = SlopeScaleDepthBias
			};
			stateObjectCache.Add(hash, value);
		}
		return value;
	}

	private long CalculateNewHash()
	{
		int num = (int)BitConverter.DoubleToInt64Bits(DepthBias) >> 4;
		int num2 = (int)BitConverter.DoubleToInt64Bits(SlopeScaleDepthBias) >> 4;
		return num | (num2 << 30) | ((byte)CullMode << 28) | ((byte)FillMode << 30);
	}
}
