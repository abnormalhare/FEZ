using System;
using FezEngine.Services.Scripting;
using FezEngine.Tools;

namespace FezGame.Services.Scripting;

internal class OwlService : IOwlService, IScriptingBase
{
	public int OwlsCollected => GameState.SaveData.CollectedOwls;

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public event Action OwlCollected;

	public event Action OwlLanded;

	public void ResetEvents()
	{
		this.OwlCollected = null;
		this.OwlLanded = null;
	}

	public void OnOwlCollected()
	{
		if (this.OwlCollected != null)
		{
			this.OwlCollected();
		}
	}

	public void OnOwlLanded()
	{
		if (this.OwlLanded != null)
		{
			this.OwlLanded();
		}
	}
}
