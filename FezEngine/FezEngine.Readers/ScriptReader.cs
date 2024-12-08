using System;
using FezEngine.Structure.Scripting;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class ScriptReader : ContentTypeReader<Script>
{
	protected override Script Read(ContentReader input, Script existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new Script();
		}
		existingInstance.Name = input.ReadString();
		existingInstance.Timeout = input.ReadObject<TimeSpan?>();
		existingInstance.Triggers = input.ReadObject(existingInstance.Triggers);
		existingInstance.Conditions = input.ReadObject(existingInstance.Conditions);
		existingInstance.Actions = input.ReadObject(existingInstance.Actions);
		existingInstance.OneTime = input.ReadBoolean();
		existingInstance.Triggerless = input.ReadBoolean();
		existingInstance.IgnoreEndTriggers = input.ReadBoolean();
		existingInstance.LevelWideOneTime = input.ReadBoolean();
		existingInstance.Disabled = input.ReadBoolean();
		existingInstance.IsWinCondition = input.ReadBoolean();
		return existingInstance;
	}
}
