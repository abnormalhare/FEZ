using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class MovementPathReader : ContentTypeReader<MovementPath>
{
	protected override MovementPath Read(ContentReader input, MovementPath existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new MovementPath();
		}
		existingInstance.Segments = input.ReadObject(existingInstance.Segments);
		existingInstance.NeedsTrigger = input.ReadBoolean();
		existingInstance.EndBehavior = input.ReadObject<PathEndBehavior>();
		existingInstance.SoundName = input.ReadObject<string>();
		existingInstance.IsSpline = input.ReadBoolean();
		existingInstance.OffsetSeconds = input.ReadSingle();
		existingInstance.SaveTrigger = input.ReadBoolean();
		return existingInstance;
	}
}
