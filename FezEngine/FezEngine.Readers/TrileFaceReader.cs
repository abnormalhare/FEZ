using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class TrileFaceReader : ContentTypeReader<TrileFace>
{
	protected override TrileFace Read(ContentReader input, TrileFace existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new TrileFace();
		}
		existingInstance.Id = input.ReadObject<TrileEmplacement>();
		existingInstance.Face = input.ReadObject<FaceOrientation>();
		return existingInstance;
	}
}
