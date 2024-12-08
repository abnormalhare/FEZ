using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(ArtObjectInstance), RestrictTo = new ActorType[] { ActorType.WarpGate })]
public interface IWarpGateService : IScriptingBase
{
	void SetEnabled(int id, bool enabled);
}
