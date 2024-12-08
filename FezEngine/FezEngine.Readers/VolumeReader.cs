using System.Collections.Generic;
using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class VolumeReader : ContentTypeReader<Volume>
{
	protected override Volume Read(ContentReader input, Volume existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new Volume();
		}
		existingInstance.Orientations = new HashSet<FaceOrientation>(input.ReadObject<FaceOrientation[]>(), FaceOrientationComparer.Default);
		existingInstance.From = input.ReadVector3();
		existingInstance.To = input.ReadVector3();
		existingInstance.ActorSettings = input.ReadObject(existingInstance.ActorSettings);
		return existingInstance;
	}
}
