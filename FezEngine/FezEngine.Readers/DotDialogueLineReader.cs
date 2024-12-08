using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class DotDialogueLineReader : ContentTypeReader<DotDialogueLine>
{
	protected override DotDialogueLine Read(ContentReader input, DotDialogueLine existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new DotDialogueLine();
		}
		existingInstance.ResourceText = input.ReadObject<string>();
		existingInstance.Grouped = input.ReadBoolean();
		return existingInstance;
	}
}
