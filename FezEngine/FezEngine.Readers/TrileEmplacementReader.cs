using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class TrileEmplacementReader : ContentTypeReader<TrileEmplacement>
{
	protected override TrileEmplacement Read(ContentReader input, TrileEmplacement existingInstance)
	{
		TrileEmplacement result = default(TrileEmplacement);
		result.X = input.ReadInt32();
		result.Y = input.ReadInt32();
		result.Z = input.ReadInt32();
		return result;
	}
}
