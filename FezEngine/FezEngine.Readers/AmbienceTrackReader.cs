using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class AmbienceTrackReader : ContentTypeReader<AmbienceTrack>
{
	protected override AmbienceTrack Read(ContentReader input, AmbienceTrack existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new AmbienceTrack();
		}
		existingInstance.Name = input.ReadObject<string>();
		existingInstance.Dawn = input.ReadBoolean();
		existingInstance.Day = input.ReadBoolean();
		existingInstance.Dusk = input.ReadBoolean();
		existingInstance.Night = input.ReadBoolean();
		return existingInstance;
	}
}
