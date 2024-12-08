using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine.Effects;
using FezEngine.Structure.Geometry;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure;

public class Mesh : IDisposable
{
	public delegate void RenderingHandler(Mesh mesh, BaseEffect effect);

	private static readonly Comparison<Group> DefaultOrder = (Group a, Group b) => 0;

	private Matrix worldMatrix;

	private Vector3 position;

	private Vector3 scale;

	private Quaternion rotation = Quaternion.Identity;

	private bool groupsDirty;

	private TexturingType texturingType;

	private readonly Dirtyable<Texture> texture = new Dirtyable<Texture>();

	private readonly Dirtyable<Matrix> textureMatrix = new Dirtyable<Matrix>();

	private Comparison<Group> _groupOrder;

	internal GraphicsDevice GraphicsDevice { get; private set; }

	public Mesh Parent { get; set; }

	public object CustomData { get; set; }

	public Group FirstGroup => Groups[0];

	public List<Group> Groups { get; private set; }

	public BaseEffect Effect { get; set; }

	public RenderingHandler CustomRenderingHandler { get; set; }

	public Material Material { get; set; }

	public bool Enabled { get; set; }

	public bool AlwaysOnTop { get; set; }

	public bool DepthWrites { get; set; }

	public CullMode Culling { get; set; }

	public BlendingMode? Blending { get; set; }

	public int UsedSamplers { get; set; }

	public SamplerState SamplerState { get; set; }

	public bool RotateOffCenter { get; set; }

	public bool ScaleAfterRotation { get; set; }

	public bool SkipStates { get; set; }

	public bool SkipGroupCheck { get; set; }

	public TexturingType TexturingType => texturingType;

	public Dirtyable<Texture> Texture
	{
		get
		{
			return texture;
		}
		set
		{
			if (value == null)
			{
				texture.Set(null);
			}
			else
			{
				texture.Set(value);
			}
			texturingType = ((value != null) ? ((value.Value is Texture2D) ? TexturingType.Texture2D : ((value.Value is TextureCube) ? TexturingType.Cubemap : TexturingType.None)) : TexturingType.None);
		}
	}

	public Texture2D TextureMap => texture.Value as Texture2D;

	public TextureCube CubeMap => texture.Value as TextureCube;

	public Dirtyable<Matrix> TextureMatrix
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
				RebuildWorld();
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
				RebuildWorld();
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
				RebuildWorld();
			}
		}
	}

	public Matrix WorldMatrix
	{
		get
		{
			return worldMatrix;
		}
		set
		{
			worldMatrix = value;
			worldMatrix.Decompose(out scale, out rotation, out position);
			foreach (Group group in Groups)
			{
				group.RebuildCompositeWorld();
			}
		}
	}

	public Comparison<Group> GroupOrder
	{
		get
		{
			return _groupOrder;
		}
		set
		{
			_groupOrder = value;
			groupsDirty = true;
		}
	}

	public bool GroupLazyMatrices { get; set; }

	public Mesh()
	{
		Material = new Material();
		Groups = new List<Group>();
		WorldMatrix = (TextureMatrix = Matrix.Identity);
		DepthWrites = true;
		Culling = CullMode.CullCounterClockwiseFace;
		texturingType = TexturingType.None;
		UsedSamplers = 1;
		GroupOrder = DefaultOrder;
		Enabled = true;
		GraphicsDevice = ServiceHelper.Get<IGraphicsDeviceService>().GraphicsDevice;
	}

	public void SetFastPosition(Vector3 position)
	{
		this.position = position;
		worldMatrix = Matrix.CreateTranslation(position);
		foreach (Group group in Groups)
		{
			group.RebuildCompositeWorld(invert: false);
		}
	}

	public void Recenter<T>(Vector3 center) where T : struct, IVertex
	{
		foreach (Group group in Groups)
		{
			T[] vertices = (group.Geometry as IndexedUserPrimitives<T>).Vertices;
			for (int i = 0; i < vertices.Length; i++)
			{
				vertices[i].Position = center - vertices[i].Position;
			}
		}
	}

	public void BakeTransform<T>() where T : struct, IVertex
	{
		foreach (Group group in Groups)
		{
			group.BakeTransform<T>();
		}
		WorldMatrix = Matrix.Identity;
	}

	public void BakeTransformWithNormal<T>() where T : struct, ILitVertex
	{
		foreach (Group group in Groups)
		{
			group.BakeTransformWithNormal<T>();
		}
		WorldMatrix = Matrix.Identity;
	}

	public Group CollapseToBufferWithNormal<T>() where T : struct, ILitVertex
	{
		return CollapseToBufferWithNormal<T>(0, Groups.Count);
	}

	public Group CollapseToBufferWithNormal<T>(int fromGroup, int count) where T : struct, ILitVertex
	{
		if (count == 0)
		{
			return null;
		}
		List<T> vertices = new List<T>();
		List<int> list = new List<int>();
		PrimitiveType type = PrimitiveType.TriangleList;
		for (int num = fromGroup + count - 1; num >= fromGroup; num--)
		{
			Group group = Groups[num];
			group.BakeTransformWithNormal<T>();
			IndexedUserPrimitives<T> indexedUserPrimitives = group.Geometry as IndexedUserPrimitives<T>;
			list.AddRange(indexedUserPrimitives.Indices.Select((int x) => x + vertices.Count));
			vertices.AddRange(indexedUserPrimitives.Vertices);
			type = indexedUserPrimitives.PrimitiveType;
			RemoveGroupAt(num);
		}
		Group group2 = AddGroup();
		BufferedIndexedPrimitives<T> bufferedIndexedPrimitives = new BufferedIndexedPrimitives<T>(vertices.ToArray(), list.ToArray(), type);
		bufferedIndexedPrimitives.UpdateBuffers();
		group2.Geometry = bufferedIndexedPrimitives;
		return group2;
	}

	public Group CollapseToBufferWithNormalTexture<T>() where T : struct, ILitVertex, ITexturedVertex
	{
		return CollapseToBufferWithNormalTexture<T>(0, Groups.Count);
	}

	public Group CollapseToBufferWithNormalTexture<T>(int fromGroup, int count) where T : struct, ILitVertex, ITexturedVertex
	{
		if (count == 0)
		{
			return null;
		}
		List<T> vertices = new List<T>();
		List<int> list = new List<int>();
		PrimitiveType type = PrimitiveType.TriangleList;
		for (int num = fromGroup + count - 1; num >= fromGroup; num--)
		{
			Group group = Groups[num];
			group.BakeTransformWithNormalTexture<T>();
			IndexedUserPrimitives<T> indexedUserPrimitives = group.Geometry as IndexedUserPrimitives<T>;
			list.AddRange(indexedUserPrimitives.Indices.Select((int x) => x + vertices.Count));
			vertices.AddRange(indexedUserPrimitives.Vertices);
			type = indexedUserPrimitives.PrimitiveType;
			RemoveGroupAt(num);
		}
		Group group2 = AddGroup();
		BufferedIndexedPrimitives<T> bufferedIndexedPrimitives = new BufferedIndexedPrimitives<T>(vertices.ToArray(), list.ToArray(), type);
		bufferedIndexedPrimitives.UpdateBuffers();
		group2.Geometry = bufferedIndexedPrimitives;
		return group2;
	}

	public Group CollapseWithNormalTexture<T>() where T : struct, ILitVertex, ITexturedVertex
	{
		return CollapseWithNormalTexture<T>(0, Groups.Count);
	}

	public Group CollapseWithNormalTexture<T>(int fromGroup, int count) where T : struct, ILitVertex, ITexturedVertex
	{
		if (count == 0)
		{
			return null;
		}
		List<T> vertices = new List<T>();
		List<int> list = new List<int>();
		PrimitiveType type = PrimitiveType.TriangleList;
		for (int num = fromGroup + count - 1; num >= fromGroup; num--)
		{
			Group group = Groups[num];
			group.BakeTransformWithNormalTexture<T>();
			IndexedUserPrimitives<T> indexedUserPrimitives = group.Geometry as IndexedUserPrimitives<T>;
			list.AddRange(indexedUserPrimitives.Indices.Select((int x) => x + vertices.Count));
			vertices.AddRange(indexedUserPrimitives.Vertices);
			type = indexedUserPrimitives.PrimitiveType;
			RemoveGroupAt(num);
		}
		Group group2 = AddGroup();
		group2.Geometry = new IndexedUserPrimitives<T>(vertices.ToArray(), list.ToArray(), type);
		return group2;
	}

	public Group CollapseWithNormal<T>() where T : struct, ILitVertex
	{
		return CollapseWithNormal<T>(0, Groups.Count);
	}

	public Group CollapseWithNormal<T>(int fromGroup, int count) where T : struct, ILitVertex
	{
		if (count == 0)
		{
			return null;
		}
		List<T> vertices = new List<T>();
		List<int> list = new List<int>();
		PrimitiveType type = PrimitiveType.TriangleList;
		for (int num = fromGroup + count - 1; num >= fromGroup; num--)
		{
			Group group = Groups[num];
			group.BakeTransformWithNormal<T>();
			IndexedUserPrimitives<T> indexedUserPrimitives = group.Geometry as IndexedUserPrimitives<T>;
			list.AddRange(indexedUserPrimitives.Indices.Select((int x) => x + vertices.Count));
			vertices.AddRange(indexedUserPrimitives.Vertices);
			type = indexedUserPrimitives.PrimitiveType;
			RemoveGroupAt(num);
		}
		Group group2 = AddGroup();
		group2.Geometry = new IndexedUserPrimitives<T>(vertices.ToArray(), list.ToArray(), type);
		return group2;
	}

	public Group CollapseToBuffer<T>() where T : struct, IVertex
	{
		return CollapseToBuffer<T>(0, Groups.Count);
	}

	public Group CollapseToBuffer<T>(int fromGroup, int count) where T : struct, IVertex
	{
		if (count == 0)
		{
			return null;
		}
		List<T> vertices = new List<T>();
		List<int> list = new List<int>();
		PrimitiveType type = PrimitiveType.TriangleList;
		for (int num = fromGroup + count - 1; num >= fromGroup; num--)
		{
			Group group = Groups[num];
			group.BakeTransform<T>();
			IndexedUserPrimitives<T> indexedUserPrimitives = group.Geometry as IndexedUserPrimitives<T>;
			list.AddRange(indexedUserPrimitives.Indices.Select((int x) => x + vertices.Count));
			vertices.AddRange(indexedUserPrimitives.Vertices);
			type = indexedUserPrimitives.PrimitiveType;
			RemoveGroupAt(num);
		}
		Group group2 = AddGroup();
		BufferedIndexedPrimitives<T> bufferedIndexedPrimitives = new BufferedIndexedPrimitives<T>(vertices.ToArray(), list.ToArray(), type);
		bufferedIndexedPrimitives.UpdateBuffers();
		group2.Geometry = bufferedIndexedPrimitives;
		return group2;
	}

	public Group Collapse<T>() where T : struct, IVertex
	{
		return Collapse<T>(0, Groups.Count);
	}

	public Group Collapse<T>(int fromGroup, int count) where T : struct, IVertex
	{
		if (count == 0)
		{
			return null;
		}
		List<T> vertices = new List<T>();
		List<int> list = new List<int>();
		PrimitiveType type = PrimitiveType.TriangleList;
		for (int num = fromGroup + count - 1; num >= fromGroup; num--)
		{
			Group group = Groups[num];
			group.BakeTransform<T>();
			IndexedUserPrimitives<T> indexedUserPrimitives = group.Geometry as IndexedUserPrimitives<T>;
			list.AddRange(indexedUserPrimitives.Indices.Select((int x) => x + vertices.Count));
			vertices.AddRange(indexedUserPrimitives.Vertices);
			type = indexedUserPrimitives.PrimitiveType;
			RemoveGroupAt(num);
		}
		Group group2 = AddGroup();
		group2.Geometry = new IndexedUserPrimitives<T>(vertices.ToArray(), list.ToArray(), type);
		return group2;
	}

	private void RebuildWorld()
	{
		Matrix matrix;
		Matrix matrix2;
		Matrix matrix3;
		if (RotateOffCenter)
		{
			matrix = Matrix.CreateScale(scale);
			matrix2 = Matrix.CreateTranslation(position);
			matrix3 = Matrix.CreateFromQuaternion(rotation);
		}
		else if (ScaleAfterRotation)
		{
			matrix = Matrix.CreateFromQuaternion(rotation);
			matrix2 = Matrix.CreateScale(scale);
			matrix3 = Matrix.CreateTranslation(position);
		}
		else
		{
			matrix = Matrix.CreateScale(scale);
			matrix2 = Matrix.CreateFromQuaternion(rotation);
			matrix3 = Matrix.CreateTranslation(position);
		}
		worldMatrix = matrix * matrix2 * matrix3;
		if (GroupLazyMatrices)
		{
			return;
		}
		foreach (Group group in Groups)
		{
			group.RebuildCompositeWorld();
		}
	}

	public void Draw()
	{
		if (!Enabled || Effect == null || Effect.IsDisposed)
		{
			return;
		}
		BlendingMode blendingMode = GraphicsDevice.GetBlendCombiner().BlendingMode;
		if (!SkipGroupCheck)
		{
			bool flag = false;
			for (int i = 0; i < Groups.Count; i++)
			{
				if (flag)
				{
					break;
				}
				flag |= Groups[i].Enabled;
			}
			if (!flag)
			{
				return;
			}
		}
		Effect.Prepare(this);
		GraphicsDevice graphicsDevice = GraphicsDevice;
		if (!SkipStates)
		{
			graphicsDevice.GetDssCombiner().DepthBufferFunction = ((!AlwaysOnTop) ? CompareFunction.LessEqual : CompareFunction.Always);
			graphicsDevice.GetDssCombiner().DepthBufferWriteEnable = DepthWrites;
			graphicsDevice.SetCullMode(Culling);
			if (Blending.HasValue)
			{
				graphicsDevice.SetBlendingMode(Blending.Value);
			}
			if (SamplerState != null)
			{
				for (int j = 0; j < UsedSamplers; j++)
				{
					graphicsDevice.SamplerStates[j] = SamplerState;
				}
			}
		}
		if (CustomRenderingHandler == null)
		{
			if (_groupOrder != DefaultOrder && groupsDirty)
			{
				Groups.Sort(_groupOrder);
				groupsDirty = false;
			}
			foreach (Group group in Groups)
			{
				if (!group.Enabled)
				{
					continue;
				}
				group.Draw(Effect);
				if (SkipStates)
				{
					continue;
				}
				if (group.SamplerState != null && SamplerState != null)
				{
					for (int k = 0; k < UsedSamplers; k++)
					{
						graphicsDevice.SamplerStates[k] = SamplerState;
					}
				}
				if (group.Blending.HasValue)
				{
					graphicsDevice.SetBlendingMode((!Blending.HasValue) ? blendingMode : Blending.Value);
				}
				if (group.CullMode.HasValue)
				{
					graphicsDevice.SetCullMode(Culling);
				}
				if (group.AlwaysOnTop.HasValue)
				{
					graphicsDevice.GetDssCombiner().DepthBufferFunction = ((!AlwaysOnTop) ? CompareFunction.LessEqual : CompareFunction.Always);
				}
				if (group.NoAlphaWrite.HasValue)
				{
					graphicsDevice.GetBlendCombiner().ColorWriteChannels = ColorWriteChannels.All;
				}
			}
		}
		else
		{
			CustomRenderingHandler(this, Effect);
		}
		if (!SkipStates)
		{
			if (Blending.HasValue && Blending.Value != blendingMode)
			{
				graphicsDevice.SetBlendingMode(blendingMode);
			}
			if (!DepthWrites)
			{
				graphicsDevice.GetDssCombiner().DepthBufferWriteEnable = true;
				graphicsDevice.GetDssCombiner().Apply(graphicsDevice);
			}
			graphicsDevice.GetRasterCombiner().Apply(graphicsDevice);
			graphicsDevice.GetBlendCombiner().Apply(graphicsDevice);
		}
	}

	public Mesh Clone()
	{
		Mesh mesh = new Mesh
		{
			AlwaysOnTop = AlwaysOnTop,
			CustomRenderingHandler = CustomRenderingHandler,
			DepthWrites = DepthWrites,
			Effect = Effect.Clone(),
			RotateOffCenter = RotateOffCenter,
			WorldMatrix = WorldMatrix,
			TextureMatrix = TextureMatrix,
			ScaleAfterRotation = ScaleAfterRotation,
			Texture = Texture,
			SamplerState = SamplerState,
			UsedSamplers = UsedSamplers,
			Blending = Blending,
			Culling = Culling,
			Material = ((Material == null) ? null : Material.Clone())
		};
		foreach (Group group in Groups)
		{
			Group item = group.Clone(mesh);
			mesh.Groups.Add(item);
		}
		return mesh;
	}

	public void ClearGroups()
	{
		foreach (Group group in Groups)
		{
			if (group.Geometry is IDisposable)
			{
				(group.Geometry as IDisposable).Dispose();
			}
			if (group.Geometry is IFakeDisposable)
			{
				(group.Geometry as IFakeDisposable).Dispose();
			}
		}
		Groups.Clear();
		groupsDirty = true;
	}

	public void RemoveGroup(Group group)
	{
		RemoveGroup(group, skipDispose: false);
	}

	public void RemoveGroup(Group group, bool skipDispose)
	{
		if (!skipDispose)
		{
			if (group.Geometry is IDisposable)
			{
				(group.Geometry as IDisposable).Dispose();
			}
			if (group.Geometry is IFakeDisposable)
			{
				(group.Geometry as IFakeDisposable).Dispose();
			}
		}
		Groups.Remove(group);
		groupsDirty = true;
	}

	public void RemoveGroupAt(int i)
	{
		if (Groups[i].Geometry is IDisposable)
		{
			(Groups[i].Geometry as IDisposable).Dispose();
		}
		if (Groups[i].Geometry is IFakeDisposable)
		{
			(Groups[i].Geometry as IFakeDisposable).Dispose();
		}
		Groups.RemoveAt(i);
		groupsDirty = true;
	}

	public Group AddGroup()
	{
		Group group = new Group(this, Groups.Count);
		Groups.Add(group);
		groupsDirty = true;
		return group;
	}

	public Group CloneGroup(Group group)
	{
		Group group2 = group.Clone(this);
		Groups.Add(group2);
		groupsDirty = true;
		return group2;
	}

	public Group AddTexturedCylinder(Vector3 size, Vector3 origin, int stacks, int slices, bool centeredOnOrigin)
	{
		return AddTexturedCylinder(size, origin, stacks, slices, centeredOnOrigin, capped: true);
	}

	public Group AddTexturedCylinder(Vector3 size, Vector3 origin, int stacks, int slices, bool centeredOnOrigin, bool capped)
	{
		size /= 2f;
		if (!centeredOnOrigin)
		{
			origin += size;
		}
		Group group = new Group(this, Groups.Count);
		List<FezVertexPositionNormalTexture> list = new List<FezVertexPositionNormalTexture>();
		for (int i = 0; i <= stacks; i++)
		{
			if (capped || (i != 0 && i != stacks))
			{
				float num = MathHelper.Clamp(i - 1, 0f, stacks - 2) / (float)(stacks - 2) * 2f - 1f;
				int num2 = ((i != 0 && i != stacks) ? 1 : 0);
				for (int j = 0; j <= slices; j++)
				{
					float num3 = (float)j / (float)slices;
					float num4 = num3 * ((float)Math.PI * 2f);
					list.Add(new FezVertexPositionNormalTexture(new Vector3((float)Math.Sin(num4) * (float)num2, num, (float)Math.Cos(num4) * (float)num2) * size + origin, new Vector3((float)Math.Sin(num4), 0f, (float)Math.Cos(num4)), new Vector2(num3, 1f - (num + 1f) / 2f)));
				}
			}
		}
		List<int> list2 = new List<int>();
		for (int k = 0; k < stacks; k++)
		{
			if (capped || (k != 0 && k != stacks - 1))
			{
				int num5 = k;
				if (!capped)
				{
					num5--;
				}
				for (int l = 0; l <= slices; l++)
				{
					list2.Add(num5 * (slices + 1) + l);
					list2.Add((num5 + 1) * (slices + 1) + l);
				}
			}
		}
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionNormalTexture>(list.ToArray(), list2.ToArray(), PrimitiveType.TriangleStrip);
		Groups.Add(group);
		return group;
	}

	public Group AddColoredBox(Vector3 size, Vector3 origin, Color color, bool centeredOnOrigin)
	{
		size /= 2f;
		if (!centeredOnOrigin)
		{
			origin += size;
		}
		Group group = new Group(this, Groups.Count);
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(new FezVertexPositionColor[8]
		{
			new FezVertexPositionColor(new Vector3(-1f, -1f, -1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(1f, -1f, -1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(1f, 1f, -1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(-1f, 1f, -1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(-1f, -1f, 1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(1f, -1f, 1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(1f, 1f, 1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(-1f, 1f, 1f) * size + origin, color)
		}, new int[36]
		{
			0, 1, 2, 0, 2, 3, 1, 5, 6, 1,
			6, 2, 0, 7, 4, 0, 3, 7, 3, 2,
			6, 3, 6, 7, 4, 6, 5, 4, 7, 6,
			0, 5, 1, 0, 4, 5
		}, PrimitiveType.TriangleList);
		Group group2 = group;
		Groups.Add(group2);
		return group2;
	}

	public Group AddWireframeBox(Vector3 size, Vector3 origin, Color color, bool centeredOnOrigin)
	{
		size /= 2f;
		if (!centeredOnOrigin)
		{
			origin += size;
		}
		Group group = new Group(this, Groups.Count);
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(new FezVertexPositionColor[8]
		{
			new FezVertexPositionColor(new Vector3(-1f, -1f, -1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(1f, -1f, -1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(1f, 1f, -1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(-1f, 1f, -1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(-1f, -1f, 1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(1f, -1f, 1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(1f, 1f, 1f) * size + origin, color),
			new FezVertexPositionColor(new Vector3(-1f, 1f, 1f) * size + origin, color)
		}, new int[24]
		{
			0, 1, 1, 2, 2, 3, 3, 0, 4, 5,
			5, 6, 6, 7, 7, 4, 0, 4, 1, 5,
			2, 6, 3, 7
		}, PrimitiveType.LineList);
		Group group2 = group;
		Groups.Add(group2);
		return group2;
	}

	public Group AddFlatShadedBox(Vector3 size, Vector3 origin, Color color, bool centeredOnOrigin)
	{
		size /= 2f;
		if (!centeredOnOrigin)
		{
			origin += size;
		}
		Group group = new Group(this, Groups.Count);
		group.Geometry = new IndexedUserPrimitives<VertexPositionNormalColor>(new VertexPositionNormalColor[24]
		{
			new VertexPositionNormalColor(new Vector3(-1f, -1f, -1f) * size + origin, -Vector3.UnitZ, color),
			new VertexPositionNormalColor(new Vector3(-1f, 1f, -1f) * size + origin, -Vector3.UnitZ, color),
			new VertexPositionNormalColor(new Vector3(1f, 1f, -1f) * size + origin, -Vector3.UnitZ, color),
			new VertexPositionNormalColor(new Vector3(1f, -1f, -1f) * size + origin, -Vector3.UnitZ, color),
			new VertexPositionNormalColor(new Vector3(1f, -1f, -1f) * size + origin, Vector3.UnitX, color),
			new VertexPositionNormalColor(new Vector3(1f, 1f, -1f) * size + origin, Vector3.UnitX, color),
			new VertexPositionNormalColor(new Vector3(1f, 1f, 1f) * size + origin, Vector3.UnitX, color),
			new VertexPositionNormalColor(new Vector3(1f, -1f, 1f) * size + origin, Vector3.UnitX, color),
			new VertexPositionNormalColor(new Vector3(1f, -1f, 1f) * size + origin, Vector3.UnitZ, color),
			new VertexPositionNormalColor(new Vector3(1f, 1f, 1f) * size + origin, Vector3.UnitZ, color),
			new VertexPositionNormalColor(new Vector3(-1f, 1f, 1f) * size + origin, Vector3.UnitZ, color),
			new VertexPositionNormalColor(new Vector3(-1f, -1f, 1f) * size + origin, Vector3.UnitZ, color),
			new VertexPositionNormalColor(new Vector3(-1f, -1f, 1f) * size + origin, -Vector3.UnitX, color),
			new VertexPositionNormalColor(new Vector3(-1f, 1f, 1f) * size + origin, -Vector3.UnitX, color),
			new VertexPositionNormalColor(new Vector3(-1f, 1f, -1f) * size + origin, -Vector3.UnitX, color),
			new VertexPositionNormalColor(new Vector3(-1f, -1f, -1f) * size + origin, -Vector3.UnitX, color),
			new VertexPositionNormalColor(new Vector3(-1f, -1f, -1f) * size + origin, -Vector3.UnitY, color),
			new VertexPositionNormalColor(new Vector3(-1f, -1f, 1f) * size + origin, -Vector3.UnitY, color),
			new VertexPositionNormalColor(new Vector3(1f, -1f, 1f) * size + origin, -Vector3.UnitY, color),
			new VertexPositionNormalColor(new Vector3(1f, -1f, -1f) * size + origin, -Vector3.UnitY, color),
			new VertexPositionNormalColor(new Vector3(-1f, 1f, -1f) * size + origin, Vector3.UnitY, color),
			new VertexPositionNormalColor(new Vector3(-1f, 1f, 1f) * size + origin, Vector3.UnitY, color),
			new VertexPositionNormalColor(new Vector3(1f, 1f, 1f) * size + origin, Vector3.UnitY, color),
			new VertexPositionNormalColor(new Vector3(1f, 1f, -1f) * size + origin, Vector3.UnitY, color)
		}, new int[36]
		{
			0, 2, 1, 0, 3, 2, 4, 6, 5, 4,
			7, 6, 8, 10, 9, 8, 11, 10, 12, 14,
			13, 12, 15, 14, 16, 17, 18, 16, 18, 19,
			20, 22, 21, 20, 23, 22
		}, PrimitiveType.TriangleList);
		Group group2 = group;
		Groups.Add(group2);
		return group2;
	}

	public Group AddCubemappedBox(Vector3 size, Vector3 origin, bool centeredOnOrigin)
	{
		size /= 2f;
		if (!centeredOnOrigin)
		{
			origin += size;
		}
		Group group = new Group(this, Groups.Count);
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionNormalTexture>(new FezVertexPositionNormalTexture[24]
		{
			new FezVertexPositionNormalTexture(new Vector3(-1f, -1f, -1f) * size + origin, -Vector3.UnitZ),
			new FezVertexPositionNormalTexture(new Vector3(-1f, 1f, -1f) * size + origin, -Vector3.UnitZ),
			new FezVertexPositionNormalTexture(new Vector3(1f, 1f, -1f) * size + origin, -Vector3.UnitZ),
			new FezVertexPositionNormalTexture(new Vector3(1f, -1f, -1f) * size + origin, -Vector3.UnitZ),
			new FezVertexPositionNormalTexture(new Vector3(1f, -1f, -1f) * size + origin, Vector3.UnitX),
			new FezVertexPositionNormalTexture(new Vector3(1f, 1f, -1f) * size + origin, Vector3.UnitX),
			new FezVertexPositionNormalTexture(new Vector3(1f, 1f, 1f) * size + origin, Vector3.UnitX),
			new FezVertexPositionNormalTexture(new Vector3(1f, -1f, 1f) * size + origin, Vector3.UnitX),
			new FezVertexPositionNormalTexture(new Vector3(1f, -1f, 1f) * size + origin, Vector3.UnitZ),
			new FezVertexPositionNormalTexture(new Vector3(1f, 1f, 1f) * size + origin, Vector3.UnitZ),
			new FezVertexPositionNormalTexture(new Vector3(-1f, 1f, 1f) * size + origin, Vector3.UnitZ),
			new FezVertexPositionNormalTexture(new Vector3(-1f, -1f, 1f) * size + origin, Vector3.UnitZ),
			new FezVertexPositionNormalTexture(new Vector3(-1f, -1f, 1f) * size + origin, -Vector3.UnitX),
			new FezVertexPositionNormalTexture(new Vector3(-1f, 1f, 1f) * size + origin, -Vector3.UnitX),
			new FezVertexPositionNormalTexture(new Vector3(-1f, 1f, -1f) * size + origin, -Vector3.UnitX),
			new FezVertexPositionNormalTexture(new Vector3(-1f, -1f, -1f) * size + origin, -Vector3.UnitX),
			new FezVertexPositionNormalTexture(new Vector3(-1f, -1f, -1f) * size + origin, -Vector3.UnitY),
			new FezVertexPositionNormalTexture(new Vector3(-1f, -1f, 1f) * size + origin, -Vector3.UnitY),
			new FezVertexPositionNormalTexture(new Vector3(1f, -1f, 1f) * size + origin, -Vector3.UnitY),
			new FezVertexPositionNormalTexture(new Vector3(1f, -1f, -1f) * size + origin, -Vector3.UnitY),
			new FezVertexPositionNormalTexture(new Vector3(-1f, 1f, -1f) * size + origin, Vector3.UnitY),
			new FezVertexPositionNormalTexture(new Vector3(-1f, 1f, 1f) * size + origin, Vector3.UnitY),
			new FezVertexPositionNormalTexture(new Vector3(1f, 1f, 1f) * size + origin, Vector3.UnitY),
			new FezVertexPositionNormalTexture(new Vector3(1f, 1f, -1f) * size + origin, Vector3.UnitY)
		}, new int[36]
		{
			0, 2, 1, 0, 3, 2, 4, 6, 5, 4,
			7, 6, 8, 10, 9, 8, 11, 10, 12, 14,
			13, 12, 15, 14, 16, 17, 18, 16, 18, 19,
			20, 22, 21, 20, 23, 22
		}, PrimitiveType.TriangleList);
		Group group2 = group;
		IndexedUserPrimitives<FezVertexPositionNormalTexture> indexedUserPrimitives = group2.Geometry as IndexedUserPrimitives<FezVertexPositionNormalTexture>;
		for (int i = 0; i < indexedUserPrimitives.Vertices.Length; i++)
		{
			indexedUserPrimitives.Vertices[i].TextureCoordinate = indexedUserPrimitives.Vertices[i].ComputeTexCoord();
		}
		Groups.Add(group2);
		return group2;
	}

	public Group AddLine(Vector3 origin, Vector3 destination, Color color)
	{
		Group group = new Group(this, Groups.Count);
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(new FezVertexPositionColor[2]
		{
			new FezVertexPositionColor(origin, color),
			new FezVertexPositionColor(destination, color)
		}, new int[2] { 0, 1 }, PrimitiveType.LineList);
		Group group2 = group;
		Groups.Add(group2);
		return group2;
	}

	public Group AddLine(Vector3 origin, Vector3 destination, Color colorA, Color colorB)
	{
		Group group = new Group(this, Groups.Count);
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(new FezVertexPositionColor[2]
		{
			new FezVertexPositionColor(origin, colorA),
			new FezVertexPositionColor(destination, colorB)
		}, new int[2] { 0, 1 }, PrimitiveType.LineList);
		Group group2 = group;
		Groups.Add(group2);
		return group2;
	}

	public Group AddLines(Color[] pointColors, Vector3[] pointPairs, bool buffered)
	{
		FezVertexPositionColor[] array = new FezVertexPositionColor[pointColors.Length];
		int[] array2 = new int[pointColors.Length];
		for (int i = 0; i < pointColors.Length; i++)
		{
			array[i] = new FezVertexPositionColor(pointPairs[i], pointColors[i]);
			array2[i] = i;
		}
		IIndexedPrimitiveCollection indexedPrimitiveCollection;
		if (buffered)
		{
			indexedPrimitiveCollection = new BufferedIndexedPrimitives<FezVertexPositionColor>(array, array2, PrimitiveType.LineList);
			(indexedPrimitiveCollection as BufferedIndexedPrimitives<FezVertexPositionColor>).UpdateBuffers();
			(indexedPrimitiveCollection as BufferedIndexedPrimitives<FezVertexPositionColor>).CleanUp();
		}
		else
		{
			indexedPrimitiveCollection = new IndexedUserPrimitives<FezVertexPositionColor>(array, array2, PrimitiveType.LineList);
		}
		Group group = new Group(this, Groups.Count)
		{
			Geometry = indexedPrimitiveCollection
		};
		Groups.Add(group);
		return group;
	}

	public Group AddLines(Color[] pointColors, params Vector3[] pointPairs)
	{
		return AddLines(pointColors, pointPairs, buffered: false);
	}

	public Group AddWireframePolygon(Color color, params Vector3[] points)
	{
		Group group = new Group(this, Groups.Count)
		{
			Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(points.Select((Vector3 x) => new FezVertexPositionColor(x, color)).ToArray(), points.SelectMany((Vector3 x, int i) => new int[2]
			{
				i - 1,
				i
			}).Skip(2).ToArray(), PrimitiveType.LineList)
		};
		Groups.Add(group);
		return group;
	}

	public Group AddWireframeArrow(float size, float arrowSize, Vector3 origin, FaceOrientation direction, Color color)
	{
		Vector3 direction2 = direction.AsVector();
		return AddWireframeArrow(size, arrowSize, origin, direction2, color);
	}

	public Group AddWireframeArrow(float size, float arrowSize, Vector3 origin, Vector3 direction, Color color)
	{
		direction.Normalize();
		Vector3 vector = (FezMath.AlmostEqual(direction.Abs(), Vector3.UnitX) ? Vector3.Cross(direction, Vector3.UnitZ) : Vector3.Cross(direction, Vector3.UnitX));
		vector.Normalize();
		Group group = new Group(this, Groups.Count);
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(new FezVertexPositionColor[4]
		{
			new FezVertexPositionColor(origin, color),
			new FezVertexPositionColor(direction * size + origin, color),
			new FezVertexPositionColor(direction * (size - arrowSize) + vector * arrowSize + origin, color),
			new FezVertexPositionColor(direction * (size - arrowSize) - vector * arrowSize + origin, color)
		}, new int[6] { 0, 1, 1, 2, 1, 3 }, PrimitiveType.LineList);
		Group group2 = group;
		Groups.Add(group2);
		return group2;
	}

	public Group AddColoredQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color aColor, Color bColor, Color cColor, Color dColor)
	{
		Group group = new Group(this, Groups.Count);
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(new FezVertexPositionColor[4]
		{
			new FezVertexPositionColor(a, aColor),
			new FezVertexPositionColor(b, bColor),
			new FezVertexPositionColor(c, cColor),
			new FezVertexPositionColor(d, dColor)
		}, new int[6] { 0, 1, 2, 1, 0, 3 }, PrimitiveType.TriangleList);
		Group group2 = group;
		Groups.Add(group2);
		return group2;
	}

	public Group AddColoredTriangle(Vector3 a, Vector3 b, Vector3 c, Color aColor, Color bColor, Color cColor)
	{
		Group group = new Group(this, Groups.Count);
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(new FezVertexPositionColor[3]
		{
			new FezVertexPositionColor(a, aColor),
			new FezVertexPositionColor(b, bColor),
			new FezVertexPositionColor(c, cColor)
		}, new int[3] { 0, 1, 2 }, PrimitiveType.TriangleList);
		Group group2 = group;
		Groups.Add(group2);
		return group2;
	}

	public Group AddFace(Vector3 size, Vector3 origin, FaceOrientation face, Color color, bool centeredOnOrigin)
	{
		return AddFace(size, origin, face, color, centeredOnOrigin, doublesided: true, crosshatch: false);
	}

	public Group AddFace(Vector3 size, Vector3 origin, FaceOrientation face, Color color, bool centeredOnOrigin, bool doublesided)
	{
		return AddFace(size, origin, face, color, centeredOnOrigin, colored: true, doublesided, crosshatch: false);
	}

	public Group AddFace(Vector3 size, Vector3 origin, FaceOrientation face, Color color, bool centeredOnOrigin, bool doublesided, bool crosshatch)
	{
		return AddFace(size, origin, face, color, centeredOnOrigin, colored: true, doublesided, crosshatch);
	}

	public Group AddFace(Vector3 size, Vector3 origin, FaceOrientation face, bool centeredOnOrigin)
	{
		return AddFace(size, origin, face, centeredOnOrigin, doublesided: false);
	}

	public Group AddFace(Vector3 size, Vector3 origin, FaceOrientation face, bool centeredOnOrigin, bool doublesided)
	{
		return AddFace(size, origin, face, Color.White, centeredOnOrigin, colored: false, doublesided, crosshatch: false);
	}

	public Group AddFace(Vector3 size, Vector3 origin, FaceOrientation face, bool centeredOnOrigin, bool doublesided, bool crosshatch)
	{
		return AddFace(size, origin, face, Color.White, centeredOnOrigin, colored: false, doublesided, crosshatch);
	}

	private Group AddFace(Vector3 size, Vector3 origin, FaceOrientation face, Color color, bool centeredOnOrigin, bool colored, bool doublesided, bool crosshatch)
	{
		Vector3 vector = face.AsVector();
		FaceOrientation tangent = face.GetTangent();
		FaceOrientation bitangent = face.GetBitangent();
		Vector3 vector2 = tangent.AsVector();
		Vector3 vector3 = bitangent.AsVector();
		if (centeredOnOrigin)
		{
			origin -= (vector2 + vector3) * size / 2f;
		}
		Vector3 vector4 = origin;
		Vector3 vector5 = origin + vector2 * size;
		Vector3 vector6 = origin + (vector2 + vector3) * size;
		Vector3 vector7 = origin + vector3 * size;
		Group group = new Group(this, Groups.Count);
		Groups.Add(group);
		if (colored)
		{
			AddFace(group, face, new VertexPositionNormalColor(vector4, vector, color), new VertexPositionNormalColor(vector5, vector, color), new VertexPositionNormalColor(vector6, vector, color), new VertexPositionNormalColor(vector7, vector, color));
			if (doublesided)
			{
				AddFace(group, face, new VertexPositionNormalColor(vector4, -vector, color), new VertexPositionNormalColor(vector7, -vector, color), new VertexPositionNormalColor(vector6, -vector, color), new VertexPositionNormalColor(vector5, -vector, color));
			}
			if (crosshatch)
			{
				if (centeredOnOrigin)
				{
					origin += (vector2 + vector3) * size / 2f;
				}
				face = (face.GetTangent().IsSide() ? face.GetTangent() : face.GetBitangent());
				float z = size.Z;
				size.Z = size.X;
				size.X = z;
				vector = face.AsVector();
				tangent = face.GetTangent();
				FaceOrientation bitangent2 = face.GetBitangent();
				vector2 = tangent.AsVector();
				vector3 = bitangent2.AsVector();
				if (centeredOnOrigin)
				{
					origin -= (vector2 + vector3) * size / 2f;
				}
				vector4 = origin;
				vector5 = origin + vector2 * size;
				vector6 = origin + (vector2 + vector3) * size;
				vector7 = origin + vector3 * size;
				AddFace(group, face, new VertexPositionNormalColor(vector4, vector, color), new VertexPositionNormalColor(vector5, vector, color), new VertexPositionNormalColor(vector6, vector, color), new VertexPositionNormalColor(vector7, vector, color));
				if (doublesided)
				{
					AddFace(group, face, new VertexPositionNormalColor(vector4, -vector, color), new VertexPositionNormalColor(vector7, -vector, color), new VertexPositionNormalColor(vector6, -vector, color), new VertexPositionNormalColor(vector5, -vector, color));
				}
			}
		}
		else
		{
			Vector2 vector8 = ((tangent.AsAxis() == Axis.Y) ? Vector2.UnitY : Vector2.UnitX);
			Vector2 vector9 = Vector2.One - vector8;
			Vector2 vector10 = new Vector2((tangent.AsAxis() == Axis.Y) ? (!face.IsPositive()).AsNumeric() : face.IsPositive().AsNumeric(), 1f);
			Vector2 texCoord = vector10;
			Vector2 texCoord2 = (vector8 - vector10).Abs();
			Vector2 texCoord3 = (Vector2.One - vector10).Abs();
			Vector2 texCoord4 = (vector9 - vector10).Abs();
			AddFace(group, face, new FezVertexPositionNormalTexture(vector4, vector, texCoord), new FezVertexPositionNormalTexture(vector5, vector, texCoord2), new FezVertexPositionNormalTexture(vector6, vector, texCoord3), new FezVertexPositionNormalTexture(vector7, vector, texCoord4));
			if (doublesided)
			{
				AddFace(group, face, new FezVertexPositionNormalTexture(vector4, -vector, texCoord), new FezVertexPositionNormalTexture(vector7, -vector, texCoord4), new FezVertexPositionNormalTexture(vector6, -vector, texCoord3), new FezVertexPositionNormalTexture(vector5, -vector, texCoord2));
			}
			if (crosshatch)
			{
				if (centeredOnOrigin)
				{
					origin += (vector2 + vector3) * size / 2f;
				}
				face = (face.GetTangent().IsSide() ? face.GetTangent() : face.GetBitangent());
				float z2 = size.Z;
				size.Z = size.X;
				size.X = z2;
				vector = face.AsVector();
				tangent = face.GetTangent();
				FaceOrientation bitangent3 = face.GetBitangent();
				vector2 = tangent.AsVector();
				vector3 = bitangent3.AsVector();
				if (centeredOnOrigin)
				{
					origin -= (vector2 + vector3) * size / 2f;
				}
				vector4 = origin;
				vector5 = origin + vector2 * size;
				vector6 = origin + (vector2 + vector3) * size;
				vector7 = origin + vector3 * size;
				vector8 = ((tangent.AsAxis() == Axis.Y) ? Vector2.UnitY : Vector2.UnitX);
				Vector2 vector11 = Vector2.One - vector8;
				vector10 = new Vector2((tangent.AsAxis() == Axis.Y) ? (!face.IsPositive()).AsNumeric() : face.IsPositive().AsNumeric(), 1f);
				texCoord = vector10;
				texCoord2 = (vector8 - vector10).Abs();
				texCoord3 = (Vector2.One - vector10).Abs();
				texCoord4 = (vector11 - vector10).Abs();
				AddFace(group, face, new FezVertexPositionNormalTexture(vector4, vector, texCoord), new FezVertexPositionNormalTexture(vector5, vector, texCoord2), new FezVertexPositionNormalTexture(vector6, vector, texCoord3), new FezVertexPositionNormalTexture(vector7, vector, texCoord4));
				if (doublesided)
				{
					AddFace(group, face, new FezVertexPositionNormalTexture(vector4, -vector, texCoord), new FezVertexPositionNormalTexture(vector7, -vector, texCoord4), new FezVertexPositionNormalTexture(vector6, -vector, texCoord3), new FezVertexPositionNormalTexture(vector5, -vector, texCoord2));
				}
			}
		}
		return group;
	}

	private static void AddFace<TVertex>(Group group, FaceOrientation face, TVertex v0, TVertex v1, TVertex v2, TVertex v3) where TVertex : struct, IVertex, IEquatable<TVertex>
	{
		FaceMaterialization<TVertex> faceMaterialization = default(FaceMaterialization<TVertex>);
		faceMaterialization.V0 = new SharedVertex<TVertex>
		{
			Vertex = v0,
			Index = 0
		};
		faceMaterialization.V1 = new SharedVertex<TVertex>
		{
			Vertex = v1,
			Index = 1
		};
		faceMaterialization.V2 = new SharedVertex<TVertex>
		{
			Vertex = v2,
			Index = 2
		};
		faceMaterialization.V3 = new SharedVertex<TVertex>
		{
			Vertex = v3,
			Index = 3
		};
		FaceMaterialization<TVertex> faceMaterialization2 = faceMaterialization;
		faceMaterialization2.SetupIndices(face);
		if (!(group.Geometry is IndexedUserPrimitives<TVertex> indexedUserPrimitives))
		{
			group.Geometry = new IndexedUserPrimitives<TVertex>(new TVertex[4]
			{
				faceMaterialization2.V0.Vertex,
				faceMaterialization2.V1.Vertex,
				faceMaterialization2.V2.Vertex,
				faceMaterialization2.V3.Vertex
			}, new int[6]
			{
				faceMaterialization2.GetIndex(0),
				faceMaterialization2.GetIndex(1),
				faceMaterialization2.GetIndex(2),
				faceMaterialization2.GetIndex(3),
				faceMaterialization2.GetIndex(4),
				faceMaterialization2.GetIndex(5)
			}, PrimitiveType.TriangleList);
		}
		else
		{
			int num = indexedUserPrimitives.Vertices.Length;
			indexedUserPrimitives.Vertices = Util.JoinArrays(indexedUserPrimitives.Vertices, new TVertex[4]
			{
				faceMaterialization2.V0.Vertex,
				faceMaterialization2.V1.Vertex,
				faceMaterialization2.V2.Vertex,
				faceMaterialization2.V3.Vertex
			});
			indexedUserPrimitives.Indices = Util.JoinArrays(indexedUserPrimitives.Indices, new int[6]
			{
				faceMaterialization2.GetIndex(0) + num,
				faceMaterialization2.GetIndex(1) + num,
				faceMaterialization2.GetIndex(2) + num,
				faceMaterialization2.GetIndex(3) + num,
				faceMaterialization2.GetIndex(4) + num,
				faceMaterialization2.GetIndex(5) + num
			});
		}
	}

	public Group AddWireframeFace(Vector3 size, Vector3 origin, FaceOrientation face, Color color, bool centeredOnOrigin)
	{
		if (centeredOnOrigin)
		{
			origin -= size / 2f;
		}
		Vector3 vector = face.AsVector() * size;
		Vector3 vector2 = face.GetTangent().AsVector() * size;
		Vector3 vector3 = face.GetBitangent().AsVector() * size;
		origin += (face.IsPositive() ? 1 : 0) * vector;
		Group group = new Group(this, Groups.Count);
		group.Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(new FezVertexPositionColor[4]
		{
			new FezVertexPositionColor(origin, color),
			new FezVertexPositionColor(origin + vector2, color),
			new FezVertexPositionColor(origin + vector2 + vector3, color),
			new FezVertexPositionColor(origin + vector3, color)
		}, new int[5] { 0, 1, 2, 3, 0 }, PrimitiveType.LineStrip);
		Group group2 = group;
		Groups.Add(group2);
		return group2;
	}

	public Group AddPoints(Color color, IEnumerable<Vector3> points, bool buffered)
	{
		Group group;
		if (buffered)
		{
			group = new Group(this, Groups.Count)
			{
				Geometry = new BufferedIndexedPrimitives<FezVertexPositionColor>(points.SelectMany((Vector3 p) => new FezVertexPositionColor[2]
				{
					new FezVertexPositionColor(p, new Color(color.R, color.G, color.B, 0)),
					new FezVertexPositionColor(p, new Color(color.R, color.G, color.B, 255))
				}).ToArray(), points.SelectMany((Vector3 p, int i) => new int[2]
				{
					i * 2,
					i * 2 + 1
				}).ToArray(), PrimitiveType.LineList)
			};
			(group.Geometry as BufferedIndexedPrimitives<FezVertexPositionColor>).UpdateBuffers();
		}
		else
		{
			group = new Group(this, Groups.Count)
			{
				Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(points.SelectMany((Vector3 p) => new FezVertexPositionColor[2]
				{
					new FezVertexPositionColor(p, new Color(color.R, color.G, color.B, 0)),
					new FezVertexPositionColor(p, new Color(color.R, color.G, color.B, 255))
				}).ToArray(), points.SelectMany((Vector3 p, int i) => new int[2]
				{
					i * 2,
					i * 2 + 1
				}).ToArray(), PrimitiveType.LineList)
			};
		}
		Groups.Add(group);
		return group;
	}

	public Group AddPoints(IList<Color> colors, IEnumerable<Vector3> points, bool buffered)
	{
		Group group;
		if (buffered)
		{
			group = new Group(this, Groups.Count)
			{
				Geometry = new BufferedIndexedPrimitives<FezVertexPositionColor>(points.SelectMany((Vector3 p, int i) => new FezVertexPositionColor[2]
				{
					new FezVertexPositionColor(p, new Color(colors[i].R, colors[i].G, colors[i].B, 0)),
					new FezVertexPositionColor(p, new Color(colors[i].R, colors[i].G, colors[i].B, 255))
				}).ToArray(), points.SelectMany((Vector3 p, int i) => new int[2]
				{
					i * 2,
					i * 2 + 1
				}).ToArray(), PrimitiveType.LineList)
			};
			(group.Geometry as BufferedIndexedPrimitives<FezVertexPositionColor>).UpdateBuffers();
		}
		else
		{
			group = new Group(this, Groups.Count)
			{
				Geometry = new IndexedUserPrimitives<FezVertexPositionColor>(points.SelectMany((Vector3 p, int i) => new FezVertexPositionColor[2]
				{
					new FezVertexPositionColor(p, new Color(colors[i].R, colors[i].G, colors[i].B, 0)),
					new FezVertexPositionColor(p, new Color(colors[i].R, colors[i].G, colors[i].B, 255))
				}).ToArray(), points.SelectMany((Vector3 p, int i) => new int[2]
				{
					i * 2,
					i * 2 + 1
				}).ToArray(), PrimitiveType.LineList)
			};
		}
		Groups.Add(group);
		return group;
	}

	public void Dispose()
	{
		Dispose(disposeEffect: true);
	}

	public void Dispose(bool disposeEffect)
	{
		foreach (Group group in Groups)
		{
			if (group.Geometry is IDisposable)
			{
				(group.Geometry as IDisposable).Dispose();
			}
			if (group.Geometry is IFakeDisposable)
			{
				(group.Geometry as IFakeDisposable).Dispose();
			}
		}
		if (disposeEffect)
		{
			Effect.Dispose();
		}
	}
}
