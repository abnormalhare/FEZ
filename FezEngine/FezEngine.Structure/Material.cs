using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class Material
{
	public Vector3 Diffuse;

	public float Opacity;

	public Material()
	{
		Diffuse = Vector3.One;
		Opacity = 1f;
	}

	public Material Clone()
	{
		return new Material
		{
			Diffuse = Diffuse,
			Opacity = Opacity
		};
	}
}
