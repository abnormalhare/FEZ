using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class TrileEffect : BaseEffect, IShaderInstantiatableEffect<Vector4>
{
	private readonly SemanticMappedTexture textureAtlas;

	private readonly SemanticMappedBoolean blink;

	private readonly SemanticMappedBoolean unstable;

	private readonly SemanticMappedBoolean tiltTwoAxis;

	private readonly SemanticMappedBoolean shiny;

	private readonly SemanticMappedVectorArray instanceData;

	private readonly bool InEditor;

	private static readonly TrileCustomData DefaultCustom = new TrileCustomData();

	private bool lastWasCustom;

	public LightingEffectPass Pass
	{
		set
		{
			currentPass = currentTechnique.Passes[(value != 0) ? 1 : 0];
		}
	}

	public bool Blink
	{
		set
		{
			blink.Set(value);
		}
	}

	public TrileEffect()
		: base(BaseEffect.UseHardwareInstancing ? "HwTrileEffect" : "TrileEffect")
	{
		textureAtlas = new SemanticMappedTexture(effect.Parameters, "AtlasTexture");
		blink = new SemanticMappedBoolean(effect.Parameters, "Blink");
		unstable = new SemanticMappedBoolean(effect.Parameters, "Unstable");
		tiltTwoAxis = new SemanticMappedBoolean(effect.Parameters, "TiltTwoAxis");
		shiny = new SemanticMappedBoolean(effect.Parameters, "Shiny");
		if (!BaseEffect.UseHardwareInstancing)
		{
			instanceData = new SemanticMappedVectorArray(effect.Parameters, "InstanceData");
		}
		InEditor = base.EngineState.InEditor;
		Pass = LightingEffectPass.Main;
		SimpleGroupPrepare = true;
		material.Opacity = 1f;
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		textureAtlas.Set(mesh.Texture);
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		if (InEditor)
		{
			textureAtlas.Set(group.Texture);
		}
		TrileCustomData trileCustomData = (group.CustomData as TrileCustomData) ?? DefaultCustom;
		bool isCustom = trileCustomData.IsCustom;
		if (lastWasCustom || isCustom)
		{
			unstable.Set(trileCustomData.Unstable);
			shiny.Set(trileCustomData.Shiny);
			tiltTwoAxis.Set(trileCustomData.TiltTwoAxis);
			lastWasCustom = isCustom;
		}
	}

	public void SetInstanceData(Vector4[] instances, int start, int batchInstanceCount)
	{
		instanceData.Set(instances, start, batchInstanceCount);
	}
}
