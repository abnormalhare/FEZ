using FezEngine.Structure;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class VolumeActorSettingsReader : ContentTypeReader<VolumeActorSettings>
{
	protected override VolumeActorSettings Read(ContentReader input, VolumeActorSettings existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new VolumeActorSettings();
		}
		existingInstance.FarawayPlaneOffset = input.ReadVector2();
		existingInstance.IsPointOfInterest = input.ReadBoolean();
		existingInstance.DotDialogue = input.ReadObject(existingInstance.DotDialogue);
		existingInstance.WaterLocked = input.ReadBoolean();
		existingInstance.CodePattern = input.ReadObject<CodeInput[]>();
		existingInstance.IsBlackHole = input.ReadBoolean();
		existingInstance.NeedsTrigger = input.ReadBoolean();
		existingInstance.IsSecretPassage = input.ReadBoolean();
		return existingInstance;
	}
}
