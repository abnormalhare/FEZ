using System.Collections.Generic;
using System.Linq;
using ContentSerialization.Attributes;

namespace FezEngine.Structure;

public class TrackedSong
{
	public string Name = "Untitled";

	public List<Loop> Loops = new List<Loop>();

	public int Tempo = 60;

	public int TimeSignature = 4;

	[Serialization(Optional = true)]
	public AssembleChords AssembleChord;

	[Serialization(Optional = true)]
	public ShardNotes[] Notes = new ShardNotes[8]
	{
		ShardNotes.C2,
		ShardNotes.D2,
		ShardNotes.E2,
		ShardNotes.F2,
		ShardNotes.G2,
		ShardNotes.A2,
		ShardNotes.B2,
		ShardNotes.C3
	};

	[Serialization(Optional = true)]
	public bool RandomOrdering;

	[Serialization(Optional = true)]
	public int[] CustomOrdering;

	public void Initialize()
	{
		foreach (Loop loop in Loops)
		{
			if (loop.Initialized)
			{
				loop.Dawn = loop.OriginalDawn;
				loop.Dusk = loop.OriginalDusk;
				loop.Night = loop.OriginalNight;
				loop.Day = loop.OriginalDay;
			}
			else
			{
				loop.OriginalDawn = loop.Dawn;
				loop.OriginalDusk = loop.Dusk;
				loop.OriginalNight = loop.Night;
				loop.OriginalDay = loop.Day;
				loop.Initialized = true;
			}
		}
	}

	public TrackedSong Clone()
	{
		return new TrackedSong
		{
			Name = Name,
			Loops = new List<Loop>(Loops.Select((Loop loop) => loop.Clone())),
			Tempo = Tempo,
			TimeSignature = TimeSignature,
			Notes = Notes.ToArray(),
			AssembleChord = AssembleChord,
			RandomOrdering = RandomOrdering,
			CustomOrdering = ((CustomOrdering == null) ? null : CustomOrdering.ToArray())
		};
	}

	public void UpdateFromCopy(TrackedSong other)
	{
		Name = other.Name;
		Loops = other.Loops;
		Tempo = other.Tempo;
		TimeSignature = other.TimeSignature;
		Notes = other.Notes;
		AssembleChord = other.AssembleChord;
		RandomOrdering = other.RandomOrdering;
		CustomOrdering = other.CustomOrdering;
	}
}
