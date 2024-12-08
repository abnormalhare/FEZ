using ContentSerialization.Attributes;

namespace FezEngine.Structure.Scripting;

public class Entity
{
	public string Type { get; set; }

	[Serialization(Optional = true)]
	public int? Identifier { get; set; }

	public override string ToString()
	{
		if (!Identifier.HasValue)
		{
			return Type;
		}
		return Type + "[" + Identifier + "]";
	}

	public Entity Clone()
	{
		return new Entity
		{
			Type = Type,
			Identifier = Identifier
		};
	}
}
