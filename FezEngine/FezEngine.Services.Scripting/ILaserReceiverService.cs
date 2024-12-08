using System;
using Common;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(ArtObjectInstance), RestrictTo = new ActorType[] { ActorType.LaserReceiver })]
public interface ILaserReceiverService : IScriptingBase
{
	[Description("When a receiver receives a laser")]
	event Action<int> Activate;

	void OnActivated(int id);
}
