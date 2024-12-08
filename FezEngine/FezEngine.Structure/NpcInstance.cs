using System.Collections.Generic;
using System.Linq;
using ContentSerialization.Attributes;
using FezEngine.Components;
using Microsoft.Xna.Framework;

namespace FezEngine.Structure;

public class NpcInstance
{
	[Serialization(Ignore = true)]
	public readonly NpcMetadata Metadata = new NpcMetadata();

	[Serialization(Ignore = true)]
	public int Id { get; set; }

	[Serialization(Ignore = true)]
	public bool Talking { get; set; }

	[Serialization(Ignore = true)]
	public bool Enabled { get; set; }

	[Serialization(Ignore = true)]
	public bool Visible { get; set; }

	public string Name { get; set; }

	public Vector3 Position { get; set; }

	public Vector3 DestinationOffset { get; set; }

	[Serialization(Optional = true)]
	public bool RandomizeSpeech { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool SayFirstSpeechLineOnce { get; set; }

	public List<SpeechLine> Speech { get; set; }

	[Serialization(Ignore = true)]
	public SpeechLine CustomSpeechLine { get; set; }

	[Serialization(Ignore = true)]
	public Group Group { get; set; }

	[Serialization(Ignore = true)]
	public NpcState State { get; set; }

	public Dictionary<NpcAction, NpcActionContent> Actions { get; set; }

	[Serialization(Optional = true)]
	public float WalkSpeed
	{
		get
		{
			return Metadata.WalkSpeed;
		}
		set
		{
			Metadata.WalkSpeed = value;
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool AvoidsGomez
	{
		get
		{
			return Metadata.AvoidsGomez;
		}
		set
		{
			Metadata.AvoidsGomez = value;
		}
	}

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public ActorType ActorType
	{
		get
		{
			return Metadata.ActorType;
		}
		set
		{
			Metadata.ActorType = value;
		}
	}

	public NpcInstance()
	{
		Speech = new List<SpeechLine>();
		Actions = new Dictionary<NpcAction, NpcActionContent>(NpcActionComparer.Default);
		Enabled = true;
		Visible = true;
	}

	public NpcInstance Clone()
	{
		List<SpeechLine> speech = Speech.Select((SpeechLine line) => line.Clone()).ToList();
		Dictionary<NpcAction, NpcActionContent> actions = Actions.Keys.ToDictionary((NpcAction action) => action, (NpcAction action) => Actions[action].Clone());
		return new NpcInstance
		{
			Name = Name,
			Position = Position,
			DestinationOffset = DestinationOffset,
			WalkSpeed = WalkSpeed,
			RandomizeSpeech = RandomizeSpeech,
			SayFirstSpeechLineOnce = SayFirstSpeechLineOnce,
			Speech = speech,
			Actions = actions,
			AvoidsGomez = AvoidsGomez,
			ActorType = ActorType
		};
	}

	public void CopyFrom(NpcInstance instance)
	{
		Name = instance.Name;
		Position = instance.Position;
		DestinationOffset = instance.DestinationOffset;
		WalkSpeed = instance.WalkSpeed;
		RandomizeSpeech = instance.RandomizeSpeech;
		SayFirstSpeechLineOnce = instance.SayFirstSpeechLineOnce;
		Speech = instance.Speech;
		Actions = instance.Actions;
		AvoidsGomez = instance.AvoidsGomez;
		ActorType = instance.ActorType;
	}

	public void FillMetadata(NpcMetadata md)
	{
		Metadata.AvoidsGomez = md.AvoidsGomez;
		Metadata.WalkSpeed = md.WalkSpeed;
		Metadata.SoundPath = md.SoundPath;
		Metadata.SoundActions = md.SoundActions;
	}
}
