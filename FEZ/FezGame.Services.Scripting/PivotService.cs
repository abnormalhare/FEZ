using System;
using Common;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;

namespace FezGame.Services.Scripting;

internal class PivotService : IPivotService, IScriptingBase
{
	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public event Action<int> RotatedRight = Util.NullAction;

	public event Action<int> RotatedLeft = Util.NullAction;

	public void ResetEvents()
	{
		this.RotatedRight = Util.NullAction;
		this.RotatedLeft = Util.NullAction;
	}

	public void OnRotateRight(int id)
	{
		this.RotatedRight(id);
	}

	public void OnRotateLeft(int id)
	{
		this.RotatedLeft(id);
	}

	public int get_Turns(int id)
	{
		if (!GameState.SaveData.ThisLevel.PivotRotations.TryGetValue(id, out var value))
		{
			return 0;
		}
		return value;
	}

	public void SetEnabled(int id, bool enabled)
	{
		LevelManager.ArtObjects[id].Enabled = enabled;
	}

	public void RotateTo(int id, int turns)
	{
		GameState.SaveData.ThisLevel.PivotRotations[id] = turns;
	}
}
