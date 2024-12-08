using System;
using FezEngine.Effects;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Structure.Geometry;

public class ShaderInstancedIndexedPrimitives<TemplateType, InstanceType> : IndexedPrimitiveCollectionBase<TemplateType, int>, IFakeDisposable where TemplateType : struct, IShaderInstantiatableVertex where InstanceType : struct
{
	public int PredictiveBatchSize = 16;

	private readonly int InstancesPerBatch;

	private VertexBuffer vertexBuffer;

	private IndexBuffer indexBuffer;

	private DynamicVertexBuffer instanceBuffer;

	public InstanceType[] Instances;

	public int InstanceCount;

	public bool InstancesDirty;

	private int oldInstanceCount;

	private int[] tempIndices = new int[0];

	private TemplateType[] tempVertices = new TemplateType[0];

	private VertexDeclaration vertexDeclaration;

	private bool appendIndex;

	private IndexedVector4[] indexedInstances = new IndexedVector4[0];

	private bool useHwInstancing;

	public override TemplateType[] Vertices
	{
		get
		{
			return base.Vertices;
		}
		set
		{
			base.Vertices = value;
			UpdateBuffers(rebuild: true);
		}
	}

	public override int[] Indices
	{
		get
		{
			return base.Indices;
		}
		set
		{
			base.Indices = value;
			UpdateBuffers(rebuild: true);
		}
	}

	public bool IsDisposed { get; private set; }

	public ShaderInstancedIndexedPrimitives(PrimitiveType type, int instancesPerBatch, bool appendIndex = false)
		: base(type)
	{
		InstancesPerBatch = instancesPerBatch;
		this.appendIndex = appendIndex;
		RefreshInstancingMode(force: true);
		BaseEffect.InstancingModeChanged += RefreshInstancingModeInternal;
	}

	private void RefreshInstancingModeInternal()
	{
		RefreshInstancingMode();
	}

	private void RefreshInstancingMode(bool force = false, bool skipUpdate = false)
	{
		if (!force && useHwInstancing == BaseEffect.UseHardwareInstancing)
		{
			return;
		}
		if (vertexDeclaration != null)
		{
			vertexDeclaration.Dispose();
			vertexDeclaration = null;
		}
		useHwInstancing = BaseEffect.UseHardwareInstancing;
		if (useHwInstancing)
		{
			if (typeof(InstanceType) == typeof(Matrix))
			{
				vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2), new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 3), new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 4), new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 5));
			}
			else
			{
				if (!(typeof(InstanceType) == typeof(Vector4)))
				{
					throw new InvalidOperationException("Unsupported instance size!");
				}
				if (appendIndex)
				{
					vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2), new VertexElement(16, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 3));
				}
				else
				{
					vertexDeclaration = new VertexDeclaration(new VertexElement(0, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2));
				}
			}
		}
		if (!skipUpdate)
		{
			ResetBuffers();
			UpdateBuffers(rebuild: true);
		}
	}

	public void MaximizeBuffers(int maxInstances)
	{
		int instanceCount = InstanceCount;
		InstanceCount = maxInstances;
		UpdateBuffers();
		InstanceCount = instanceCount;
	}

	public void ResetBuffers()
	{
		DrawActionScheduler.Schedule(delegate
		{
			oldInstanceCount = 0;
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
			if (instanceBuffer != null)
			{
				instanceBuffer.Dispose();
			}
			instanceBuffer = null;
			Array.Resize(ref tempIndices, 0);
			Array.Resize(ref tempVertices, 0);
			Array.Resize(ref indexedInstances, 0);
		});
	}

	public void Dispose()
	{
		ResetBuffers();
		BaseEffect.InstancingModeChanged -= RefreshInstancingModeInternal;
		IsDisposed = true;
	}

	private void Rehydrate(bool skipUpdate)
	{
		IsDisposed = false;
		RefreshInstancingMode(force: true, skipUpdate);
		BaseEffect.InstancingModeChanged += RefreshInstancingModeInternal;
	}

	public void UpdateBuffers()
	{
		UpdateBuffers(rebuild: false);
	}

	private void UpdateBuffers(bool rebuild)
	{
		if (IsDisposed)
		{
			Rehydrate(skipUpdate: true);
		}
		if (device == null || vertices == null || vertices.Length == 0 || indices == null || indices.Length == 0 || Instances == null || InstanceCount <= 0)
		{
			return;
		}
		int num = (int)Math.Ceiling((double)oldInstanceCount / (double)PredictiveBatchSize) * PredictiveBatchSize;
		int batchCeiling = (int)Math.Ceiling((double)InstanceCount / (double)PredictiveBatchSize) * PredictiveBatchSize;
		bool newInstanceBatch = batchCeiling > num;
		bool flag = vertexBuffer == null || rebuild;
		if (!useHwInstancing)
		{
			flag = flag || newInstanceBatch;
		}
		if (flag)
		{
			int vertexCount = (useHwInstancing ? vertices.Length : (batchCeiling * vertices.Length));
			DrawActionScheduler.Schedule(delegate
			{
				if (vertexBuffer != null)
				{
					vertexBuffer.Dispose();
					vertexBuffer = null;
				}
				if (useHwInstancing)
				{
					vertexBuffer = new VertexBuffer(device, typeof(TemplateType), vertexCount, BufferUsage.WriteOnly);
					vertexBuffer.SetData(vertices);
				}
				else
				{
					vertexBuffer = new DynamicVertexBuffer(device, typeof(TemplateType), vertexCount, BufferUsage.WriteOnly);
				}
			});
		}
		bool flag2 = indexBuffer == null || rebuild;
		if (!useHwInstancing)
		{
			flag2 = flag2 || newInstanceBatch;
		}
		if (flag2)
		{
			int indexCount = (useHwInstancing ? indices.Length : (batchCeiling * indices.Length));
			DrawActionScheduler.Schedule(delegate
			{
				if (indexBuffer != null)
				{
					indexBuffer.Dispose();
					indexBuffer = null;
				}
				if (useHwInstancing)
				{
					indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, indexCount, BufferUsage.WriteOnly);
					indexBuffer.SetData(indices);
				}
				else
				{
					indexBuffer = new DynamicIndexBuffer(device, IndexElementSize.ThirtyTwoBits, indexCount, BufferUsage.WriteOnly);
				}
			});
		}
		bool newInstances = InstanceCount > oldInstanceCount;
		if (rebuild || newInstanceBatch)
		{
			oldInstanceCount = 0;
		}
		int newInstanceCount = InstanceCount;
		if (!useHwInstancing)
		{
			DrawActionScheduler.Schedule(delegate
			{
				if (rebuild || newInstanceBatch)
				{
					Array.Resize(ref tempIndices, batchCeiling * indices.Length);
					Array.Resize(ref tempVertices, batchCeiling * vertices.Length);
				}
				if (oldInstanceCount == 0)
				{
					Array.Copy(indices, tempIndices, indices.Length);
				}
				for (int i = oldInstanceCount; i < newInstanceCount; i++)
				{
					int num2 = vertices.Length * i;
					Array.Copy(vertices, 0, tempVertices, num2, vertices.Length);
					for (int j = 0; j < vertices.Length; j++)
					{
						tempVertices[num2 + j].InstanceIndex = i;
					}
					if (i != 0)
					{
						int num3 = i * indices.Length;
						for (int k = 0; k < indices.Length; k++)
						{
							tempIndices[num3 + k] = indices[k] + num2;
						}
					}
				}
				if (rebuild || newInstances)
				{
					vertexBuffer.SetData(tempVertices);
					indexBuffer.SetData(tempIndices);
				}
				oldInstanceCount = newInstanceCount;
			});
		}
		else
		{
			if (!(instanceBuffer == null || rebuild || newInstanceBatch))
			{
				return;
			}
			DrawActionScheduler.Schedule(delegate
			{
				if (instanceBuffer != null)
				{
					instanceBuffer.Dispose();
					instanceBuffer = null;
				}
				instanceBuffer = new DynamicVertexBuffer(device, vertexDeclaration, batchCeiling, BufferUsage.WriteOnly);
				if (appendIndex)
				{
					Array.Resize(ref indexedInstances, batchCeiling);
				}
				oldInstanceCount = newInstanceCount;
				InstancesDirty = true;
			});
		}
	}

	public override void Draw(BaseEffect effect)
	{
		if (IsDisposed)
		{
			Rehydrate(skipUpdate: false);
		}
		if (device == null || primitiveCount <= 0 || vertices == null || vertices.Length == 0 || indexBuffer == null || vertexBuffer == null || Instances == null || InstanceCount <= 0)
		{
			return;
		}
		IShaderInstantiatableEffect<InstanceType> shaderInstantiatableEffect = effect as IShaderInstantiatableEffect<InstanceType>;
		if (useHwInstancing)
		{
			device.SetVertexBuffers(vertexBuffer, new VertexBufferBinding(instanceBuffer, 0, 1));
		}
		else
		{
			device.SetVertexBuffer(vertexBuffer);
		}
		device.Indices = indexBuffer;
		if (useHwInstancing)
		{
			int num = Math.Min(InstanceCount, instanceBuffer.VertexCount);
			if (InstancesDirty)
			{
				if (appendIndex)
				{
					for (int i = 0; i < num; i++)
					{
						indexedInstances[i].Data = __refvalue(__makeref(Instances[i]), Vector4);
						indexedInstances[i].Index = i;
					}
					instanceBuffer.SetData(indexedInstances, 0, num);
				}
				else
				{
					instanceBuffer.SetData(Instances, 0, num);
				}
				InstancesDirty = false;
			}
			effect.Apply();
			device.DrawInstancedPrimitives(primitiveType, 0, 0, vertices.Length, 0, primitiveCount, num);
		}
		else
		{
			int num2 = InstanceCount;
			while (num2 > 0)
			{
				int num3 = Math.Min(num2, InstancesPerBatch);
				int start = InstanceCount - num2;
				shaderInstantiatableEffect.SetInstanceData(Instances, start, num3);
				effect.Apply();
				device.DrawIndexedPrimitives(primitiveType, 0, 0, num3 * vertices.Length, 0, num3 * primitiveCount);
				num2 -= num3;
			}
		}
	}

	public override IIndexedPrimitiveCollection Clone()
	{
		return new ShaderInstancedIndexedPrimitives<TemplateType, InstanceType>(primitiveType, InstancesPerBatch)
		{
			Vertices = Vertices,
			Indices = Indices
		};
	}
}
