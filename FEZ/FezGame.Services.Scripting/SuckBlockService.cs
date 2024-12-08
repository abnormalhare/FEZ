using System;
using System.Collections.Generic;
using Common;
using FezEngine.Services.Scripting;

namespace FezGame.Services.Scripting;

internal class SuckBlockService : ISuckBlockService, IScriptingBase
{
	private readonly Dictionary<int, bool> SuckState = new Dictionary<int, bool>();

	public event Action<int> Sucked;

	public void ResetEvents()
	{
		this.Sucked = Util.NullAction;
		SuckState.Clear();
	}

	public void OnSuck(int id)
	{
		SuckState[id] = true;
		this.Sucked(id);
	}

	public bool get_IsSucked(int id)
	{
		if (SuckState.Count == 0)
		{
			return true;
		}
		if (SuckState.TryGetValue(id, out var value))
		{
			return value;
		}
		return false;
	}
}
