using FezEngine.Structure.Scripting;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class ScriptConditionReader : ContentTypeReader<ScriptCondition>
{
	protected override ScriptCondition Read(ContentReader input, ScriptCondition existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new ScriptCondition();
		}
		existingInstance.Object = input.ReadObject(existingInstance.Object);
		existingInstance.Operator = input.ReadObject<ComparisonOperator>();
		existingInstance.Property = input.ReadString();
		existingInstance.Value = input.ReadString();
		existingInstance.OnDeserialization();
		return existingInstance;
	}
}
