using System;
using System.Collections.Generic;
using System.Linq;
using ContentSerialization;
using ContentSerialization.Attributes;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class TrixelSurface : IDeserializationCallback
{
	private struct TrixelToTraverse
	{
		public TrixelEmplacement Trixel;

		public FaceOrientation? Except;
	}

	private Vector3 normal;

	private FaceOrientation tangentFace;

	private FaceOrientation bitangentFace;

	private FaceOrientation[] tangentFaces;

	private int depth;

	public FaceOrientation Orientation { get; set; }

	[Serialization(Name = "parts", CollectionItemName = "part")]
	public List<RectangularTrixelSurfacePart> RectangularParts { get; set; }

	public Vector3 Tangent { get; private set; }

	public Vector3 Bitangent { get; private set; }

	public HashSet<TrixelEmplacement> Trixels { get; private set; }

	public bool Dirty { get; private set; }

	public TrixelSurface()
	{
	}

	public TrixelSurface(FaceOrientation orientation, TrixelEmplacement firstTrixel)
	{
		RectangularParts = new List<RectangularTrixelSurfacePart>();
		Orientation = orientation;
		Initialize();
		Trixels.Add(firstTrixel);
		MarkAsDirty();
		InitializeDepth();
	}

	public void OnDeserialization()
	{
		Initialize();
		RebuildFromParts();
		InitializeDepth();
	}

	private void Initialize()
	{
		Trixels = new HashSet<TrixelEmplacement>();
		tangentFace = Orientation.GetTangent();
		bitangentFace = Orientation.GetBitangent();
		tangentFaces = new FaceOrientation[4]
		{
			tangentFace,
			bitangentFace,
			tangentFace.GetOpposite(),
			bitangentFace.GetOpposite()
		};
		normal = Orientation.AsVector();
		Tangent = tangentFace.AsVector();
		Bitangent = bitangentFace.AsVector();
	}

	private void InitializeDepth()
	{
		depth = (int)Vector3.Dot(Trixels.First().Position, normal);
	}

	public bool CanContain(TrixelEmplacement trixel, FaceOrientation face)
	{
		if (face != Orientation || (int)Vector3.Dot(trixel.Position, normal) != depth)
		{
			return false;
		}
		if (Trixels.Contains(trixel + Tangent) || Trixels.Contains(trixel + Bitangent) || Trixels.Contains(trixel - Tangent) || Trixels.Contains(trixel - Bitangent))
		{
			return true;
		}
		return false;
	}

	public void MarkAsDirty()
	{
		Dirty = true;
	}

	public void RebuildFromParts()
	{
		foreach (RectangularTrixelSurfacePart rectangularPart in RectangularParts)
		{
			rectangularPart.Orientation = Orientation;
			for (int i = 0; i < rectangularPart.TangentSize; i++)
			{
				for (int j = 0; j < rectangularPart.BitangentSize; j++)
				{
					Trixels.Add(new TrixelEmplacement(rectangularPart.Start + Tangent * i + Bitangent * j));
				}
			}
		}
		MarkAsDirty();
	}

	public void RebuildParts()
	{
		Dirty = false;
		RectangularParts.Clear();
		Queue<HashSet<TrixelEmplacement>> queue = new Queue<HashSet<TrixelEmplacement>>();
		if (Trixels.Count > 0)
		{
			queue.Enqueue(new HashSet<TrixelEmplacement>(Trixels));
		}
		while (queue.Count > 0)
		{
			HashSet<TrixelEmplacement> hashSet = queue.Dequeue();
			TrixelEmplacement trixelEmplacement = default(TrixelEmplacement);
			foreach (TrixelEmplacement item in hashSet)
			{
				trixelEmplacement.Position += item.Position;
			}
			trixelEmplacement.Position = (trixelEmplacement.Position / hashSet.Count).Floor();
			if (Vector3.Dot(trixelEmplacement.Position, normal) != (float)depth)
			{
				trixelEmplacement.Position = trixelEmplacement.Position * (Vector3.One - normal.Abs()) + depth * normal;
			}
			if (!hashSet.Contains(trixelEmplacement))
			{
				trixelEmplacement = FindNearestTrixel(trixelEmplacement, hashSet);
			}
			Rectangle rectangle;
			List<TrixelEmplacement> other = FindBiggestRectangle(trixelEmplacement, hashSet, out rectangle);
			rectangle.Offset((int)Vector3.Dot(trixelEmplacement.Position, Tangent), (int)Vector3.Dot(trixelEmplacement.Position, Bitangent));
			RectangularParts.Add(new RectangularTrixelSurfacePart
			{
				Orientation = Orientation,
				Start = new TrixelEmplacement(rectangle.X * Tangent + rectangle.Y * Bitangent + depth * normal),
				TangentSize = rectangle.Width,
				BitangentSize = rectangle.Height
			});
			hashSet.ExceptWith(other);
			while (hashSet.Count > 0)
			{
				TrixelEmplacement origin = hashSet.First();
				HashSet<TrixelEmplacement> hashSet2 = TraverseSurface(origin, hashSet);
				queue.Enqueue(hashSet2);
				if (hashSet.Count == hashSet2.Count)
				{
					hashSet.Clear();
				}
				else
				{
					hashSet.ExceptWith(hashSet2);
				}
			}
		}
	}

	public bool AnyRectangleContains(TrixelEmplacement trixel)
	{
		foreach (RectangularTrixelSurfacePart rectangularPart in RectangularParts)
		{
			Vector3 vector = trixel.Position - rectangularPart.Start.Position;
			if (vector.X >= 0f && vector.Y >= 0f && vector.Z >= 0f)
			{
				int num = (int)Vector3.Dot(vector, Tangent);
				int num2 = (int)Vector3.Dot(vector, Bitangent);
				if (num < rectangularPart.TangentSize && num2 < rectangularPart.BitangentSize)
				{
					return true;
				}
			}
		}
		return false;
	}

	private HashSet<TrixelEmplacement> TraverseSurface(TrixelEmplacement origin, ICollection<TrixelEmplacement> subSurface)
	{
		HashSet<TrixelEmplacement> hashSet = new HashSet<TrixelEmplacement> { origin };
		Queue<TrixelToTraverse> queue = new Queue<TrixelToTraverse>();
		queue.Enqueue(new TrixelToTraverse
		{
			Trixel = origin
		});
		while (queue.Count != 0)
		{
			TrixelToTraverse toTraverse = queue.Dequeue();
			TrixelEmplacement trixel = toTraverse.Trixel;
			foreach (FaceOrientation item in tangentFaces.Where((FaceOrientation x) => !toTraverse.Except.HasValue || toTraverse.Except.Value != x))
			{
				TrixelEmplacement traversal = trixel.GetTraversal(item);
				while (!hashSet.Contains(traversal) && subSurface.Contains(traversal))
				{
					hashSet.Add(traversal);
					if (hashSet.Count == subSurface.Count)
					{
						return hashSet;
					}
					queue.Enqueue(new TrixelToTraverse
					{
						Trixel = traversal,
						Except = item
					});
					traversal = traversal.GetTraversal(item);
				}
			}
		}
		return hashSet;
	}

	private List<TrixelEmplacement> FindBiggestRectangle(TrixelEmplacement center, ICollection<TrixelEmplacement> subSurface, out Rectangle rectangle)
	{
		List<TrixelEmplacement> list = new List<TrixelEmplacement>();
		TrixelEmplacement trixelEmplacement = new TrixelEmplacement(center);
		int num = 1;
		int num2 = 0;
		int num3 = 1;
		int num4 = 0;
		int num5 = 1;
		int num6 = -1;
		do
		{
			list.Add(new TrixelEmplacement(trixelEmplacement));
			if (num3 > 0)
			{
				trixelEmplacement.Position += Tangent * num5;
				if (--num3 == 0)
				{
					num6 *= -1;
					num4 = ++num2;
				}
			}
			else if (num4 > 0)
			{
				trixelEmplacement.Position += Bitangent * num6;
				if (--num4 == 0)
				{
					num5 *= -1;
					num3 = ++num;
				}
			}
		}
		while (subSurface.Contains(trixelEmplacement));
		int num7 = ClampToRectangleSpiral(list.Count);
		if (num7 != list.Count)
		{
			list.RemoveRange(num7, list.Count - num7);
		}
		rectangle = GetRectangleSpiralLimits(num7);
		if (list.Count < subSurface.Count)
		{
			ExpandSide(ref rectangle, center, subSurface, list, useTangent: true, 1);
			ExpandSide(ref rectangle, center, subSurface, list, useTangent: true, -1);
			ExpandSide(ref rectangle, center, subSurface, list, useTangent: false, 1);
			ExpandSide(ref rectangle, center, subSurface, list, useTangent: false, -1);
		}
		return list;
	}

	private static int ClampToRectangleSpiral(int trixelCount)
	{
		int num = (int)Math.Floor(Math.Sqrt(trixelCount));
		int num2 = num * num;
		int num3 = num2 + num;
		if (num3 >= trixelCount)
		{
			return num2;
		}
		return num3;
	}

	private static Rectangle GetRectangleSpiralLimits(int trixelCount)
	{
		double num = Math.Sqrt(trixelCount);
		int num2 = (int)Math.Floor(num);
		Point point = default(Point);
		point.X = (point.Y = (int)Math.Floor(num / 2.0) + 1);
		Point point2 = default(Point);
		point2.X = (point2.Y = (int)Math.Ceiling((0.0 - (num - 1.0)) / 2.0));
		if ((double)num2 != num)
		{
			if (num % 2.0 == 0.0)
			{
				point2.X--;
			}
			else
			{
				point.X++;
			}
		}
		return new Rectangle(point2.X, point2.Y, point.X - point2.X, point.Y - point2.Y);
	}

	private void ExpandSide(ref Rectangle rectangle, TrixelEmplacement center, ICollection<TrixelEmplacement> subSurface, List<TrixelEmplacement> rectangleTrixels, bool useTangent, int sign)
	{
		TrixelEmplacement other = center + rectangle.X * Tangent + rectangle.Y * Bitangent;
		if (sign > 0)
		{
			other += (useTangent ? (Tangent * (rectangle.Width - 1)) : (Bitangent * (rectangle.Height - 1)));
		}
		int num = (useTangent ? rectangle.Height : rectangle.Width);
		bool flag;
		do
		{
			other.Position += (useTangent ? Tangent : Bitangent) * sign;
			TrixelEmplacement trixelEmplacement = new TrixelEmplacement(other);
			int num2 = 0;
			flag = subSurface.Contains(trixelEmplacement);
			while (flag)
			{
				rectangleTrixels.Add(new TrixelEmplacement(trixelEmplacement));
				if (++num2 == num)
				{
					break;
				}
				trixelEmplacement.Position += (useTangent ? Bitangent : Tangent);
				flag = subSurface.Contains(trixelEmplacement);
			}
			if (flag)
			{
				if (useTangent)
				{
					if (sign < 0)
					{
						rectangle.X--;
					}
					rectangle.Width++;
				}
				else
				{
					if (sign < 0)
					{
						rectangle.Y--;
					}
					rectangle.Height++;
				}
			}
			else if (num2 > 0)
			{
				rectangleTrixels.RemoveRange(rectangleTrixels.Count - num2, num2);
			}
		}
		while (flag);
	}

	private TrixelEmplacement FindNearestTrixel(TrixelEmplacement center, ICollection<TrixelEmplacement> subSurface)
	{
		TrixelEmplacement trixelEmplacement = new TrixelEmplacement(center);
		int num = 1;
		int num2 = 0;
		int num3 = 1;
		int num4 = 0;
		int num5 = 1;
		int num6 = -1;
		do
		{
			if (num3 > 0)
			{
				trixelEmplacement.Position += Tangent * num5;
				if (--num3 == 0)
				{
					num6 *= -1;
					num4 = ++num2;
				}
			}
			else if (num4 > 0)
			{
				trixelEmplacement.Position += Bitangent * num6;
				if (--num4 == 0)
				{
					num5 *= -1;
					num3 = ++num;
				}
			}
		}
		while (!subSurface.Contains(trixelEmplacement));
		return trixelEmplacement;
	}
}
