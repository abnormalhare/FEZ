using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Common;

namespace FezEngine.Tools;

public class CrcReader
{
	private readonly List<byte> readBytes = new List<byte>();

	private readonly BinaryReader reader;

	public CrcReader(BinaryReader reader)
	{
		this.reader = reader;
	}

	public bool ReadBoolean()
	{
		bool flag = reader.ReadBoolean();
		readBytes.AddRange(BitConverter.GetBytes(flag));
		return flag;
	}

	public byte ReadByte()
	{
		byte b = reader.ReadByte();
		readBytes.Add(b);
		return b;
	}

	public byte[] ReadBytes(int count)
	{
		byte[] array = reader.ReadBytes(count);
		readBytes.AddRange(array);
		return array;
	}

	public char ReadChar()
	{
		char c = reader.ReadChar();
		readBytes.AddRange(BitConverter.GetBytes(c));
		return c;
	}

	public double ReadDouble()
	{
		double num = reader.ReadDouble();
		readBytes.AddRange(BitConverter.GetBytes(num));
		return num;
	}

	public short ReadInt16()
	{
		short num = reader.ReadInt16();
		readBytes.AddRange(BitConverter.GetBytes(num));
		return num;
	}

	public int ReadInt32()
	{
		int num = reader.ReadInt32();
		readBytes.AddRange(BitConverter.GetBytes(num));
		return num;
	}

	public long ReadInt64()
	{
		long num = reader.ReadInt64();
		readBytes.AddRange(BitConverter.GetBytes(num));
		return num;
	}

	public sbyte ReadSByte()
	{
		sbyte b = reader.ReadSByte();
		readBytes.AddRange(BitConverter.GetBytes(b));
		return b;
	}

	public float ReadSingle()
	{
		float num = reader.ReadSingle();
		readBytes.AddRange(BitConverter.GetBytes(num));
		return num;
	}

	public string ReadString()
	{
		string text = reader.ReadString();
		readBytes.AddRange(Encoding.Unicode.GetBytes(text));
		return text;
	}

	public ushort ReadUInt16()
	{
		ushort num = reader.ReadUInt16();
		readBytes.AddRange(BitConverter.GetBytes(num));
		return num;
	}

	public uint ReadUInt32()
	{
		uint num = reader.ReadUInt32();
		readBytes.AddRange(BitConverter.GetBytes(num));
		return num;
	}

	public ulong ReadUInt64()
	{
		ulong num = reader.ReadUInt64();
		readBytes.AddRange(BitConverter.GetBytes(num));
		return num;
	}

	public bool CheckHash()
	{
		uint num = Crc32.ComputeChecksum(readBytes.ToArray());
		return ReadUInt32() == num;
	}
}
