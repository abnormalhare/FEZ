using FezEngine.Tools;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure;

public class FutureTexture2D
{
	public struct MipLevel
	{
		public long StreamOffset;

		public int SizeInBytes;
	}

	public SurfaceFormat Format;

	public int Width;

	public int Height;

	public byte[] BackingStream;

	public MipLevel[] MipLevels;

	public Texture2D Create()
	{
		Texture2D texture2D = new Texture2D(ServiceHelper.Game.GraphicsDevice, Width, Height, MipLevels.Length > 1, Format);
		for (int i = 0; i < MipLevels.Length; i++)
		{
			texture2D.SetData(i, null, BackingStream, (int)MipLevels[i].StreamOffset, MipLevels[i].SizeInBytes);
		}
		return texture2D;
	}
}
