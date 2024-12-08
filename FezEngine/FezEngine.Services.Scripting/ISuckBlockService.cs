using System;
using Common;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(TrileGroup), RestrictTo = new ActorType[] { ActorType.SuckBlock })]
public interface ISuckBlockService : IScriptingBase
{
	[Description("When it's completely inside its host volume")]
	event Action<int> Sucked;

	void OnSuck(int id);

	bool get_IsSucked(int id);
}
