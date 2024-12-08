using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class NpcActionContentReader : ContentTypeReader<NpcActionContent>
{
	protected override NpcActionContent Read(ContentReader input, NpcActionContent existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new NpcActionContent();
		}
		existingInstance.AnimationName = input.ReadObject<string>();
		existingInstance.SoundName = input.ReadObject<string>();
		return existingInstance;
	}
}
