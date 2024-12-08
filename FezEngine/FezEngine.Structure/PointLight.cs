using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public struct PointLight
{
	public Vector3 Position { get; set; }

	public float LinearAttenuation { get; set; }

	public float QuadraticAttenuation { get; set; }

	public bool Spot { get; set; }

	public float Theta { get; set; }

	public float Phi { get; set; }

	public float Falloff { get; set; }

	public Vector3 Diffuse { get; set; }

	public Vector3 Specular { get; set; }

	public void Initialize()
	{
		Vector3 diffuse = (Specular = Vector3.One);
		Diffuse = diffuse;
	}
}
