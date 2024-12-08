using System;
using Common;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;

namespace FezGame.Services.Scripting;

internal class ValveService : IValveService, IScriptingBase
{
	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	public event Action<int> Screwed = Util.NullAction;

	public event Action<int> Unscrewed = Util.NullAction;

	public void ResetEvents()
	{
		this.Screwed = Util.NullAction;
		this.Unscrewed = Util.NullAction;
	}

	public void OnScrew(int id)
	{
		this.Screwed(id);
	}

	public void OnUnscrew(int id)
	{
		this.Unscrewed(id);
	}

	public void SetEnabled(int id, bool enabled)
	{
		LevelManager.ArtObjects[id].Enabled = enabled;
	}
}
