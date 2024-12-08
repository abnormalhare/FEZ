using Common;
using FezEngine.Components.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(MovementPath))]
public interface IPathService : IScriptingBase
{
	[Description("Applies the whole path to the camera")]
	LongRunningAction Start(int id, bool inTransition, bool outTransition);
}
