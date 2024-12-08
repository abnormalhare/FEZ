using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Common;

namespace FezEngine.Tools;

public class CrcWriter
{
	private readonly BinaryWriter writer;

	private readonly List<byte> writtenBytes = new List<byte>();

	public CrcWriter(BinaryWriter writer)
	{
		this.writer = writer;
	}

	public void Write(bool value)
	{
		writtenBytes.AddRange(BitConverter.GetBytes(value));
		writer.Write(value);
	}

	public void Write(byte value)
	{
		writtenBytes.AddRange(BitConverter.GetBytes(value));
		writer.Write(value);
	}

	public void Write(byte[] buffer)
	{
		writtenBytes.AddRange(buffer);
		writer.Write(buffer);
	}

	public void Write(double value)
	{
		writtenBytes.AddRange(BitConverter.GetBytes(value));
		writer.Write(value);
	}

	public void Write(float value)
	{
		writtenBytes.AddRange(BitConverter.GetBytes(value));
		writer.Write(value);
	}

	public void Write(int value)
	{
		writtenBytes.AddRange(BitConverter.GetBytes(value));
		writer.Write(value);
	}

	public void Write(long value)
	{
		writtenBytes.AddRange(BitConverter.GetBytes(value));
		writer.Write(value);
	}

	public void Write(sbyte value)
	{
		writtenBytes.AddRange(BitConverter.GetBytes(value));
		writer.Write(value);
	}

	public void Write(short value)
	{
		writtenBytes.AddRange(BitConverter.GetBytes(value));
		writer.Write(value);
	}

	public void Write(string value)
	{
		writtenBytes.AddRange(Encoding.Unicode.GetBytes(value));
		writer.Write(value);
	}

	public void Write(uint value)
	{
		writtenBytes.AddRange(BitConverter.GetBytes(value));
		writer.Write(value);
	}

	public void Write(ulong value)
	{
		writtenBytes.AddRange(BitConverter.GetBytes(value));
		writer.Write(value);
	}

	public void Write(ushort value)
	{
		writtenBytes.AddRange(BitConverter.GetBytes(value));
		writer.Write(value);
	}

	public void WriteHash()
	{
		Write(Crc32.ComputeChecksum(writtenBytes.ToArray()));
	}
}
