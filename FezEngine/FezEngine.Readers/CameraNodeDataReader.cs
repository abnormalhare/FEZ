using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class CameraNodeDataReader : ContentTypeReader<CameraNodeData>
{
	protected override CameraNodeData Read(ContentReader input, CameraNodeData existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new CameraNodeData();
		}
		existingInstance.Perspective = input.ReadBoolean();
		existingInstance.PixelsPerTrixel = input.ReadInt32();
		existingInstance.SoundName = input.ReadObject<string>();
		return existingInstance;
	}
}
