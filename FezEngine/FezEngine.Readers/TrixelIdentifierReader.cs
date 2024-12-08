using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public static class TrixelIdentifierReader
{
	public static TrixelEmplacement ReadTrixelIdentifier(this ContentReader input)
	{
		TrixelEmplacement result = default(TrixelEmplacement);
		result.X = input.ReadInt32();
		result.Y = input.ReadInt32();
		result.Z = input.ReadInt32();
		return result;
	}
}
