using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class TrileReader : ContentTypeReader<Trile>
{
	protected override Trile Read(ContentReader input, Trile existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new Trile();
		}
		existingInstance.Name = input.ReadString();
		existingInstance.CubemapPath = input.ReadString();
		existingInstance.Size = input.ReadVector3();
		existingInstance.Offset = input.ReadVector3();
		existingInstance.Immaterial = input.ReadBoolean();
		existingInstance.SeeThrough = input.ReadBoolean();
		existingInstance.Thin = input.ReadBoolean();
		existingInstance.ForceHugging = input.ReadBoolean();
		existingInstance.Faces = input.ReadObject(existingInstance.Faces);
		existingInstance.Geometry = input.ReadObject(existingInstance.Geometry);
		existingInstance.ActorSettings.Type = input.ReadObject<ActorType>();
		existingInstance.ActorSettings.Face = input.ReadObject<FaceOrientation>();
		existingInstance.SurfaceType = input.ReadObject<SurfaceType>();
		existingInstance.AtlasOffset = input.ReadVector2();
		return existingInstance;
	}
}
