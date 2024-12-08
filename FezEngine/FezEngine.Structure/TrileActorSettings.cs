using System;
using Common;
using ContentSerialization.Attributes;

namespace FezEngine.Structure;

public class TrileActorSettings : IEquatable<TrileActorSettings>
{
	[Serialization(Optional = true)]
	public ActorType Type { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public FaceOrientation Face { get; set; }

	public TrileActorSettings()
	{
	}

	public TrileActorSettings(TrileActorSettings copy)
	{
		Type = copy.Type;
		Face = copy.Face;
	}

	public override int GetHashCode()
	{
		return Util.CombineHashCodes(Type.GetHashCode(), Face.GetHashCode());
	}

	public bool Equals(TrileActorSettings other)
	{
		if ((object)other != null && other.Type == Type)
		{
			return other.Face == Face;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is TrileActorSettings)
		{
			return Equals(obj as TrileActorSettings);
		}
		return false;
	}

	public static bool operator ==(TrileActorSettings lhs, TrileActorSettings rhs)
	{
		return lhs?.Equals(rhs) ?? ((object)rhs == null);
	}

	public static bool operator !=(TrileActorSettings lhs, TrileActorSettings rhs)
	{
		return !(lhs == rhs);
	}
}
