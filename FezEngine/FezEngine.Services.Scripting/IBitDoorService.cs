using System;
using Common;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(ArtObjectInstance), RestrictTo = new ActorType[] { ActorType.EightBitDoor })]
public interface IBitDoorService : IScriptingBase
{
	[Description("When it's opened")]
	event Action<int> Open;

	void OnOpen(int id);
}
