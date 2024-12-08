using System;
using Common;
using FezEngine.Services.Scripting;

namespace FezGame.Services.Scripting;

internal class CodePatternService : ICodePatternService, IScriptingBase
{
	public event Action<int> Activated;

	public void ResetEvents()
	{
		this.Activated = Util.NullAction;
	}

	public void OnActivate(int id)
	{
		this.Activated(id);
	}
}
