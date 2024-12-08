using System;
using Common;
using FezEngine.Components.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(TrileGroup), RestrictTo = new ActorType[]
{
	ActorType.PushSwitch,
	ActorType.ExploSwitch,
	ActorType.PushSwitchPermanent
})]
public interface ISwitchService : IScriptingBase
{
	[Description("When a bomb explodes near this switch")]
	event Action<int> Explode;

	[Description("When this switch is pushed completely")]
	[EndTrigger("Lift")]
	event Action<int> Push;

	[Description("When this switch is lifted back up")]
	event Action<int> Lift;

	void OnExplode(int id);

	void OnPush(int id);

	void OnLift(int id);

	[Description("Activates this switch")]
	void Activate(int id);

	[Description("Changes the visual of this switch's triles")]
	LongRunningAction ChangeTrile(int id, int newTrileId);
}
