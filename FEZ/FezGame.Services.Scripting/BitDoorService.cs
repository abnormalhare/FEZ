using System;
using Common;
using FezEngine.Services.Scripting;

namespace FezGame.Services.Scripting;

internal class BitDoorService : IBitDoorService, IScriptingBase
{
	public event Action<int> Open;

	public void OnOpen(int id)
	{
		this.Open(id);
	}

	public void ResetEvents()
	{
		this.Open = Util.NullAction;
	}
}
