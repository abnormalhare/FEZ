using System;
using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class PathSegmentReader : ContentTypeReader<PathSegment>
{
	protected override PathSegment Read(ContentReader input, PathSegment existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new PathSegment();
		}
		existingInstance.Destination = input.ReadVector3();
		existingInstance.Duration = input.ReadObject<TimeSpan>();
		existingInstance.WaitTimeOnStart = input.ReadObject<TimeSpan>();
		existingInstance.WaitTimeOnFinish = input.ReadObject<TimeSpan>();
		existingInstance.Acceleration = input.ReadSingle();
		existingInstance.Deceleration = input.ReadSingle();
		existingInstance.JitterFactor = input.ReadSingle();
		existingInstance.Orientation = input.ReadQuaternion();
		if (input.ReadBoolean())
		{
			existingInstance.CustomData = input.ReadObject<CameraNodeData>();
		}
		return existingInstance;
	}
}
