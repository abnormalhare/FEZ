using System;
using ContentSerialization.Attributes;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

[TypeSerialization(FlattenToList = true)]
public struct TrixelEmplacement : IEquatable<TrixelEmplacement>, IComparable<TrixelEmplacement>
{
	public int X;

	public int Y;

	public int Z;

	[Serialization(Ignore = true)]
	public Vector3 Position
	{
		get
		{
			return new Vector3(X, Y, Z);
		}
		set
		{
			X = FezMath.Round(value.X);
			Y = FezMath.Round(value.Y);
			Z = FezMath.Round(value.Z);
		}
	}

	public TrixelEmplacement(TrixelEmplacement other)
	{
		X = other.X;
		Y = other.Y;
		Z = other.Z;
	}

	public TrixelEmplacement(Vector3 position)
	{
		X = FezMath.Round(position.X);
		Y = FezMath.Round(position.Y);
		Z = FezMath.Round(position.Z);
	}

	public TrixelEmplacement(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public void Offset(int offsetX, int offsetY, int offsetZ)
	{
		X += offsetX;
		Y += offsetY;
		Z += offsetZ;
	}

	public override bool Equals(object obj)
	{
		if (obj is TrixelEmplacement)
		{
			return Equals((TrixelEmplacement)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return X ^ (Y << 10) ^ (Z << 20);
	}

	public override string ToString()
	{
		return $"{{X:{X}, Y:{Y}, Z:{Z}}}";
	}

	public TrixelEmplacement GetTraversal(FaceOrientation face)
	{
		return new TrixelEmplacement(Position + face.AsVector());
	}

	public void TraverseInto(FaceOrientation face)
	{
		Position += face.AsVector();
	}

	public bool IsNeighbor(TrixelEmplacement other)
	{
		if (Math.Abs(X - other.X) != 1 && Math.Abs(Y - other.Y) != 1)
		{
			return Math.Abs(Z - other.Z) == 1;
		}
		return true;
	}

	public bool Equals(TrixelEmplacement other)
	{
		if ((object)other != null && other.X == X && other.Y == Y)
		{
			return other.Z == Z;
		}
		return false;
	}

	public int CompareTo(TrixelEmplacement other)
	{
		int num = X.CompareTo(other.X);
		if (num == 0)
		{
			num = Y.CompareTo(other.Y);
			if (num == 0)
			{
				num = Z.CompareTo(other.Z);
			}
		}
		return num;
	}

	public static bool operator ==(TrixelEmplacement lhs, TrixelEmplacement rhs)
	{
		return lhs.Equals(rhs);
	}

	public static bool operator !=(TrixelEmplacement lhs, TrixelEmplacement rhs)
	{
		return !(lhs == rhs);
	}

	public static TrixelEmplacement operator +(TrixelEmplacement lhs, TrixelEmplacement rhs)
	{
		return new TrixelEmplacement(lhs.Position + rhs.Position);
	}

	public static TrixelEmplacement operator -(TrixelEmplacement lhs, TrixelEmplacement rhs)
	{
		return new TrixelEmplacement(lhs.Position - rhs.Position);
	}

	public static TrixelEmplacement operator +(TrixelEmplacement lhs, Vector3 rhs)
	{
		return new TrixelEmplacement(lhs.Position + rhs);
	}

	public static TrixelEmplacement operator -(TrixelEmplacement lhs, Vector3 rhs)
	{
		return new TrixelEmplacement(lhs.Position - rhs);
	}

	public static TrixelEmplacement operator /(TrixelEmplacement lhs, float rhs)
	{
		return new TrixelEmplacement(lhs.Position / rhs);
	}

	public static bool operator <(TrixelEmplacement lhs, TrixelEmplacement rhs)
	{
		return lhs.CompareTo(rhs) < 0;
	}

	public static bool operator >(TrixelEmplacement lhs, TrixelEmplacement rhs)
	{
		return lhs.CompareTo(rhs) > 0;
	}

	public static bool operator <(TrixelEmplacement lhs, Vector3 rhs)
	{
		return lhs.CompareTo(new TrixelEmplacement(rhs)) < 0;
	}

	public static bool operator >(TrixelEmplacement lhs, Vector3 rhs)
	{
		return lhs.CompareTo(new TrixelEmplacement(rhs)) > 0;
	}
}
