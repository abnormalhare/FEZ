using Common;
using FezEngine.Components.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(NpcInstance))]
public interface INpcService : IScriptingBase
{
	[Description("Makes the NPC say a custom text line")]
	LongRunningAction Say(int id, string line, string customSound, string customAnimation);

	[Description("CarryGeezerLetter")]
	void CarryGeezerLetter(int id);
}
