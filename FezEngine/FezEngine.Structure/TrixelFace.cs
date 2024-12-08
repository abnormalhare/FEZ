using System;
using Common;

namespace FezEngine.Structure;

public class TrixelFace : IEquatable<TrixelFace>
{
	public TrixelEmplacement Id;

	public FaceOrientation Face { get; set; }

	public TrixelFace()
		: this(default(TrixelEmplacement), FaceOrientation.Left)
	{
	}

	public TrixelFace(int x, int y, int z, FaceOrientation face)
		: this(new TrixelEmplacement(x, y, z), face)
	{
	}

	public TrixelFace(TrixelEmplacement id, FaceOrientation face)
	{
		Id = id;
		Face = face;
	}

	public override int GetHashCode()
	{
		return Id.GetHashCode() + Face.GetHashCode();
	}

	public override bool Equals(object obj)
	{
		if (obj is TrixelFace)
		{
			return Equals(obj as TrixelFace);
		}
		return false;
	}

	public override string ToString()
	{
		return Util.ReflectToString(this);
	}

	public bool Equals(TrixelFace other)
	{
		if (other.Id.Equals(Id))
		{
			return other.Face == Face;
		}
		return false;
	}
}
