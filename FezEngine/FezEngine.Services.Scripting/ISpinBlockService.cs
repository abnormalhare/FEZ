using Common;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(ArtObjectInstance), RestrictTo = new ActorType[] { ActorType.SpinBlock })]
public interface ISpinBlockService : IScriptingBase
{
	[Description("Enables or disables a spinblock (which ceases or resumes its spinning)")]
	void SetEnabled(int id, bool enabled);
}
