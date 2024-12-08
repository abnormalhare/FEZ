using FezEngine.Structure.Scripting;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class EntityReader : ContentTypeReader<Entity>
{
	protected override Entity Read(ContentReader input, Entity existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new Entity();
		}
		existingInstance.Type = input.ReadString();
		existingInstance.Identifier = input.ReadObject<int?>();
		return existingInstance;
	}
}
