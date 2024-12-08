using FezEngine.Effects.Structures;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects;

public class ProjectedNodeEffect : BaseEffect
{
	private readonly SemanticMappedTexture texture;

	private readonly SemanticMappedVector2 textureSize;

	private readonly SemanticMappedVector2 viewportSize;

	private readonly SemanticMappedVector3 cubeOffset;

	private readonly SemanticMappedSingle pixPerTrix;

	private readonly SemanticMappedBoolean noTexture;

	private readonly SemanticMappedBoolean complete;

	private Vector3 lastDiffuse;

	private bool lastWasComplete;

	public LightingEffectPass Pass
	{
		set
		{
			currentPass = currentTechnique.Passes[(value == LightingEffectPass.Pre) ? 1 : 0];
		}
	}

	public ProjectedNodeEffect()
		: base("ProjectedNodeEffect")
	{
		texture = new SemanticMappedTexture(effect.Parameters, "BaseTexture");
		textureSize = new SemanticMappedVector2(effect.Parameters, "TextureSize");
		viewportSize = new SemanticMappedVector2(effect.Parameters, "ViewportSize");
		cubeOffset = new SemanticMappedVector3(effect.Parameters, "CubeOffset");
		pixPerTrix = new SemanticMappedSingle(effect.Parameters, "PixelsPerTrixel");
		noTexture = new SemanticMappedBoolean(effect.Parameters, "NoTexture");
		complete = new SemanticMappedBoolean(effect.Parameters, "Complete");
		Pass = LightingEffectPass.Main;
	}

	public override BaseEffect Clone()
	{
		return new ProjectedNodeEffect();
	}

	public override void Prepare(Mesh mesh)
	{
		base.Prepare(mesh);
		float viewScale = base.GraphicsDeviceService.GraphicsDevice.GetViewScale();
		float num = (float)base.GraphicsDeviceService.GraphicsDevice.Viewport.Width / (1280f * viewScale);
		float num2 = (float)base.GraphicsDeviceService.GraphicsDevice.Viewport.Height / (720f * viewScale);
		_ = base.GraphicsDeviceService.GraphicsDevice.Viewport;
		matrices.ViewProjection = viewProjection;
		pixPerTrix.Set(base.CameraProvider.Radius / num / 45f * 18f / viewScale);
		viewportSize.Set(new Vector2(1280f * num, 720f * num2));
	}

	public override void Prepare(Group group)
	{
		base.Prepare(group);
		cubeOffset.Set(Vector3.Transform(Vector3.Zero, group.WorldMatrix));
		if (IgnoreCache || !group.EffectOwner || group.InverseTransposeWorldMatrix.Dirty)
		{
			matrices.WorldInverseTranspose = group.InverseTransposeWorldMatrix;
			group.InverseTransposeWorldMatrix.Clean();
		}
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
		if (group.Material != null)
		{
			material.Opacity = group.Mesh.Material.Opacity * group.Material.Opacity;
		}
		noTexture.Set(group.TexturingType != TexturingType.Texture2D);
		bool flag = (group.CustomData as NodeGroupData).Complete;
		complete.Set(flag);
		if (flag)
		{
			base.GraphicsDeviceService.GraphicsDevice.PrepareStencilWrite(StencilMask.Trails);
			lastWasComplete = true;
		}
		else if (lastWasComplete)
		{
			base.GraphicsDeviceService.GraphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
		}
		if (group.TexturingType == TexturingType.Texture2D)
		{
			texture.Set(group.Texture);
			textureSize.Set(new Vector2(group.TextureMap.Width, group.TextureMap.Height));
		}
	}
}
