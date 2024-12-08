using FezEngine.Effects;
using FezEngine.Tools;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

public abstract class IndexedPrimitiveCollectionBase<VertexType, IndexType> : IIndexedPrimitiveCollection
{
	protected readonly GraphicsDevice device;

	protected readonly IGraphicsDeviceService GraphicsDeviceService;

	protected VertexType[] vertices;

	protected IndexType[] indices;

	protected PrimitiveType primitiveType;

	protected int primitiveCount;

	public PrimitiveType PrimitiveType
	{
		get
		{
			return primitiveType;
		}
		set
		{
			primitiveType = value;
			UpdatePrimitiveCount();
		}
	}

	public virtual VertexType[] Vertices
	{
		get
		{
			return vertices;
		}
		set
		{
			vertices = value;
		}
	}

	public virtual IndexType[] Indices
	{
		get
		{
			return indices;
		}
		set
		{
			indices = value;
			UpdatePrimitiveCount();
		}
	}

	public int VertexCount => vertices.Length;

	public bool Empty => primitiveCount == 0;

	private IndexedPrimitiveCollectionBase()
	{
		if (ServiceHelper.IsFull)
		{
			GraphicsDeviceService = ServiceHelper.Get<IGraphicsDeviceService>();
		}
	}

	protected IndexedPrimitiveCollectionBase(PrimitiveType type)
		: this()
	{
		primitiveType = type;
		if (GraphicsDeviceService != null)
		{
			device = GraphicsDeviceService.GraphicsDevice;
		}
	}

	protected void UpdatePrimitiveCount()
	{
		primitiveCount = indices.Length;
		switch (primitiveType)
		{
		case PrimitiveType.LineList:
			primitiveCount /= 2;
			break;
		case PrimitiveType.LineStrip:
			primitiveCount--;
			break;
		case PrimitiveType.TriangleList:
			primitiveCount /= 3;
			break;
		case PrimitiveType.TriangleStrip:
			primitiveCount -= 2;
			break;
		}
	}

	public abstract void Draw(BaseEffect effect);

	public abstract IIndexedPrimitiveCollection Clone();
}
