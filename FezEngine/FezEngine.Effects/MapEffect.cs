using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects;

public class MapEffect : BaseEffect
{
	private bool lastWasComplete;

	private Vector3 lastDiffuse;

	public MapEffect()
		: base("MapEffect")
	{
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
	}
}
