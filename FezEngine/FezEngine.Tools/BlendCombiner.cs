using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Tools;

public class BlendCombiner
{
	private readonly Dictionary<int, BlendState> stateObjectCache = new Dictionary<int, BlendState>();

	private BlendFunction colorBlendFunction;

	private Blend colorSourceBlend;

	private Blend colorDestinationBlend;

	private BlendingMode blendingMode = BlendingMode.Alphablending;

	public BlendFunction AlphaBlendFunction { get; set; }

	public Blend AlphaSourceBlend { get; set; }

	public Blend AlphaDestinationBlend { get; set; }

	public ColorWriteChannels ColorWriteChannels { get; set; }

	public BlendingMode BlendingMode
	{
		get
		{
			return blendingMode;
		}
		set
		{
			blendingMode = value;
			switch (value)
			{
			case BlendingMode.Additive:
				colorSourceBlend = BlendState.Additive.ColorSourceBlend;
				colorDestinationBlend = BlendState.Additive.ColorDestinationBlend;
				colorBlendFunction = BlendState.Additive.ColorBlendFunction;
				break;
			case BlendingMode.Screen:
				colorBlendFunction = BlendFunction.Add;
				colorSourceBlend = Blend.InverseDestinationColor;
				colorDestinationBlend = Blend.One;
				break;
			case BlendingMode.Multiply:
				colorBlendFunction = BlendFunction.Add;
				colorSourceBlend = Blend.DestinationColor;
				colorDestinationBlend = Blend.Zero;
				break;
			case BlendingMode.Multiply2X:
				colorBlendFunction = BlendFunction.Add;
				colorSourceBlend = Blend.DestinationColor;
				colorDestinationBlend = Blend.SourceColor;
				break;
			case BlendingMode.Alphablending:
				colorSourceBlend = BlendState.NonPremultiplied.ColorSourceBlend;
				colorDestinationBlend = BlendState.NonPremultiplied.ColorDestinationBlend;
				colorBlendFunction = BlendState.NonPremultiplied.ColorBlendFunction;
				break;
			case BlendingMode.Maximum:
				colorBlendFunction = BlendFunction.Max;
				colorSourceBlend = Blend.One;
				colorDestinationBlend = Blend.One;
				break;
			case BlendingMode.Minimum:
				colorBlendFunction = BlendFunction.Min;
				colorSourceBlend = Blend.One;
				colorDestinationBlend = Blend.One;
				break;
			case BlendingMode.Subtract:
				colorBlendFunction = BlendFunction.ReverseSubtract;
				colorSourceBlend = Blend.One;
				colorDestinationBlend = Blend.One;
				break;
			case BlendingMode.StarsOverClouds:
				colorBlendFunction = BlendFunction.Add;
				colorSourceBlend = Blend.One;
				colorDestinationBlend = Blend.InverseSourceColor;
				break;
			case BlendingMode.Opaque:
				colorSourceBlend = BlendState.Opaque.ColorSourceBlend;
				colorDestinationBlend = BlendState.Opaque.ColorDestinationBlend;
				colorBlendFunction = BlendState.Opaque.ColorBlendFunction;
				break;
			case BlendingMode.Lightmap:
				colorSourceBlend = BlendState.Opaque.ColorSourceBlend;
				colorDestinationBlend = BlendState.Opaque.ColorDestinationBlend;
				colorBlendFunction = BlendState.Opaque.ColorBlendFunction;
				break;
			}
		}
	}

	public BlendState Current => FindOrCreateStateObject(CalculateNewHash());

	internal void Apply(GraphicsDevice device)
	{
		int hash = CalculateNewHash();
		device.BlendState = FindOrCreateStateObject(hash);
	}

	private BlendState FindOrCreateStateObject(int hash)
	{
		if (!stateObjectCache.TryGetValue(hash, out var value))
		{
			value = new BlendState
			{
				ColorBlendFunction = colorBlendFunction,
				ColorSourceBlend = colorSourceBlend,
				ColorDestinationBlend = colorDestinationBlend,
				ColorWriteChannels = ColorWriteChannels,
				AlphaBlendFunction = AlphaBlendFunction,
				AlphaSourceBlend = AlphaSourceBlend,
				AlphaDestinationBlend = AlphaDestinationBlend
			};
			stateObjectCache.Add(hash, value);
		}
		return value;
	}

	private int CalculateNewHash()
	{
		return (byte)colorBlendFunction | ((byte)colorSourceBlend << 3) | ((byte)colorDestinationBlend << 7) | ((byte)AlphaBlendFunction << 11) | ((byte)AlphaSourceBlend << 14) | ((byte)AlphaDestinationBlend << 18) | ((byte)ColorWriteChannels << 22);
	}
}
