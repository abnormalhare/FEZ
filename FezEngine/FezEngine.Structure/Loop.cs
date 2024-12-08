using ContentSerialization.Attributes;

namespace FezEngine.Structure;

public class Loop
{
	public string Name;

	public int TriggerFrom;

	public int TriggerTo;

	public int LoopTimesFrom = 1;

	public int LoopTimesTo = 1;

	public int Duration = 1;

	[Serialization(Optional = true)]
	public int Delay;

	[Serialization(Optional = true)]
	public bool Day = true;

	[Serialization(Optional = true)]
	public bool Night = true;

	[Serialization(Optional = true)]
	public bool Dawn = true;

	[Serialization(Optional = true)]
	public bool Dusk = true;

	[Serialization(Optional = true)]
	public bool OneAtATime;

	[Serialization(Optional = true)]
	public bool CutOffTail;

	[Serialization(Optional = true)]
	public bool FractionalTime;

	[Serialization(Ignore = true)]
	public bool Initialized;

	[Serialization(Ignore = true)]
	public bool OriginalDay;

	[Serialization(Ignore = true)]
	public bool OriginalDusk;

	[Serialization(Ignore = true)]
	public bool OriginalNight;

	[Serialization(Ignore = true)]
	public bool OriginalDawn;

	public Loop Clone()
	{
		return new Loop
		{
			Name = Name,
			TriggerFrom = TriggerFrom,
			TriggerTo = TriggerTo,
			LoopTimesFrom = LoopTimesFrom,
			LoopTimesTo = LoopTimesTo,
			Duration = Duration,
			Delay = Delay,
			Night = Night,
			Day = Day,
			Dawn = Dawn,
			Dusk = Dusk,
			OneAtATime = OneAtATime,
			CutOffTail = CutOffTail,
			FractionalTime = FractionalTime
		};
	}

	public void UpdateFromCopy(Loop other)
	{
		Name = other.Name;
		TriggerFrom = other.TriggerFrom;
		TriggerTo = other.TriggerTo;
		LoopTimesFrom = other.LoopTimesFrom;
		LoopTimesTo = other.LoopTimesTo;
		Duration = other.Duration;
		Delay = other.Delay;
		Night = other.Night;
		Day = other.Day;
		Dawn = other.Dawn;
		Dusk = other.Dusk;
		OneAtATime = other.OneAtATime;
		CutOffTail = other.CutOffTail;
		FractionalTime = other.FractionalTime;
	}
}
