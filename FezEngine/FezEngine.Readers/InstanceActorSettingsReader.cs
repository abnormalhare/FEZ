using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class InstanceActorSettingsReader : ContentTypeReader<InstanceActorSettings>
{
	protected override InstanceActorSettings Read(ContentReader input, InstanceActorSettings existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new InstanceActorSettings();
		}
		existingInstance.ContainedTrile = input.ReadObject<int?>();
		existingInstance.SignText = input.ReadObject(existingInstance.SignText);
		existingInstance.Sequence = input.ReadObject(existingInstance.Sequence);
		existingInstance.SequenceSampleName = input.ReadObject(existingInstance.SequenceSampleName);
		existingInstance.SequenceAlternateSampleName = input.ReadObject(existingInstance.SequenceAlternateSampleName);
		existingInstance.HostVolume = input.ReadObject<int?>();
		return existingInstance;
	}
}
