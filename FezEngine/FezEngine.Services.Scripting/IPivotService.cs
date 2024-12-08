using System;
using Common;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(ArtObjectInstance), RestrictTo = new ActorType[] { ActorType.PivotHandle })]
public interface IPivotService : IScriptingBase
{
	[Description("When it's been rotated right")]
	event Action<int> RotatedRight;

	[Description("When it's been rotated left")]
	event Action<int> RotatedLeft;

	void OnRotateRight(int id);

	void OnRotateLeft(int id);

	[Description("Gets the number of turns it's relative to the original state")]
	int get_Turns(int id);

	[Description("Enables or disables a pivot handle's rotatability")]
	void SetEnabled(int id, bool enabled);

	[Description("Enables or disables a pivot handle's rotatability")]
	void RotateTo(int id, int turns);
}
