using System;
using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class DotEffect : BaseEffect
{
	private readonly SemanticMappedSingle HueOffset;

	private float hueOffset;

	private Vector3 lastDiffuse;

	public float ShiftSpeed { get; set; }

	public float AdditionalOffset { get; set; }

	public DotEffect()
		: base("DotEffect")
	{
		HueOffset = new SemanticMappedSingle(effect.Parameters, "HueOffset");
		ShiftSpeed = 1f;
	}

	public void UpdateHueOffset(TimeSpan elapsed)
	{
		hueOffset += 0.05f * ShiftSpeed * (float)elapsed.TotalSeconds;
		float num;
		for (num = hueOffset + AdditionalOffset * 360f; num >= 360f; num -= 360f)
		{
		}
		for (; num < 0f; num += 360f)
		{
		}
		HueOffset.Set(num);
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		if (group.Material != null)
		{
			if (lastDiffuse != group.Material.Diffuse)
			{
				material.Diffuse = group.Material.Diffuse;
				lastDiffuse = group.Material.Diffuse;
			}
		}
		else
		{
			material.Diffuse = group.Mesh.Material.Diffuse;
		}
	}
}
