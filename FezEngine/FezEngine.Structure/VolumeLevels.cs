using System;
using System.Collections.Generic;
using ContentSerialization.Attributes;

namespace FezEngine.Structure;

public class VolumeLevels
{
	[Serialization(CollectionItemName = "Sound")]
	public Dictionary<string, VolumeLevel> Sounds = new Dictionary<string, VolumeLevel>(StringComparer.OrdinalIgnoreCase);
}
