using System;
using Common;
using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;

namespace FezGame.Services.Scripting;

public class SwitchService : ISwitchService, IScriptingBase
{
	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	public event Action<int> Explode = Util.NullAction;

	public event Action<int> Push = Util.NullAction;

	public event Action<int> Lift = Util.NullAction;

	public void OnExplode(int id)
	{
		this.Explode(id);
	}

	public void OnPush(int id)
	{
		this.Push(id);
	}

	public void OnLift(int id)
	{
		this.Lift(id);
	}

	public void Activate(int id)
	{
		OnExplode(id);
		OnPush(id);
	}

	public LongRunningAction ChangeTrile(int id, int newTrileId)
	{
		int[] oldTrileId = new int[LevelManager.Groups[id].Triles.Count];
		for (int i = 0; i < oldTrileId.Length; i++)
		{
			TrileInstance trileInstance = LevelManager.Groups[id].Triles[i];
			oldTrileId[i] = trileInstance.Trile.Id;
			LevelManager.SwapTrile(trileInstance, LevelManager.SafeGetTrile(newTrileId));
		}
		return new LongRunningAction(delegate
		{
			if (LevelManager.Groups.TryGetValue(id, out var value))
			{
				for (int j = 0; j < oldTrileId.Length; j++)
				{
					LevelManager.SwapTrile(value.Triles[j], LevelManager.SafeGetTrile(oldTrileId[j]));
				}
			}
		});
	}

	public void ResetEvents()
	{
		this.Explode = Util.NullAction;
		this.Push = Util.NullAction;
		this.Lift = Util.NullAction;
	}
}
