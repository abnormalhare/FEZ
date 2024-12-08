using Common;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(ArtObjectInstance), RestrictTo = new ActorType[] { ActorType.LaserEmitter })]
public interface ILaserEmitterService : IScriptingBase
{
	[Description("Starts or stops an emitter")]
	void SetEnabled(int id, bool enabled);
}
