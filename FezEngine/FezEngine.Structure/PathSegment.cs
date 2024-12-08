using System;
using ContentSerialization.Attributes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezEngine.Structure;

public class PathSegment
{
	public Vector3 Destination { get; set; }

	[Serialization(Optional = true)]
	public Quaternion Orientation { get; set; }

	[Serialization(Optional = true)]
	public TimeSpan WaitTimeOnStart { get; set; }

	[Serialization(Optional = true)]
	public TimeSpan WaitTimeOnFinish { get; set; }

	public TimeSpan Duration { get; set; }

	public float Acceleration { get; set; }

	[Serialization(Optional = true)]
	public float Deceleration { get; set; }

	public float JitterFactor { get; set; }

	[Serialization(Ignore = true)]
	public bool Bounced { get; set; }

	[Serialization(Ignore = true)]
	public SoundEffect Sound { get; set; }

	[Serialization(Optional = true)]
	public ICloneable CustomData { get; set; }

	public PathSegment()
	{
		Duration = TimeSpan.FromSeconds(1.0);
		Orientation = Quaternion.Identity;
	}

	public bool IsFirstNode(MovementPath path)
	{
		return path.Segments[0] == this;
	}

	public void CopyFrom(PathSegment other)
	{
		WaitTimeOnStart = other.WaitTimeOnStart;
		WaitTimeOnFinish = other.WaitTimeOnFinish;
		Duration = other.Duration;
		Acceleration = other.Acceleration;
		Deceleration = other.Deceleration;
		JitterFactor = other.JitterFactor;
		Destination = other.Destination;
		Orientation = other.Orientation;
		CustomData = ((other.CustomData == null) ? null : (other.CustomData.Clone() as ICloneable));
	}

	public PathSegment Clone()
	{
		return new PathSegment
		{
			Acceleration = Acceleration,
			Deceleration = Deceleration,
			Destination = Destination,
			Duration = Duration,
			JitterFactor = JitterFactor,
			WaitTimeOnFinish = WaitTimeOnFinish,
			WaitTimeOnStart = WaitTimeOnStart,
			Orientation = Orientation,
			CustomData = ((CustomData == null) ? null : (CustomData.Clone() as ICloneable))
		};
	}
}
