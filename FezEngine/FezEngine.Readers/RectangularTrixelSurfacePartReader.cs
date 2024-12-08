using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class RectangularTrixelSurfacePartReader : ContentTypeReader<RectangularTrixelSurfacePart>
{
	protected override RectangularTrixelSurfacePart Read(ContentReader input, RectangularTrixelSurfacePart existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new RectangularTrixelSurfacePart();
		}
		existingInstance.Start = input.ReadTrixelIdentifier();
		existingInstance.Orientation = input.ReadObject<FaceOrientation>();
		existingInstance.TangentSize = input.ReadInt32();
		existingInstance.BitangentSize = input.ReadInt32();
		return existingInstance;
	}
}
