using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class MapNodeConnectionReader : ContentTypeReader<MapNode.Connection>
{
	protected override MapNode.Connection Read(ContentReader input, MapNode.Connection existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new MapNode.Connection();
		}
		existingInstance.Face = input.ReadObject<FaceOrientation>();
		existingInstance.Node = input.ReadObject(existingInstance.Node);
		existingInstance.BranchOversize = input.ReadSingle();
		return existingInstance;
	}
}
