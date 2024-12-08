using System.Collections.Generic;
using System.Linq;
using ContentSerialization;
using ContentSerialization.Attributes;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class TrixelCluster : ISpatialStructure<TrixelEmplacement>, IDeserializationCallback
{
	public class Chunk
	{
		private struct TrixelToVisit
		{
			public TrixelEmplacement Trixel;

			public Vector3? Except;
		}

		[Serialization(Optional = true)]
		public List<Box> Boxes { get; set; }

		[Serialization(Optional = true, CollectionItemName = "content")]
		public HashSet<TrixelEmplacement> Trixels { get; set; }

		[Serialization(Ignore = true)]
		internal bool Dirty { get; set; }

		internal bool Empty
		{
			get
			{
				if (Boxes.Count == 0)
				{
					return Trixels.Count == 0;
				}
				return false;
			}
		}

		public Chunk()
		{
			Boxes = new List<Box>();
			Trixels = new HashSet<TrixelEmplacement>();
		}

		internal bool IsNeighbor(Box box)
		{
			if (Boxes.Any((Box b) => b.IsNeighbor(box)))
			{
				return true;
			}
			return Trixels.Any(box.IsNeighbor);
		}

		internal bool IsNeighbor(TrixelEmplacement trixel)
		{
			if (Boxes.Any((Box b) => b.IsNeighbor(trixel)))
			{
				return true;
			}
			return Trixels.Any((TrixelEmplacement t) => t.IsNeighbor(trixel));
		}

		internal bool Contains(TrixelEmplacement trixel)
		{
			for (int i = 0; i < Boxes.Count; i++)
			{
				if (Boxes[i].Contains(trixel))
				{
					return true;
				}
			}
			return Trixels.Contains(trixel);
		}

		internal bool TryAdd(TrixelEmplacement trixel)
		{
			bool flag = false;
			if (Boxes.Count > 0)
			{
				Box[] array = Boxes.Where((Box b) => b.IsNeighbor(trixel)).ToArray();
				foreach (Box box in array)
				{
					flag = true;
					Dismantle(box);
					Boxes.Remove(box);
				}
			}
			if (!flag)
			{
				foreach (TrixelEmplacement trixel2 in Trixels)
				{
					if (trixel2.IsNeighbor(trixel))
					{
						flag = true;
						break;
					}
				}
			}
			if (flag)
			{
				Trixels.Add(trixel);
			}
			Dirty |= flag;
			return flag;
		}

		internal bool TryRemove(TrixelEmplacement trixel)
		{
			bool flag = false;
			foreach (Box item in Boxes.Where((Box b) => b.Contains(trixel) || b.IsNeighbor(trixel)))
			{
				flag = true;
				Dismantle(item);
			}
			if (flag)
			{
				Boxes.RemoveAll((Box b) => b.Contains(trixel) || b.IsNeighbor(trixel));
			}
			flag |= Trixels.Remove(trixel);
			Dirty |= flag;
			return flag;
		}

		private void Dismantle(Box box)
		{
			for (int i = box.Start.X; i < box.End.X; i++)
			{
				for (int j = box.Start.Y; j < box.End.Y; j++)
				{
					for (int k = box.Start.Z; k < box.End.Z; k++)
					{
						Trixels.Add(new TrixelEmplacement(i, j, k));
					}
				}
			}
		}

		internal void ConsolidateTrixels()
		{
			if (Trixels.Count <= 1)
			{
				return;
			}
			Stack<HashSet<TrixelEmplacement>> stack = new Stack<HashSet<TrixelEmplacement>>();
			stack.Push(new HashSet<TrixelEmplacement>(Trixels));
			while (stack.Count > 0)
			{
				HashSet<TrixelEmplacement> hashSet = stack.Pop();
				TrixelEmplacement trixelEmplacement = default(TrixelEmplacement);
				foreach (TrixelEmplacement item in hashSet)
				{
					trixelEmplacement.Offset(item.X, item.Y, item.Z);
				}
				trixelEmplacement.Position = (trixelEmplacement.Position / hashSet.Count).Round(3).Floor();
				if (!hashSet.Contains(trixelEmplacement))
				{
					trixelEmplacement = hashSet.First();
				}
				Box box;
				List<TrixelEmplacement> other = FindBiggestBox(trixelEmplacement, hashSet, out box);
				Boxes.Add(box);
				hashSet.ExceptWith(other);
				Trixels.ExceptWith(other);
				while (hashSet.Count > 1)
				{
					HashSet<TrixelEmplacement> hashSet2 = VisitChunk(hashSet.First(), hashSet);
					stack.Push(hashSet2);
					hashSet.ExceptWith(hashSet2);
				}
			}
			Dirty = false;
		}

		private static List<TrixelEmplacement> FindBiggestBox(TrixelEmplacement center, ICollection<TrixelEmplacement> subChunk, out Box box)
		{
			List<TrixelEmplacement> list = new List<TrixelEmplacement> { center };
			box = new Box
			{
				Start = center,
				End = center
			};
			int trixelsToRollback;
			do
			{
				box.Start -= Vector3.One;
				box.End += Vector3.One;
				trixelsToRollback = 0;
			}
			while (TestFace(subChunk, list, Vector3.UnitZ, partial: false, box, ref trixelsToRollback) && TestFace(subChunk, list, -Vector3.UnitZ, partial: false, box, ref trixelsToRollback) && TestFace(subChunk, list, Vector3.UnitX, partial: true, box, ref trixelsToRollback) && TestFace(subChunk, list, -Vector3.UnitX, partial: true, box, ref trixelsToRollback) && TestFace(subChunk, list, Vector3.UnitY, partial: true, box, ref trixelsToRollback) && TestFace(subChunk, list, -Vector3.UnitY, partial: true, box, ref trixelsToRollback));
			list.RemoveRange(list.Count - trixelsToRollback, trixelsToRollback);
			box.Start += Vector3.One;
			box.End -= Vector3.One;
			if (list.Count < subChunk.Count)
			{
				Vector3[] directions = Directions;
				foreach (Vector3 normal in directions)
				{
					ExpandSide(box, subChunk, list, normal);
				}
			}
			box.End += Vector3.One;
			return list;
		}

		private static void ExpandSide(Box box, ICollection<TrixelEmplacement> subChunk, List<TrixelEmplacement> boxTrixels, Vector3 normal)
		{
			int trixelsToRollback = 0;
			bool flag = Vector3.Dot(normal, Vector3.One) > 0f;
			if (flag)
			{
				box.End += normal;
			}
			else
			{
				box.Start += normal;
			}
			while (TestFace(subChunk, boxTrixels, normal, partial: false, box, ref trixelsToRollback))
			{
				if (flag)
				{
					box.End += normal;
				}
				else
				{
					box.Start += normal;
				}
				trixelsToRollback = 0;
			}
			boxTrixels.RemoveRange(boxTrixels.Count - trixelsToRollback, trixelsToRollback);
			if (flag)
			{
				box.End -= normal;
			}
			else
			{
				box.Start -= normal;
			}
		}

		private static bool TestFace(ICollection<TrixelEmplacement> subChunk, ICollection<TrixelEmplacement> boxTrixels, Vector3 normal, bool partial, Box box, ref int trixelsToRollback)
		{
			Vector3 vector = normal.Abs();
			Vector3 vector2 = ((normal.Z != 0f) ? Vector3.UnitX : Vector3.UnitZ);
			Vector3 vector3 = ((normal.Z != 0f) ? Vector3.UnitY : (new Vector3(1f, 1f, 0f) - vector));
			Vector3 vector4 = ((Vector3.Dot(normal, Vector3.One) > 0f) ? box.End.Position : box.Start.Position) * vector;
			int num = (int)Vector3.Dot(box.Start.Position, vector2);
			int num2 = (int)Vector3.Dot(box.End.Position, vector2);
			int num3 = (int)Vector3.Dot(box.Start.Position, vector3);
			int num4 = (int)Vector3.Dot(box.End.Position, vector3);
			if (partial)
			{
				num++;
				num2--;
			}
			for (int i = num; i <= num2; i++)
			{
				for (int j = num3; j <= num4; j++)
				{
					TrixelEmplacement item = new TrixelEmplacement(i * vector2 + j * vector3 + vector4);
					if (!subChunk.Contains(item))
					{
						return false;
					}
					trixelsToRollback++;
					boxTrixels.Add(item);
				}
			}
			return true;
		}

		private static HashSet<TrixelEmplacement> VisitChunk(TrixelEmplacement origin, ICollection<TrixelEmplacement> subChunk)
		{
			HashSet<TrixelEmplacement> hashSet = new HashSet<TrixelEmplacement> { origin };
			Queue<TrixelToVisit> queue = new Queue<TrixelToVisit>();
			queue.Enqueue(new TrixelToVisit
			{
				Trixel = origin
			});
			while (queue.Count != 0)
			{
				TrixelToVisit toTraverse = queue.Dequeue();
				TrixelEmplacement trixel = toTraverse.Trixel;
				foreach (Vector3 item in Directions.Where((Vector3 x) => !toTraverse.Except.HasValue || toTraverse.Except.Value != x))
				{
					for (TrixelEmplacement trixelEmplacement = trixel + item; !hashSet.Contains(trixelEmplacement) && subChunk.Contains(trixelEmplacement); trixelEmplacement += item)
					{
						hashSet.Add(trixelEmplacement);
						if (hashSet.Count == subChunk.Count)
						{
							return hashSet;
						}
						queue.Enqueue(new TrixelToVisit
						{
							Trixel = trixelEmplacement,
							Except = item
						});
					}
				}
			}
			return hashSet;
		}
	}

	public class Box
	{
		public TrixelEmplacement Start { get; set; }

		public TrixelEmplacement End { get; set; }

		internal IEnumerable<TrixelEmplacement> Cells
		{
			get
			{
				for (int x = Start.X; x < End.X; x++)
				{
					for (int y = Start.Y; y < End.Y; y++)
					{
						for (int z = Start.Z; z < End.Z; z++)
						{
							yield return new TrixelEmplacement(x, y, z);
						}
					}
				}
			}
		}

		internal bool Contains(TrixelEmplacement trixel)
		{
			if (trixel.X >= Start.X && trixel.Y >= Start.Y && trixel.Z >= Start.Z && trixel.X < End.X && trixel.Y < End.Y)
			{
				return trixel.Z < End.Z;
			}
			return false;
		}

		internal bool IsNeighbor(Box other)
		{
			BoundingBox boundingBox = new BoundingBox(Start.Position, End.Position);
			BoundingBox box = new BoundingBox(other.Start.Position, other.End.Position);
			return boundingBox.Intersects(box);
		}

		internal bool IsNeighbor(TrixelEmplacement trixel)
		{
			BoundingBox boundingBox = new BoundingBox(Start.Position, End.Position);
			BoundingBox box = new BoundingBox(trixel.Position, trixel.Position + Vector3.One);
			return boundingBox.Intersects(box);
		}
	}

	private static readonly Vector3[] Directions = new Vector3[6]
	{
		Vector3.Up,
		Vector3.Down,
		Vector3.Left,
		Vector3.Right,
		Vector3.Forward,
		Vector3.Backward
	};

	private List<Box> deserializedBoxes;

	private List<TrixelEmplacement> deserializedOrphans;

	public List<Chunk> Chunks { get; private set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public List<Box> Boxes
	{
		get
		{
			return deserializedBoxes ?? Chunks.SelectMany((Chunk c) => c.Boxes).ToList();
		}
		set
		{
			deserializedBoxes = value;
		}
	}

	[Serialization(Optional = true, CollectionItemName = "content", DefaultValueOptional = true)]
	public List<TrixelEmplacement> Orphans
	{
		get
		{
			return deserializedOrphans ?? Chunks.SelectMany((Chunk c) => c.Trixels).ToList();
		}
		set
		{
			deserializedOrphans = value;
		}
	}

	public bool Empty => Chunks.Count == 0;

	public IEnumerable<TrixelEmplacement> Cells => Chunks.SelectMany((Chunk c) => c.Trixels.Concat(c.Boxes.SelectMany((Box b) => b.Cells)));

	public void OnDeserialization()
	{
		if (deserializedOrphans != null)
		{
			foreach (TrixelEmplacement trixel in deserializedOrphans)
			{
				Chunk chunk = Chunks.FirstOrDefault((Chunk c) => c.IsNeighbor(trixel));
				if (chunk == null)
				{
					Chunks.Add(chunk = new Chunk());
				}
				chunk.Trixels.Add(trixel);
			}
			deserializedOrphans = null;
		}
		if (deserializedBoxes == null)
		{
			return;
		}
		foreach (Box box in deserializedBoxes)
		{
			Chunk chunk2 = Chunks.FirstOrDefault((Chunk c) => c.IsNeighbor(box));
			if (chunk2 == null)
			{
				Chunks.Add(chunk2 = new Chunk());
			}
			chunk2.Boxes.Add(box);
		}
		deserializedBoxes = null;
	}

	public TrixelCluster()
	{
		Chunks = new List<Chunk>();
	}

	public void Clear()
	{
		Chunks.Clear();
	}

	public void Fill(TrixelEmplacement trixel)
	{
		Fill(Enumerable.Repeat(trixel, 1));
	}

	public void Fill(IEnumerable<TrixelEmplacement> trixels)
	{
		foreach (TrixelEmplacement trixel in trixels)
		{
			if (!Chunks.Any((Chunk c) => c.TryAdd(trixel)))
			{
				Chunk chunk = new Chunk();
				Chunks.Add(chunk);
				chunk.Trixels.Add(trixel);
			}
		}
		ConsolidateTrixels();
	}

	public void FillAsChunk(IEnumerable<TrixelEmplacement> trixels)
	{
		TrixelEmplacement firstTrixel = trixels.First();
		Chunk chunk = Chunks.FirstOrDefault((Chunk c) => c.TryAdd(firstTrixel));
		if (chunk == null)
		{
			chunk = new Chunk();
			Chunks.Add(chunk);
		}
		chunk.Trixels.UnionWith(trixels);
		chunk.ConsolidateTrixels();
	}

	public void Free(TrixelEmplacement trixel)
	{
		Free(Enumerable.Repeat(trixel, 1));
	}

	public void Free(IEnumerable<TrixelEmplacement> trixels)
	{
		foreach (TrixelEmplacement trixel in trixels)
		{
			Chunks.Any((Chunk c) => c.TryRemove(trixel));
		}
		ConsolidateTrixels();
	}

	public void ConsolidateTrixels()
	{
		Chunks.RemoveAll((Chunk c) => c.Empty);
		foreach (Chunk item in Chunks.Where((Chunk c) => c.Dirty))
		{
			item.ConsolidateTrixels();
		}
	}

	public bool IsFilled(TrixelEmplacement trixel)
	{
		for (int i = 0; i < Chunks.Count; i++)
		{
			if (Chunks[i].Contains(trixel))
			{
				return true;
			}
		}
		return false;
	}
}
