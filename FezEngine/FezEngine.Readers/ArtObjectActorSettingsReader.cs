using System.Collections.Generic;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class ArtObjectActorSettingsReader : ContentTypeReader<ArtObjectActorSettings>
{
	protected override ArtObjectActorSettings Read(ContentReader input, ArtObjectActorSettings existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new ArtObjectActorSettings();
		}
		existingInstance.Inactive = input.ReadBoolean();
		existingInstance.ContainedTrile = input.ReadObject<ActorType>();
		existingInstance.AttachedGroup = input.ReadObject<int?>();
		existingInstance.SpinView = input.ReadObject<Viewpoint>();
		existingInstance.SpinEvery = input.ReadSingle();
		existingInstance.SpinOffset = input.ReadSingle();
		existingInstance.OffCenter = input.ReadBoolean();
		existingInstance.RotationCenter = input.ReadVector3();
		existingInstance.VibrationPattern = input.ReadObject<VibrationMotor[]>();
		existingInstance.CodePattern = input.ReadObject<CodeInput[]>();
		existingInstance.Segment = input.ReadObject<PathSegment>();
		existingInstance.NextNode = input.ReadObject<int?>();
		existingInstance.DestinationLevel = input.ReadObject<string>();
		existingInstance.TreasureMapName = input.ReadObject<string>();
		existingInstance.InvisibleSides = new HashSet<FaceOrientation>(input.ReadObject<FaceOrientation[]>(), FaceOrientationComparer.Default);
		existingInstance.TimeswitchWindBackSpeed = input.ReadSingle();
		return existingInstance;
	}
}
