using System.Collections.Generic;
using ContentSerialization.Attributes;

namespace FezEngine.Structure;

public class MovementPath
{
	[Serialization(Ignore = true)]
	public int Id { get; set; }

	[Serialization(Optional = true)]
	public bool IsSpline { get; set; }

	[Serialization(Optional = true)]
	public float OffsetSeconds { get; set; }

	[Serialization(CollectionItemName = "segment")]
	public List<PathSegment> Segments { get; set; }

	public PathEndBehavior EndBehavior { get; set; }

	public bool NeedsTrigger { get; set; }

	[Serialization(Optional = true)]
	public string SoundName { get; set; }

	[Serialization(Ignore = true)]
	public bool RunOnce { get; set; }

	[Serialization(Ignore = true)]
	public bool RunSingleSegment { get; set; }

	[Serialization(Ignore = true)]
	public bool Backwards { get; set; }

	[Serialization(Ignore = true)]
	public bool InTransition { get; set; }

	[Serialization(Ignore = true)]
	public bool OutTransition { get; set; }

	[Serialization(Optional = true)]
	public bool SaveTrigger { get; set; }

	public MovementPath()
	{
		Segments = new List<PathSegment>();
	}

	public MovementPath Clone()
	{
		List<PathSegment> list = new List<PathSegment>(Segments.Count);
		foreach (PathSegment segment in Segments)
		{
			list.Add(segment.Clone());
		}
		return new MovementPath
		{
			IsSpline = IsSpline,
			OffsetSeconds = OffsetSeconds,
			Segments = list,
			NeedsTrigger = NeedsTrigger,
			SoundName = SoundName,
			RunOnce = RunOnce,
			RunSingleSegment = RunSingleSegment,
			Backwards = Backwards,
			InTransition = InTransition,
			OutTransition = OutTransition,
			SaveTrigger = SaveTrigger
		};
	}
}
