using System;
using Common;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Model = typeof(Script))]
public interface IScriptService : IScriptingBase
{
	[Description("When the script timeouts or terminates")]
	event Action<int> Complete;

	void OnComplete(int id);

	[Description("Enables or disables a script")]
	void SetEnabled(int id, bool enabled);

	[Description("Evaluates a script")]
	void Evaluate(int id);
}
