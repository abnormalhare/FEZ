using FezEngine.Effects;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure;

public class Group
{
	private Matrix worldMatrix;

	private Matrix translationMatrix;

	private Matrix scalingMatrix;

	private Matrix rotationMatrix;

	private Matrix scalingRotationMatrix;

	private readonly Dirtyable<Matrix?> textureMatrix = new Dirtyable<Matrix?>();

	private Texture texture;

	private Vector3 position;

	private Vector3 scale = Vector3.One;

	private Quaternion rotation = Quaternion.Identity;

	private readonly Dirtyable<Matrix> compositeWorldMatrix = new Dirtyable<Matrix>();

	private readonly Dirtyable<Matrix> inverseTransposeCompositeWorldMatrix = new Dirtyable<Matrix>();

	public int Id { get; set; }

	public Mesh Mesh { get; set; }

	public bool RotateOffCenter { get; set; }

	public bool Enabled { get; set; }

	public Material Material { get; set; }

	public Dirtyable<Matrix?> TextureMatrix
	{
		get
		{
			return textureMatrix;
		}
		set
		{
			textureMatrix.Set(value);
		}
	}

	public bool EffectOwner => Mesh.Groups.Count == 1;

	public CullMode? CullMode { get; set; }

	public bool? AlwaysOnTop { get; set; }

	public BlendingMode? Blending { get; set; }

	public SamplerState SamplerState { get; set; }

	public bool? NoAlphaWrite { get; set; }

	public TexturingType TexturingType { get; private set; }

	public Texture Texture
	{
		get
		{
			return texture;
		}
		set
		{
			texture = value;
			TexturingType = ((value is Texture2D) ? TexturingType.Texture2D : ((value is TextureCube) ? TexturingType.Cubemap : TexturingType.None));
		}
	}

	public Texture2D TextureMap => texture as Texture2D;

	public TextureCube CubeMap => texture as TextureCube;

	public object CustomData { get; set; }

	public IIndexedPrimitiveCollection Geometry { get; set; }

	public Vector3 Position
	{
		get
		{
			return position;
		}
		set
		{
			if (position != value)
			{
				position = value;
				translationMatrix = Matrix.CreateTranslation(position);
				RebuildWorld(invert: false);
			}
		}
	}

	public Vector3 Scale
	{
		get
		{
			return scale;
		}
		set
		{
			if (scale != value)
			{
				scale = value;
				scalingMatrix = Matrix.CreateScale(scale);
				scalingRotationMatrix = scalingMatrix * rotationMatrix;
				RebuildWorld(invert: true);
			}
		}
	}

	public Quaternion Rotation
	{
		get
		{
			return rotation;
		}
		set
		{
			if (rotation != value)
			{
				rotation = value;
				rotationMatrix = Matrix.CreateFromQuaternion(rotation);
				scalingRotationMatrix = scalingMatrix * rotationMatrix;
				RebuildWorld(invert: true);
			}
		}
	}

	public Dirtyable<Matrix> WorldMatrix
	{
		get
		{
			return compositeWorldMatrix;
		}
		set
		{
			worldMatrix = value;
			worldMatrix.Decompose(out scale, out rotation, out position);
			translationMatrix = Matrix.CreateTranslation(position);
			scalingMatrix = Matrix.CreateScale(scale);
			rotationMatrix = Matrix.CreateFromQuaternion(rotation);
			scalingRotationMatrix = scalingMatrix * rotationMatrix;
			RebuildCompositeWorld(invert: true);
		}
	}

	public Dirtyable<Matrix> InverseTransposeWorldMatrix => inverseTransposeCompositeWorldMatrix;

	internal Group(Mesh mesh, int id)
	{
		Id = id;
		Mesh = mesh;
		Enabled = true;
		textureMatrix.Dirty = true;
		translationMatrix = (scalingRotationMatrix = (scalingMatrix = (rotationMatrix = Matrix.Identity)));
		worldMatrix = Matrix.Identity;
		RebuildCompositeWorld();
	}

	public void SetLazyScale(Vector3 scale)
	{
		this.scale = scale;
		scalingMatrix = Matrix.CreateScale(scale);
		scalingRotationMatrix = scalingMatrix * rotationMatrix;
	}

	public void SetLazyRotation(Quaternion r)
	{
		rotation = r;
		rotationMatrix = Matrix.CreateFromQuaternion(rotation);
		scalingRotationMatrix = scalingMatrix * rotationMatrix;
	}

	public void SetLazyPosition(Vector3 position)
	{
		this.position = position;
		translationMatrix = Matrix.CreateTranslation(position);
	}

	public void RecomputeMatrices()
	{
		RebuildWorld(invert: true);
	}

	public void RecomputeMatrices(bool noInvert)
	{
		RebuildWorld(noInvert);
	}

	private void RebuildWorld(bool invert)
	{
		if (RotateOffCenter)
		{
			worldMatrix = translationMatrix * scalingRotationMatrix;
		}
		else
		{
			worldMatrix = scalingRotationMatrix * translationMatrix;
		}
		RebuildCompositeWorld(invert);
	}

	internal void RebuildCompositeWorld()
	{
		RebuildCompositeWorld(invert: true);
	}

	internal void RebuildCompositeWorld(bool invert)
	{
		compositeWorldMatrix.Set(worldMatrix * Mesh.WorldMatrix);
		if (invert)
		{
			inverseTransposeCompositeWorldMatrix.Set(Matrix.Transpose(Matrix.Invert(compositeWorldMatrix)));
		}
	}

	public void Draw(BaseEffect effect)
	{
		if (Geometry == null)
		{
			return;
		}
		GraphicsDevice graphicsDevice = Mesh.GraphicsDevice;
		if (AlwaysOnTop.HasValue)
		{
			graphicsDevice.GetDssCombiner().DepthBufferFunction = ((!AlwaysOnTop.Value) ? CompareFunction.LessEqual : CompareFunction.Always);
		}
		if (Blending.HasValue)
		{
			graphicsDevice.SetBlendingMode(Blending.Value);
		}
		if (SamplerState != null)
		{
			for (int i = 0; i < Mesh.UsedSamplers; i++)
			{
				graphicsDevice.SamplerStates[i] = SamplerState;
			}
		}
		if (CullMode.HasValue)
		{
			graphicsDevice.SetCullMode(CullMode.Value);
		}
		if (NoAlphaWrite.HasValue)
		{
			graphicsDevice.GetBlendCombiner().ColorWriteChannels = ColorWriteChannels.Red | ColorWriteChannels.Green | ColorWriteChannels.Blue;
		}
		effect.Prepare(this);
		graphicsDevice.ApplyCombiners();
		Geometry.Draw(effect);
	}

	internal Group Clone(Mesh mesh)
	{
		return new Group(mesh, mesh.Groups.Count)
		{
			Geometry = Geometry.Clone(),
			Material = ((Material == null) ? null : Material.Clone()),
			Texture = Texture,
			WorldMatrix = worldMatrix,
			SamplerState = SamplerState,
			Blending = Blending,
			AlwaysOnTop = AlwaysOnTop,
			CullMode = CullMode,
			TexturingType = TexturingType,
			TextureMatrix = TextureMatrix,
			RotateOffCenter = RotateOffCenter,
			Enabled = Enabled
		};
	}

	public void BakeTransformInstanced<VertexType, InstanceType>() where VertexType : struct, IShaderInstantiatableVertex, IVertex where InstanceType : struct
	{
		VertexType[] vertices = (Geometry as ShaderInstancedIndexedPrimitives<VertexType, InstanceType>).Vertices;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i].Position = Vector3.Transform(vertices[i].Position, compositeWorldMatrix);
		}
		WorldMatrix = Matrix.Identity;
	}

	public void BakeTransform<T>() where T : struct, IVertex
	{
		T[] vertices = (Geometry as IndexedUserPrimitives<T>).Vertices;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i].Position = Vector3.Transform(vertices[i].Position, compositeWorldMatrix);
		}
		WorldMatrix = Matrix.Identity;
	}

	public void BakeTransformWithNormal<T>() where T : struct, ILitVertex
	{
		T[] vertices = (Geometry as IndexedUserPrimitives<T>).Vertices;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i].Position = Vector3.Transform(vertices[i].Position, compositeWorldMatrix);
			vertices[i].Normal = Vector3.Normalize(Vector3.TransformNormal(vertices[i].Normal, compositeWorldMatrix));
		}
		WorldMatrix = Matrix.Identity;
	}

	public void BakeTransformWithNormalTexture<T>() where T : struct, ILitVertex, ITexturedVertex
	{
		T[] vertices = (Geometry as IndexedUserPrimitives<T>).Vertices;
		Matrix transform = (textureMatrix.Value.HasValue ? textureMatrix.Value.Value : Mesh.TextureMatrix.Value);
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i].Position = Vector3.Transform(vertices[i].Position, compositeWorldMatrix);
			vertices[i].Normal = Vector3.Normalize(Vector3.TransformNormal(vertices[i].Normal, compositeWorldMatrix));
			vertices[i].TextureCoordinate = FezMath.TransformTexCoord(vertices[i].TextureCoordinate, transform);
		}
		TextureMatrix.Set(null);
		WorldMatrix = Matrix.Identity;
	}

	public void InvertNormals<T>() where T : struct, ILitVertex
	{
		T[] vertices = (Geometry as IndexedUserPrimitives<T>).Vertices;
		for (int i = 0; i < vertices.Length; i++)
		{
			vertices[i].Normal = -vertices[i].Normal;
		}
	}
}
