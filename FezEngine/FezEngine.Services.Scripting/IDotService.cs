using Common;
using FezEngine.Components.Scripting;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Static = true)]
public interface IDotService : IScriptingBase
{
	[Description("Makes Dot say a custom text line")]
	LongRunningAction Say(string line, bool nearGomez, bool hideAfter);

	[Description("Hides Dot in Gomez's hat")]
	LongRunningAction ComeBackAndHide(bool withCamera);

	[Description("Spiral around the level, yo")]
	LongRunningAction SpiralAround(bool withCamera, bool hideDot);
}
