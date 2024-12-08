using System;
using FezEngine.Services.Scripting;

namespace FezGame.Services.Scripting;

public class TombstoneService : ITombstoneService, IScriptingBase
{
	private int alignCount;

	public event Action MoreThanOneAligned;

	public void ResetEvents()
	{
		this.MoreThanOneAligned = null;
	}

	public void OnMoreThanOneAligned()
	{
		if (this.MoreThanOneAligned != null)
		{
			this.MoreThanOneAligned();
		}
	}

	public int get_AlignedCount()
	{
		return alignCount;
	}

	public void UpdateAlignCount(int count)
	{
		alignCount = count;
	}
}
