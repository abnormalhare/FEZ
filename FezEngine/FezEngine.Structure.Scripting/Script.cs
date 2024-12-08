using System;
using System.Collections.Generic;
using System.Linq;
using ContentSerialization.Attributes;

namespace FezEngine.Structure.Scripting;

public class Script
{
	internal const string MemberSeparator = ".";

	[Serialization(Ignore = true)]
	public int Id { get; set; }

	public string Name { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool OneTime { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool LevelWideOneTime { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Disabled { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool Triggerless { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool IgnoreEndTriggers { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool IsWinCondition { get; set; }

	[Serialization(Optional = true)]
	public TimeSpan? Timeout { get; set; }

	[Serialization(CollectionItemName = "Trigger")]
	public List<ScriptTrigger> Triggers { get; set; }

	[Serialization(CollectionItemName = "Action")]
	public List<ScriptAction> Actions { get; set; }

	[Serialization(Optional = true, CollectionItemName = "Condition")]
	public List<ScriptCondition> Conditions { get; set; }

	[Serialization(Ignore = true)]
	public bool ScheduleEvalulation { get; set; }

	public Script()
	{
		Name = "Untitled";
		Triggers = new List<ScriptTrigger>();
		Actions = new List<ScriptAction>();
	}

	public Script Clone()
	{
		List<ScriptTrigger> triggers = Triggers.Select((ScriptTrigger t) => t.Clone()).ToList();
		List<ScriptAction> actions = Actions.Select((ScriptAction a) => a.Clone()).ToList();
		List<ScriptCondition> conditions = ((Conditions == null) ? null : Conditions.Select((ScriptCondition c) => c.Clone()).ToList());
		return new Script
		{
			Id = -1,
			Name = Name,
			Triggers = triggers,
			Actions = actions,
			Conditions = conditions,
			OneTime = OneTime,
			LevelWideOneTime = LevelWideOneTime,
			Disabled = Disabled,
			Triggerless = Triggerless,
			IgnoreEndTriggers = IgnoreEndTriggers,
			Timeout = Timeout,
			ScheduleEvalulation = ScheduleEvalulation
		};
	}
}
