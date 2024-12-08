using System;
using ContentSerialization.Attributes;

namespace FezEngine.Structure;

public class CameraNodeData : ICloneable
{
	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Perspective { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public int PixelsPerTrixel { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public string SoundName { get; set; }

	public object Clone()
	{
		return new CameraNodeData
		{
			Perspective = Perspective,
			PixelsPerTrixel = PixelsPerTrixel,
			SoundName = SoundName
		};
	}
}
