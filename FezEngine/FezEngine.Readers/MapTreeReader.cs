using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class MapTreeReader : ContentTypeReader<MapTree>
{
	protected override MapTree Read(ContentReader input, MapTree existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new MapTree();
		}
		existingInstance.Root = input.ReadObject(existingInstance.Root);
		return existingInstance;
	}
}
