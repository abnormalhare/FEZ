using FezEngine.Effects.Structures;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Effects;

public class InstancedBlackHoleEffect : BaseEffect, IShaderInstantiatableEffect<Matrix>
{
	private readonly SemanticMappedTexture baseTexture;

	private readonly SemanticMappedBoolean isTextureEnabled;

	private readonly SemanticMappedMatrixArray instanceData;

	public InstancedBlackHoleEffect(bool body)
		: base("InstancedBlackHoleEffect")
	{
		baseTexture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		isTextureEnabled = new SemanticMappedBoolean(effect.Parameters, "IsTextureEnabled");
		instanceData = new SemanticMappedMatrixArray(effect.Parameters, "InstanceData");
		currentPass = (body ? currentTechnique.Passes["Body"] : currentTechnique.Passes["Fringe"]);
		SimpleGroupPrepare = (SimpleMeshPrepare = true);
	}

	public override void Prepare(Mesh mesh)
	{
		matrices.WorldViewProjection = viewProjection;
		isTextureEnabled.Set(mesh.TexturingType == TexturingType.Texture2D);
		baseTexture.Set(mesh.Texture);
		base.Prepare(mesh);
	}

	public void SetInstanceData(Matrix[] instances, int start, int batchInstanceCount)
	{
		instanceData.Set(instances, start, batchInstanceCount);
	}
}
