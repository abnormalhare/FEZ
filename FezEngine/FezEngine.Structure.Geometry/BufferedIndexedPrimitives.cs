using System;
using FezEngine.Effects;
using FezEngine.Tools;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

public class BufferedIndexedPrimitives<T> : IndexedPrimitiveCollectionBase<T, int>, IDisposable where T : struct, IVertexType
{
	private VertexBuffer vertexBuffer;

	private IndexBuffer indexBuffer;

	private int vertexCount;

	private int pendingUpdates;

	public BufferedIndexedPrimitives(PrimitiveType type)
		: this((T[])null, (int[])null, type)
	{
	}

	public BufferedIndexedPrimitives(T[] vertices, int[] indices, PrimitiveType type)
		: base(type)
	{
		base.vertices = vertices ?? new T[0];
		base.Indices = indices ?? new int[0];
	}

	public void UpdateBuffers()
	{
		vertexCount = base.VertexCount;
		if (vertexBuffer != null)
		{
			vertexBuffer.Dispose();
			vertexBuffer = null;
		}
		if (indexBuffer != null)
		{
			indexBuffer.Dispose();
			indexBuffer = null;
		}
		pendingUpdates++;
		DrawActionScheduler.Schedule(delegate
		{
			vertexBuffer = new VertexBuffer(device, typeof(T), vertexCount, BufferUsage.WriteOnly);
			indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
			vertexBuffer.SetData(vertices);
			indexBuffer.SetData(indices);
			pendingUpdates--;
		});
	}

	public void CleanUp()
	{
		if (pendingUpdates > 0)
		{
			DrawActionScheduler.Schedule(delegate
			{
				indices = null;
				vertices = null;
			});
		}
		else
		{
			indices = null;
			vertices = null;
		}
	}

	public void Dispose()
	{
		CleanUp();
		if (indexBuffer != null)
		{
			indexBuffer.Dispose();
		}
		indexBuffer = null;
		if (vertexBuffer != null)
		{
			vertexBuffer.Dispose();
		}
		vertexBuffer = null;
	}

	public override void Draw(BaseEffect effect)
	{
		if (device != null && primitiveCount > 0 && indexBuffer != null && vertexBuffer != null)
		{
			device.SetVertexBuffer(vertexBuffer);
			device.Indices = indexBuffer;
			effect.Apply();
			device.DrawIndexedPrimitives(primitiveType, 0, 0, vertexCount, 0, primitiveCount);
		}
	}

	public override IIndexedPrimitiveCollection Clone()
	{
		throw new NotImplementedException();
	}
}
