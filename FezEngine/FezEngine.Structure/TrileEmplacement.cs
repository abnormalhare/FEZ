using System;
using ContentSerialization.Attributes;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

[TypeSerialization(FlattenToList = true)]
public struct TrileEmplacement : IEquatable<TrileEmplacement>, IComparable<TrileEmplacement>
{
	public int X;

	public int Y;

	public int Z;

	[Serialization(Ignore = true)]
	public Vector3 AsVector
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

	public TrileEmplacement(Vector3 position)
	{
		X = FezMath.Round(position.X);
		Y = FezMath.Round(position.Y);
		Z = FezMath.Round(position.Z);
	}

	public TrileEmplacement(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}

	public TrileEmplacement GetOffset(Vector3 vector)
	{
		return new TrileEmplacement(X + (int)vector.X, Y + (int)vector.Y, Z + (int)vector.Z);
	}

	public TrileEmplacement GetOffset(int offsetX, int offsetY, int offsetZ)
	{
		return new TrileEmplacement(X + offsetX, Y + offsetY, Z + offsetZ);
	}

	public override bool Equals(object obj)
	{
		if (obj is TrileEmplacement)
		{
			return Equals((TrileEmplacement)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return X ^ (Y << 10) ^ (Z << 20);
	}

	public override string ToString()
	{
		return $"({X}, {Y}, {Z})";
	}

	public void TraverseInto(FaceOrientation face)
	{
		switch (face)
		{
		case FaceOrientation.Back:
			Z--;
			break;
		case FaceOrientation.Front:
			Z++;
			break;
		case FaceOrientation.Top:
			Y++;
			break;
		case FaceOrientation.Down:
			Y--;
			break;
		case FaceOrientation.Left:
			X--;
			break;
		default:
			X++;
			break;
		}
	}

	public TrileEmplacement GetTraversal(ref FaceOrientation face)
	{
		return face switch
		{
			FaceOrientation.Back => new TrileEmplacement(X, Y, Z - 1), 
			FaceOrientation.Front => new TrileEmplacement(X, Y, Z + 1), 
			FaceOrientation.Top => new TrileEmplacement(X, Y + 1, Z), 
			FaceOrientation.Down => new TrileEmplacement(X, Y - 1, Z), 
			FaceOrientation.Left => new TrileEmplacement(X - 1, Y, Z), 
			_ => new TrileEmplacement(X + 1, Y, Z), 
		};
	}

	public bool Equals(TrileEmplacement other)
	{
		if (other.X == X && other.Y == Y)
		{
			return other.Z == Z;
		}
		return false;
	}

	public int CompareTo(TrileEmplacement other)
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

	public static bool operator ==(TrileEmplacement lhs, TrileEmplacement rhs)
	{
		if ((object)lhs != null)
		{
			return lhs.Equals(rhs);
		}
		return (object)rhs == null;
	}

	public static bool operator !=(TrileEmplacement lhs, TrileEmplacement rhs)
	{
		return !(lhs == rhs);
	}

	public static TrileEmplacement operator +(TrileEmplacement lhs, TrileEmplacement rhs)
	{
		return new TrileEmplacement(lhs.AsVector + rhs.AsVector);
	}

	public static TrileEmplacement operator -(TrileEmplacement lhs, TrileEmplacement rhs)
	{
		return new TrileEmplacement(lhs.AsVector - rhs.AsVector);
	}

	public static TrileEmplacement operator +(TrileEmplacement lhs, Vector3 rhs)
	{
		return new TrileEmplacement(lhs.AsVector + rhs);
	}

	public static TrileEmplacement operator -(TrileEmplacement lhs, Vector3 rhs)
	{
		return new TrileEmplacement(lhs.AsVector - rhs);
	}

	public static TrileEmplacement operator /(TrileEmplacement lhs, float rhs)
	{
		return new TrileEmplacement(lhs.AsVector / rhs);
	}

	public static TrileEmplacement operator *(TrileEmplacement lhs, Vector3 rhs)
	{
		return new TrileEmplacement(lhs.AsVector * rhs);
	}
}
