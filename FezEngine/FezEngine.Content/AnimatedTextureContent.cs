using System.Collections.Generic;

namespace FezEngine.Content;

public class AnimatedTextureContent
{
	public readonly List<FrameContent> Frames = new List<FrameContent>();

	public int FrameWidth;

	public int FrameHeight;

	public int Width;

	public int Height;

	public byte[] PackedImage;
}
