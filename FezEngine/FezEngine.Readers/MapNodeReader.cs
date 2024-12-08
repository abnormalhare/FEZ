using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class MapNodeReader : ContentTypeReader<MapNode>
{
	protected override MapNode Read(ContentReader input, MapNode existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new MapNode();
		}
		existingInstance.LevelName = input.ReadString();
		existingInstance.Connections = input.ReadObject(existingInstance.Connections);
		existingInstance.NodeType = input.ReadObject<LevelNodeType>();
		existingInstance.Conditions = input.ReadObject(existingInstance.Conditions);
		existingInstance.HasLesserGate = input.ReadBoolean();
		existingInstance.HasWarpGate = input.ReadBoolean();
		return existingInstance;
	}
}
