using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;

namespace FezGame.Services.Scripting;

public class WarpGateService : IWarpGateService, IScriptingBase
{
	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	public void ResetEvents()
	{
	}

	public void SetEnabled(int id, bool enabled)
	{
		LevelManager.ArtObjects[id].ActorSettings.Inactive = true;
	}
}
