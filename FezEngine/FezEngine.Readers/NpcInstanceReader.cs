using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class NpcInstanceReader : ContentTypeReader<NpcInstance>
{
	protected override NpcInstance Read(ContentReader input, NpcInstance existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new NpcInstance();
		}
		existingInstance.Name = input.ReadString();
		existingInstance.Position = input.ReadVector3();
		existingInstance.DestinationOffset = input.ReadVector3();
		existingInstance.WalkSpeed = input.ReadSingle();
		existingInstance.RandomizeSpeech = input.ReadBoolean();
		existingInstance.SayFirstSpeechLineOnce = input.ReadBoolean();
		existingInstance.AvoidsGomez = input.ReadBoolean();
		existingInstance.ActorType = input.ReadObject<ActorType>();
		existingInstance.Speech = input.ReadObject(existingInstance.Speech);
		existingInstance.Actions = input.ReadObject(existingInstance.Actions);
		return existingInstance;
	}
}
