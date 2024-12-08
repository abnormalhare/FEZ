using FezEngine.Effects.Structures;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects;

public class InstancedMapEffect : BaseEffect, IShaderInstantiatableEffect<Matrix>
{
	private bool lastWasComplete;

	private readonly SemanticMappedBoolean billboard;

	private readonly SemanticMappedMatrix cameraRotation;

	private readonly SemanticMappedTexture texture;

	private readonly SemanticMappedMatrixArray instanceData;

	public bool Billboard
	{
		set
		{
			billboard.Set(value);
		}
	}

	public InstancedMapEffect()
		: base(BaseEffect.UseHardwareInstancing ? "HwInstancedMapEffect" : "InstancedMapEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		billboard = new SemanticMappedBoolean(effect.Parameters, "Billboard");
		cameraRotation = new SemanticMappedMatrix(effect.Parameters, "CameraRotation");
		if (!BaseEffect.UseHardwareInstancing)
		{
			instanceData = new SemanticMappedMatrixArray(effect.Parameters, "InstanceData");
		}
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		texture.Set(mesh.Texture);
		cameraRotation.Set(Matrix.CreateFromQuaternion(base.CameraProvider.Rotation));
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		if (group.CustomData != null && (bool)group.CustomData)
		{
			base.GraphicsDeviceService.GraphicsDevice.PrepareStencilWrite(StencilMask.Trails);
			lastWasComplete = true;
		}
		else if (lastWasComplete)
		{
			base.GraphicsDeviceService.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		}
	}

	public void SetInstanceData(Matrix[] instances, int start, int batchInstanceCount)
	{
		instanceData.Set(instances, start, batchInstanceCount);
	}
}
