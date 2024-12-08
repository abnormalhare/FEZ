using System;
using ContentSerialization.Attributes;
using FezEngine.Effects.Structures;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure;

public class AnimatedTexture : IDisposable
{
	public Texture2D Texture { get; set; }

	public Rectangle[] Offsets { get; set; }

	public AnimationTiming Timing { get; set; }

	public int FrameWidth { get; set; }

	public int FrameHeight { get; set; }

	public Vector2 PotOffset { get; set; }

	[Serialization(Ignore = true)]
	public bool NoHat { get; set; }

	public void Dispose()
	{
		if (Texture != null)
		{
			Texture.Unhook();
			Texture.Dispose();
		}
		Texture = null;
		Timing = null;
	}
}
