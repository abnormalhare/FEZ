using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class TrileInstanceReader : ContentTypeReader<TrileInstance>
{
	protected override TrileInstance Read(ContentReader input, TrileInstance existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new TrileInstance();
		}
		existingInstance.Position = input.ReadVector3();
		existingInstance.TrileId = input.ReadInt32();
		byte phiLight = input.ReadByte();
		existingInstance.SetPhiLight(phiLight);
		if (input.ReadBoolean())
		{
			existingInstance.ActorSettings = input.ReadObject(existingInstance.ActorSettings);
		}
		existingInstance.OverlappedTriles = input.ReadObject(existingInstance.OverlappedTriles);
		return existingInstance;
	}
}
