using System;
using Common;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(ArtObjectInstance), RestrictTo = new ActorType[] { ActorType.Timeswitch })]
public interface ITimeswitchService : IScriptingBase
{
	[Description("When the screw minimally sticks out from the base (it's been screwed out)")]
	event Action<int> ScrewedOut;

	[Description("When it stop winding back in (hits the base)")]
	event Action<int> HitBase;

	void OnScrewedOut(int id);

	void OnHitBase(int id);
}
