using FezEngine.Effects.Structures;
using FezEngine.Structure;

namespace FezEngine.Effects;

public class FoamEffect : BaseEffect
{
	private readonly SemanticMappedSingle timeAccumulator;

	private readonly SemanticMappedSingle shoreTotalWidth;

	private readonly SemanticMappedSingle screenCenterSide;

	private readonly SemanticMappedBoolean isEmerged;

	private readonly SemanticMappedBoolean isWobbling;

	public float TimeAccumulator
	{
		set
		{
			timeAccumulator.Set(value);
		}
	}

	public float ShoreTotalWidth
	{
		set
		{
			shoreTotalWidth.Set(value);
		}
	}

	public float ScreenCenterSide
	{
		set
		{
			screenCenterSide.Set(value);
		}
	}

	public bool IsWobbling
	{
		set
		{
			isWobbling.Set(value);
		}
	}

	public FoamEffect()
		: base("FoamEffect")
	{
		timeAccumulator = new SemanticMappedSingle(effect.Parameters, "TimeAccumulator");
		shoreTotalWidth = new SemanticMappedSingle(effect.Parameters, "ShoreTotalWidth");
		screenCenterSide = new SemanticMappedSingle(effect.Parameters, "ScreenCenterSide");
		isEmerged = new SemanticMappedBoolean(effect.Parameters, "IsEmerged");
		isWobbling = new SemanticMappedBoolean(effect.Parameters, "IsWobbling");
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		isEmerged.Set((bool)group.CustomData);
		material.Diffuse = group.Material.Diffuse;
		material.Opacity = group.Material.Opacity;
	}
}
