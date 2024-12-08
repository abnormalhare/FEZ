using ContentSerialization.Attributes;

namespace FezEngine.Structure;

public class SpeechLine
{
	public string Text { get; set; }

	[Serialization(Optional = true)]
	public NpcActionContent OverrideContent { get; set; }

	public SpeechLine Clone()
	{
		return new SpeechLine
		{
			Text = Text,
			OverrideContent = ((OverrideContent == null) ? null : OverrideContent.Clone())
		};
	}
}
