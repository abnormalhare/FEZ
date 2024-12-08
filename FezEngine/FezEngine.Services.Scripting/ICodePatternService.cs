using System;
using Common;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(ArtObjectInstance), RestrictTo = new ActorType[]
{
	ActorType.Rumbler,
	ActorType.CodeMachine,
	ActorType.QrCode
})]
public interface ICodePatternService : IScriptingBase
{
	[Description("When the right pattern is input")]
	event Action<int> Activated;

	void OnActivate(int id);
}
