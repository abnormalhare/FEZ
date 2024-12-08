using System;
using FezEngine.Structure;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Tools;

public static class GraphicsDeviceExtensions
{
	private static readonly DepthStencilCombiner dssCombiner = new DepthStencilCombiner();

	private static readonly BlendCombiner blendCombiner = new BlendCombiner();

	private static readonly RasterizerCombiner rasterCombiner = new RasterizerCombiner();

	public static void BeginPoint(this SpriteBatch spriteBatch)
	{
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, RasterizerState.CullCounterClockwise);
	}

	public static void BeginLinear(this SpriteBatch spriteBatch)
	{
		spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp, null, RasterizerState.CullCounterClockwise);
	}

	public static void SetGamma(this GraphicsDevice device, float brightness)
	{
		double num = ((double)brightness - 0.5) * 0.25;
		double num2 = (double)brightness - 0.5 + 1.0;
		short[] array = new short[256];
		short[] array2 = new short[256];
		short[] array3 = new short[256];
		for (int i = 0; i < 256; i++)
		{
			ushort num3 = (ushort)Math.Round(Math.Max(Math.Min(Math.Pow((double)i / 255.0, 1.0 / num2) + num, 1.0), 0.0) * 65535.0);
			array[i] = (array2[i] = (array3[i] = (short)num3));
		}
	}

	public static void PrepareDraw(this GraphicsDevice device)
	{
		device.DepthStencilState = DepthStencilStates.DefaultWithStencil;
		device.BlendState = BlendState.NonPremultiplied;
		device.SamplerStates[0] = SamplerState.PointClamp;
		device.RasterizerState = RasterizerState.CullCounterClockwise;
	}

	public static DepthStencilCombiner GetDssCombiner(this GraphicsDevice _)
	{
		return dssCombiner;
	}

	public static void PrepareStencilWrite(this GraphicsDevice _, StencilMask? reference)
	{
		dssCombiner.StencilEnable = true;
		dssCombiner.StencilPass = StencilOperation.Replace;
		dssCombiner.StencilFunction = CompareFunction.Always;
		if (reference.HasValue)
		{
			dssCombiner.ReferenceStencil = (int)reference.Value;
		}
	}

	public static void PrepareStencilRead(this GraphicsDevice _, CompareFunction comparison, StencilMask reference)
	{
		dssCombiner.StencilEnable = true;
		dssCombiner.StencilPass = StencilOperation.Keep;
		dssCombiner.StencilFunction = comparison;
		dssCombiner.ReferenceStencil = (int)reference;
	}

	public static void PrepareStencilReadWrite(this GraphicsDevice _, CompareFunction comparison, StencilMask reference)
	{
		dssCombiner.StencilEnable = true;
		dssCombiner.StencilPass = StencilOperation.Replace;
		dssCombiner.StencilFunction = comparison;
		dssCombiner.ReferenceStencil = (int)reference;
	}

	public static BlendCombiner GetBlendCombiner(this GraphicsDevice _)
	{
		return blendCombiner;
	}

	public static RasterizerCombiner GetRasterCombiner(this GraphicsDevice _)
	{
		return rasterCombiner;
	}

	public static void ApplyCombiners(this GraphicsDevice device)
	{
		dssCombiner.Apply(device);
		blendCombiner.Apply(device);
		rasterCombiner.Apply(device);
	}

	public static void SetCullMode(this GraphicsDevice _, CullMode cullMode)
	{
		rasterCombiner.CullMode = cullMode;
	}

	public static void SetBlendingMode(this GraphicsDevice _, BlendingMode blendingMode)
	{
		blendCombiner.BlendingMode = blendingMode;
	}

	public static void SetColorWriteChannels(this GraphicsDevice _, ColorWriteChannels channels)
	{
		blendCombiner.ColorWriteChannels = channels;
	}

	public static void ResetAlphaBlending(this GraphicsDevice _)
	{
		blendCombiner.AlphaBlendFunction = BlendFunction.Add;
		blendCombiner.AlphaDestinationBlend = Blend.Zero;
		blendCombiner.AlphaSourceBlend = Blend.One;
	}
}
