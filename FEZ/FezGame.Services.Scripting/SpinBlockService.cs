using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;

namespace FezGame.Services.Scripting;

public class SpinBlockService : ISpinBlockService, IScriptingBase
{
	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	public void ResetEvents()
	{
	}

	public void SetEnabled(int id, bool enabled)
	{
		LevelManager.ArtObjects[id].ActorSettings.Inactive = !enabled;
	}
}
