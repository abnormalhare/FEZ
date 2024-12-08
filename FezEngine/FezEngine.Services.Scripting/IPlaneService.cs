using FezEngine.Components.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(BackgroundPlane))]
public interface IPlaneService : IScriptingBase
{
	LongRunningAction FadeIn(int id, float seconds);

	LongRunningAction FadeOut(int id, float seconds);

	LongRunningAction Flicker(int id, float factor);
}
