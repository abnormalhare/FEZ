using System.Collections.Generic;
using ContentSerialization.Attributes;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class VolumeActorSettings
{
	[Serialization(Optional = true)]
	public Vector2 FarawayPlaneOffset;

	[Serialization(Ignore = true)]
	public float WaterOffset;

	[Serialization(Ignore = true)]
	public string DestinationSong;

	[Serialization(Ignore = true)]
	public float DestinationPixelsPerTrixel;

	[Serialization(Ignore = true)]
	public float DestinationRadius;

	[Serialization(Ignore = true)]
	public Vector2 DestinationOffset;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool IsPointOfInterest;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool WaterLocked;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool IsSecretPassage;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public CodeInput[] CodePattern;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public List<DotDialogueLine> DotDialogue = new List<DotDialogueLine>();

	[Serialization(Ignore = true)]
	public int NextLine = -1;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool IsBlackHole;

	[Serialization(Ignore = true)]
	public bool Sucking;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool NeedsTrigger;

	[Serialization(Ignore = true)]
	public bool PreventHey;
}
