using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class SpeechLineReader : ContentTypeReader<SpeechLine>
{
	protected override SpeechLine Read(ContentReader input, SpeechLine existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new SpeechLine();
		}
		existingInstance.Text = input.ReadObject<string>();
		existingInstance.OverrideContent = input.ReadObject(existingInstance.OverrideContent);
		return existingInstance;
	}
}
