using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class SkyLayerReader : ContentTypeReader<SkyLayer>
{
	protected override SkyLayer Read(ContentReader input, SkyLayer existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new SkyLayer();
		}
		existingInstance.Name = input.ReadString();
		existingInstance.InFront = input.ReadBoolean();
		existingInstance.Opacity = input.ReadSingle();
		existingInstance.FogTint = input.ReadSingle();
		return existingInstance;
	}
}
