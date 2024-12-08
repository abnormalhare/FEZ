using System;
using Common;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;

namespace FezGame.Services.Scripting;

public class ScriptService : IScriptService, IScriptingBase
{
	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	public event Action<int> Complete = Util.NullAction;

	public void OnComplete(int id)
	{
		this.Complete(id);
	}

	public void SetEnabled(int id, bool enabled)
	{
		LevelManager.Scripts[id].Disabled = !enabled;
	}

	public void Evaluate(int id)
	{
		LevelManager.Scripts[id].ScheduleEvalulation = true;
	}

	public void ResetEvents()
	{
		this.Complete = Util.NullAction;
	}
}
