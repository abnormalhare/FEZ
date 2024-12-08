using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class AnimatedPlaneEffect : BaseEffect
{
	private readonly SemanticMappedTexture animatedTexture;

	private readonly SemanticMappedBoolean ignoreFog;

	private readonly SemanticMappedBoolean fullbright;

	private readonly SemanticMappedBoolean alphaIsEmissive;

	private readonly SemanticMappedBoolean ignoreShading;

	private readonly SemanticMappedBoolean sewerHax;

	public LightingEffectPass Pass
	{
		set
		{
			currentPass = currentTechnique.Passes[(value != 0) ? 1 : 0];
		}
	}

	public bool IgnoreFog
	{
		set
		{
			ignoreFog.Set(value);
		}
	}

	public bool IgnoreShading
	{
		set
		{
			ignoreShading.Set(value);
		}
	}

	public AnimatedPlaneEffect()
		: base("AnimatedPlaneEffect")
	{
		animatedTexture = new SemanticMappedTexture(effect.Parameters, "AnimatedTexture");
		ignoreFog = new SemanticMappedBoolean(effect.Parameters, "IgnoreFog");
		fullbright = new SemanticMappedBoolean(effect.Parameters, "Fullbright");
		alphaIsEmissive = new SemanticMappedBoolean(effect.Parameters, "AlphaIsEmissive");
		ignoreShading = new SemanticMappedBoolean(effect.Parameters, "IgnoreShading");
		sewerHax = new SemanticMappedBoolean(effect.Parameters, "SewerHax");
		Pass = LightingEffectPass.Main;
	}

	public override void Prepare(Mesh mesh)
	{
		sewerHax.Set(base.LevelManager.WaterType == LiquidType.Sewer);
		base.Prepare(mesh);
		if (base.ForcedViewMatrix.HasValue && base.ForcedProjectionMatrix.HasValue)
		{
			matrices.ViewProjection = base.ForcedViewMatrix.Value * base.ForcedProjectionMatrix.Value;
		}
	}

	public override void Prepare(Group group)
	{
		if (IgnoreCache || !group.EffectOwner || group.InverseTransposeWorldMatrix.Dirty)
		{
			matrices.WorldInverseTranspose = group.InverseTransposeWorldMatrix;
			group.InverseTransposeWorldMatrix.Clean();
		}
		matrices.World = group.WorldMatrix;
		if (group.TextureMatrix.Value.HasValue)
		{
			matrices.TextureMatrix = group.TextureMatrix.Value.Value;
			textureMatrixDirty = true;
		}
		else if (textureMatrixDirty)
		{
			matrices.TextureMatrix = Matrix.Identity;
			textureMatrixDirty = false;
		}
		animatedTexture.Set(group.Texture);
		material.Diffuse = group.Material.Diffuse;
		material.Opacity = group.Material.Opacity;
		if (group.CustomData is PlaneCustomData planeCustomData)
		{
			fullbright.Set(planeCustomData.Fullbright);
			alphaIsEmissive.Set(planeCustomData.AlphaIsEmissive);
		}
		else
		{
			fullbright.Set(value: false);
			alphaIsEmissive.Set(value: false);
		}
	}
}
