using System.Collections.Generic;
using ContentSerialization.Attributes;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class Volume
{
	private Vector3 from;

	private Vector3 to;

	[Serialization(Ignore = true)]
	public int Id { get; set; }

	[Serialization(Ignore = true)]
	public bool Enabled { get; set; }

	[Serialization(Ignore = true)]
	public bool PlayerInside { get; set; }

	[Serialization(Ignore = true)]
	public bool? PlayerIsHigher { get; set; }

	public HashSet<FaceOrientation> Orientations { get; set; }

	[Serialization(Optional = true)]
	public VolumeActorSettings ActorSettings { get; set; }

	public Vector3 From
	{
		get
		{
			return from;
		}
		set
		{
			from = value;
			BoundingBox = new BoundingBox(value, BoundingBox.Max);
		}
	}

	public Vector3 To
	{
		get
		{
			return to;
		}
		set
		{
			to = value;
			BoundingBox = new BoundingBox(BoundingBox.Min, value);
		}
	}

	[Serialization(Ignore = true)]
	public BoundingBox BoundingBox { get; set; }

	public Volume()
	{
		Orientations = new HashSet<FaceOrientation>(FaceOrientationComparer.Default);
		Enabled = true;
	}
}
