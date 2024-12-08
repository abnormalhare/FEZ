using ContentSerialization.Attributes;

namespace FezEngine.Structure;

public class SkyLayer
{
	[Serialization(Optional = true)]
	public string Name { get; set; }

	[Serialization(Optional = true)]
	public bool InFront { get; set; }

	[Serialization(Optional = true)]
	public float Opacity { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public float FogTint { get; set; }

	public SkyLayer()
	{
		Opacity = 1f;
	}

	public SkyLayer ShallowCopy()
	{
		return new SkyLayer
		{
			Name = Name,
			InFront = InFront,
			Opacity = Opacity,
			FogTint = FogTint
		};
	}

	public void UpdateFromCopy(SkyLayer copy)
	{
		Name = copy.Name;
		InFront = copy.InFront;
		Opacity = copy.Opacity;
		FogTint = copy.FogTint;
	}
}
