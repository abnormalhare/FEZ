using FezEngine.Structure.Scripting;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class ScriptTriggerReader : ContentTypeReader<ScriptTrigger>
{
	protected override ScriptTrigger Read(ContentReader input, ScriptTrigger existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new ScriptTrigger();
		}
		existingInstance.Object = input.ReadObject(existingInstance.Object);
		existingInstance.Event = input.ReadString();
		return existingInstance;
	}
}
