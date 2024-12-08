using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Effects.Structures;

internal class MatricesEffectStructure
{
	private readonly SemanticMappedMatrix worldViewProjection;

	private readonly SemanticMappedMatrix worldInverseTranspose;

	private readonly SemanticMappedMatrix world;

	private readonly SemanticMappedMatrix textureMatrix;

	private readonly SemanticMappedMatrix viewProjection;

	public Matrix WorldViewProjection
	{
		set
		{
			worldViewProjection.Set(value);
		}
	}

	public Matrix WorldInverseTranspose
	{
		set
		{
			worldInverseTranspose.Set(value);
		}
	}

	public Matrix ViewProjection
	{
		set
		{
			viewProjection.Set(value);
		}
	}

	public Matrix World
	{
		set
		{
			world.Set(value);
		}
	}

	public Matrix TextureMatrix
	{
		set
		{
			textureMatrix.Set(value);
		}
	}

	public MatricesEffectStructure(EffectParameterCollection parameters)
	{
		worldViewProjection = new SemanticMappedMatrix(parameters, "Matrices_WorldViewProjection");
		worldInverseTranspose = new SemanticMappedMatrix(parameters, "Matrices_WorldInverseTranspose");
		world = new SemanticMappedMatrix(parameters, "Matrices_World");
		textureMatrix = new SemanticMappedMatrix(parameters, "Matrices_Texture");
		viewProjection = new SemanticMappedMatrix(parameters, "Matrices_ViewProjection");
	}
}
