using System.Collections.Generic;
using System.Linq;
using ContentSerialization.Attributes;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class TrileGroup
{
	[Serialization(Ignore = true)]
	public int Id { get; set; }

	public string Name { get; set; }

	[Serialization(CollectionItemName = "trile")]
	public List<TrileInstance> Triles { get; set; }

	[Serialization(Optional = true)]
	public MovementPath Path { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Heavy { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public ActorType ActorType { get; set; }

	[Serialization(Ignore = true)]
	public bool InMidAir { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public float GeyserOffset { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public float GeyserPauseFor { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public float GeyserLiftFor { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public float GeyserApexHeight { get; set; }

	[Serialization(Ignore = true)]
	public bool MoveToEnd { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Spin180Degrees { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool SpinClockwise { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public float SpinFrequency { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool SpinNeedsTriggering { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public Vector3 SpinCenter { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool FallOnRotate { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public float SpinOffset { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public string AssociatedSound { get; set; }

	[Serialization(Ignore = true)]
	public bool PhysicsInitialized { get; set; }

	public TrileGroup()
	{
		Name = "Unnamed";
		Triles = new List<TrileInstance>();
	}

	public TrileGroup(TrileGroup group)
		: this()
	{
		Name = group.Name;
		Triles = new List<TrileInstance>(group.Triles.Select((TrileInstance x) => x.Clone()));
		Heavy = group.Heavy;
		ActorType = group.ActorType;
		Path = ((group.Path == null) ? null : group.Path.Clone());
		GeyserPauseFor = group.GeyserPauseFor;
		GeyserLiftFor = group.GeyserLiftFor;
		GeyserApexHeight = group.GeyserApexHeight;
		GeyserOffset = group.GeyserOffset;
		SpinClockwise = group.SpinClockwise;
		SpinFrequency = group.SpinFrequency;
		SpinNeedsTriggering = group.SpinNeedsTriggering;
		SpinCenter = group.SpinCenter;
		Spin180Degrees = group.Spin180Degrees;
		FallOnRotate = group.FallOnRotate;
		SpinOffset = group.SpinOffset;
		AssociatedSound = group.AssociatedSound;
	}
}
