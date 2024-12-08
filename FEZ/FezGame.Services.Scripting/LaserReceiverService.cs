using System;
using Common;
using FezEngine.Services.Scripting;

namespace FezGame.Services.Scripting;

internal class LaserReceiverService : ILaserReceiverService, IScriptingBase
{
	public event Action<int> Activate = Util.NullAction;

	public void ResetEvents()
	{
		this.Activate = Util.NullAction;
	}

	public void OnActivated(int id)
	{
		this.Activate(id);
	}
}
