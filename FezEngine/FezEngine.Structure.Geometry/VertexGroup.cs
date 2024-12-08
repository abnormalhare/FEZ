using System;
using System.Collections.Generic;
using FezEngine.Tools;

namespace FezEngine.Structure.Geometry;

public class VertexGroup<T> where T : struct, IEquatable<T>, IVertex
{
	private static Dictionary<T, SharedVertex<T>> vertexPresence;

	private static bool allocated;

	private static Pool<SharedVertex<T>> vertexPool;

	public ICollection<SharedVertex<T>> Vertices => vertexPresence.Values;

	public VertexGroup()
		: this(0)
	{
	}

	public VertexGroup(int capacity)
	{
		if (!allocated)
		{
			vertexPresence = new Dictionary<T, SharedVertex<T>>(capacity);
			vertexPool = new Pool<SharedVertex<T>>(capacity);
			allocated = true;
			return;
		}
		foreach (SharedVertex<T> value in vertexPresence.Values)
		{
			vertexPool.Return(value);
		}
		vertexPresence.Clear();
	}

	public void Dereference(SharedVertex<T> sv)
	{
		if (sv.References == 1)
		{
			vertexPresence.Remove(sv.Vertex);
		}
		else
		{
			sv.References--;
		}
	}

	public SharedVertex<T> Reference(T vertex)
	{
		if (!vertexPresence.TryGetValue(vertex, out var value))
		{
			value = vertexPool.Take();
			value.Vertex = vertex;
			vertexPresence.Add(vertex, value);
		}
		value.References++;
		return value;
	}

	public static void Deallocate()
	{
		vertexPresence = null;
		vertexPool = null;
		allocated = false;
	}
}
