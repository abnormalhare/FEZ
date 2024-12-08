using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class MatrixEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	private readonly SemanticMappedSingle maxHeight;

	private bool groupTextureDirty;

	private Vector3 lastDiffuse;

	public float MaxHeight
	{
		set
		{
			maxHeight.Set(value);
		}
	}

	public MatrixEffect()
		: base("MatrixEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		maxHeight = new SemanticMappedSingle(effect.Parameters, "MaxHeight");
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		texture.Set(mesh.Texture);
		groupTextureDirty = false;
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		matrices.World = group.WorldMatrix;
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
		if (group.TexturingType == TexturingType.Texture2D)
		{
			texture.Set(group.Texture);
			groupTextureDirty = true;
		}
		else if (groupTextureDirty)
		{
			texture.Set(group.Mesh.Texture);
		}
	}
}
