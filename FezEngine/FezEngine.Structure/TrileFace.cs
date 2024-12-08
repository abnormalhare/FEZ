using System;
using Common;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class TrileFace : IEquatable<TrileFace>
{
	public TrileEmplacement Id;

	public FaceOrientation Face { get; set; }

	public TrileFace()
	{
		Id = default(TrileEmplacement);
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode() ^ Face.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj is TrileFace)
		{
			return Equals(obj as TrileFace);
		}
		return false;
	}

	public override string ToString()
	{
		return Util.ReflectToString(this);
	}

	public bool Equals(TrileFace other)
	{
		if ((object)other != null && other.Id == Id)
		{
			return other.Face == Face;
		}
		return false;
	}

	public static bool operator ==(TrileFace lhs, TrileFace rhs)
	{
		return lhs?.Equals(rhs) ?? ((object)rhs == null);
	}

	public static bool operator !=(TrileFace lhs, TrileFace rhs)
	{
		return !(lhs == rhs);
	}

	public static TrileFace operator +(TrileFace lhs, Vector3 rhs)
	{
		return new TrileFace
		{
			Id = lhs.Id + rhs,
			Face = lhs.Face
		};
	}

	public static TrileFace operator -(TrileFace lhs, Vector3 rhs)
	{
		return new TrileFace
		{
			Id = lhs.Id - rhs,
			Face = lhs.Face
		};
	}
}
