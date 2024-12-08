using System;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Tools;

public static class BinaryWritingTools
{
	public static void WriteObject(this CrcWriter writer, string s)
	{
		writer.Write(s != null);
		if (s != null)
		{
			writer.Write(s);
		}
	}

	public static string ReadNullableString(this CrcReader reader)
	{
		if (reader.ReadBoolean())
		{
			return reader.ReadString();
		}
		return null;
	}

	public static void WriteObject(this CrcWriter writer, float? s)
	{
		writer.Write(s.HasValue);
		if (s.HasValue)
		{
			writer.Write(s.Value);
		}
	}

	public static float? ReadNullableSingle(this CrcReader reader)
	{
		if (reader.ReadBoolean())
		{
			return reader.ReadSingle();
		}
		return null;
	}

	public static void Write(this CrcWriter writer, Vector3 s)
	{
		writer.Write(s.X);
		writer.Write(s.Y);
		writer.Write(s.Z);
	}

	public static Vector3 ReadVector3(this CrcReader reader)
	{
		return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
	}

	public static void Write(this CrcWriter writer, TimeSpan s)
	{
		writer.Write(s.Ticks);
	}

	public static TimeSpan ReadTimeSpan(this CrcReader reader)
	{
		return new TimeSpan(reader.ReadInt64());
	}

	public static void Write(this CrcWriter writer, TrileEmplacement s)
	{
		writer.Write(s.X);
		writer.Write(s.Y);
		writer.Write(s.Z);
	}

	public static TrileEmplacement ReadTrileEmplacement(this CrcReader reader)
	{
		return new TrileEmplacement(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
	}
}
