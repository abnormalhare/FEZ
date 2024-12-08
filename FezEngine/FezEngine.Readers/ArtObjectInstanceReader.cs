using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class ArtObjectInstanceReader : ContentTypeReader<ArtObjectInstance>
{
	protected override ArtObjectInstance Read(ContentReader input, ArtObjectInstance existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new ArtObjectInstance(input.ReadString());
		}
		existingInstance.Position = input.ReadVector3();
		existingInstance.Rotation = input.ReadQuaternion();
		existingInstance.Scale = input.ReadVector3();
		existingInstance.ActorSettings = input.ReadObject(existingInstance.ActorSettings);
		return existingInstance;
	}
}
