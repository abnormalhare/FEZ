using ContentSerialization.Attributes;
using Microsoft.Xna.Framework.Audio;

namespace FezEngine.Structure;

public class NpcActionContent
{
	[Serialization(Optional = true)]
	public string AnimationName { get; set; }

	[Serialization(Optional = true)]
	public string SoundName { get; set; }

	[Serialization(Ignore = true)]
	public AnimatedTexture Animation { get; set; }

	[Serialization(Ignore = true)]
	public SoundEffect Sound { get; set; }

	public NpcActionContent Clone()
	{
		return new NpcActionContent
		{
			AnimationName = AnimationName,
			SoundName = SoundName
		};
	}
}
