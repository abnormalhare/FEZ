using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Tools;

public static class SamplerStates
{
	public static readonly SamplerState PointMipWrap = new SamplerState
	{
		AddressU = TextureAddressMode.Wrap,
		AddressV = TextureAddressMode.Wrap,
		Filter = TextureFilter.MinLinearMagPointMipLinear
	};

	public static readonly SamplerState PointMipClamp = new SamplerState
	{
		AddressU = TextureAddressMode.Clamp,
		AddressV = TextureAddressMode.Clamp,
		Filter = TextureFilter.MinLinearMagPointMipLinear
	};

	public static readonly SamplerState LinearUWrapVClamp = new SamplerState
	{
		AddressU = TextureAddressMode.Wrap,
		AddressV = TextureAddressMode.Clamp,
		Filter = TextureFilter.Linear
	};

	public static readonly SamplerState PointUWrapVClamp = new SamplerState
	{
		AddressU = TextureAddressMode.Wrap,
		AddressV = TextureAddressMode.Clamp,
		Filter = TextureFilter.Point
	};
}
