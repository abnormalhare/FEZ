using FezEngine.Components.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(BackgroundPlane), RestrictTo = new ActorType[] { ActorType.BigWaterfall })]
public interface IBigWaterfallService : IScriptingBase
{
	LongRunningAction Open(int id);
}
