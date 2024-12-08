using System;
using Common;
using FezEngine.Structure.Scripting;

namespace FezEngine.Services.Scripting;

[Entity(Static = true)]
public interface ITombstoneService : IScriptingBase
{
	[Description("When more than one tombstones are aligned")]
	event Action MoreThanOneAligned;

	void OnMoreThanOneAligned();

	int get_AlignedCount();

	void UpdateAlignCount(int count);
}
