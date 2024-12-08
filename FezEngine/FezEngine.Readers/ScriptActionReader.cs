using FezEngine.Structure.Scripting;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class ScriptActionReader : ContentTypeReader<ScriptAction>
{
	protected override ScriptAction Read(ContentReader input, ScriptAction existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new ScriptAction();
		}
		existingInstance.Object = input.ReadObject(existingInstance.Object);
		existingInstance.Operation = input.ReadString();
		existingInstance.Arguments = input.ReadObject(existingInstance.Arguments);
		existingInstance.Killswitch = input.ReadBoolean();
		existingInstance.Blocking = input.ReadBoolean();
		existingInstance.OnDeserialization();
		return existingInstance;
	}
}
