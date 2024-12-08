using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class NpcMetadataReader : ContentTypeReader<NpcMetadata>
{
	protected override NpcMetadata Read(ContentReader input, NpcMetadata existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new NpcMetadata();
		}
		existingInstance.WalkSpeed = input.ReadSingle();
		existingInstance.AvoidsGomez = input.ReadBoolean();
		existingInstance.SoundPath = input.ReadObject<string>();
		existingInstance.SoundActions = input.ReadObject(existingInstance.SoundActions);
		return existingInstance;
	}
}
