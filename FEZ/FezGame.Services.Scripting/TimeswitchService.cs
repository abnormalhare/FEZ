using System;
using Common;
using FezEngine.Services.Scripting;

namespace FezGame.Services.Scripting;

public class TimeswitchService : ITimeswitchService, IScriptingBase
{
	public event Action<int> ScrewedOut = Util.NullAction;

	public event Action<int> HitBase = Util.NullAction;

	public void ResetEvents()
	{
		this.ScrewedOut = Util.NullAction;
		this.HitBase = Util.NullAction;
	}

	public void OnScrewedOut(int id)
	{
		this.ScrewedOut(id);
	}

	public void OnHitBase(int id)
	{
		this.HitBase(id);
	}
}
