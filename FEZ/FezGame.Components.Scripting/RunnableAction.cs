using System;
using FezEngine.Structure.Scripting;

namespace FezGame.Components.Scripting;

internal struct RunnableAction
{
	public ScriptAction Action;

	public Func<object> Invocation;
}
