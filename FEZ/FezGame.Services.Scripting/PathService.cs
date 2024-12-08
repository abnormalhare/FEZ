using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;

namespace FezGame.Services.Scripting;

internal class PathService : IPathService, IScriptingBase
{
	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	public LongRunningAction Start(int id, bool inTransition, bool outTransition)
	{
		LevelManager.Paths[id].NeedsTrigger = false;
		LevelManager.Paths[id].RunOnce = true;
		LevelManager.Paths[id].InTransition = inTransition;
		LevelManager.Paths[id].OutTransition = outTransition;
		return new LongRunningAction((float elapsed, float sinceStarted) => LevelManager.Paths[id].NeedsTrigger);
	}

	public void ResetEvents()
	{
	}
}
