using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(TrileGroup), RestrictTo = new ActorType[] { ActorType.RotatingGroup })]
public interface IRotatingGroupService : IScriptingBase
{
	void Rotate(int id, bool clockwise, int turns);

	void SetEnabled(int id, bool enabled);
}
