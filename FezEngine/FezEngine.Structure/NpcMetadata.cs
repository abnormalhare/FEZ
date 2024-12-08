using System.Collections.Generic;
using ContentSerialization.Attributes;

namespace FezEngine.Structure;

public class NpcMetadata
{
	[Serialization(Optional = true)]
	public float WalkSpeed = 1.5f;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool AvoidsGomez;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public ActorType ActorType;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public string SoundPath;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public List<NpcAction> SoundActions = new List<NpcAction>();
}
