using System;
using Common;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(ArtObjectInstance), RestrictTo = new ActorType[]
{
	ActorType.Valve,
	ActorType.BoltHandle
})]
public interface IValveService : IScriptingBase
{
	[Description("When it's unscrewed")]
	event Action<int> Screwed;

	[Description("When it's screwed in")]
	event Action<int> Unscrewed;

	void OnScrew(int id);

	void OnUnscrew(int id);

	[Description("Enables or disables a valve's rotatability")]
	void SetEnabled(int id, bool enabled);
}
