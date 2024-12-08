using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;

namespace FezGame.Services.Scripting;

internal class LaserEmitterService : ILaserEmitterService, IScriptingBase
{
	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	public void ResetEvents()
	{
	}

	public void SetEnabled(int id, bool enabled)
	{
		LevelManager.ArtObjects[id].Enabled = !enabled;
	}
}
