using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class ArtObjectReader : ContentTypeReader<ArtObject>
{
	protected override ArtObject Read(ContentReader input, ArtObject existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new ArtObject();
		}
		existingInstance.Name = input.ReadString();
		FutureTexture2D futureCubemap = input.ReadObject<FutureTexture2D>(FutureTexture2DReader.Instance);
		DrawActionScheduler.Schedule(delegate
		{
			existingInstance.Cubemap = futureCubemap.Create();
		});
		existingInstance.Size = input.ReadVector3();
		existingInstance.Geometry = input.ReadObject(existingInstance.Geometry);
		existingInstance.ActorType = input.ReadObject<ActorType>();
		existingInstance.NoSihouette = input.ReadBoolean();
		return existingInstance;
	}
}
