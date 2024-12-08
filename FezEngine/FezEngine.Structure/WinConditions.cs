using System.Collections.Generic;
using System.Linq;
using ContentSerialization.Attributes;

namespace FezEngine.Structure;

public class WinConditions
{
	[Serialization(Optional = true, DefaultValueOptional = true)]
	public int LockedDoorCount;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public int UnlockedDoorCount;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public int ChestCount;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public int CubeShardCount;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public int OtherCollectibleCount;

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public int SplitUpCount;

	[Serialization(Optional = true)]
	public List<int> ScriptIds = new List<int>();

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public int SecretCount;

	public bool Fullfills(WinConditions wonditions)
	{
		if (UnlockedDoorCount >= wonditions.UnlockedDoorCount && LockedDoorCount >= wonditions.LockedDoorCount && ChestCount >= wonditions.ChestCount && CubeShardCount >= wonditions.CubeShardCount && OtherCollectibleCount >= wonditions.OtherCollectibleCount && SplitUpCount >= wonditions.SplitUpCount && SecretCount >= wonditions.SecretCount)
		{
			return wonditions.ScriptIds.All((int x) => ScriptIds.Contains(x));
		}
		return false;
	}

	public WinConditions Clone()
	{
		return new WinConditions
		{
			LockedDoorCount = LockedDoorCount,
			UnlockedDoorCount = UnlockedDoorCount,
			ChestCount = ChestCount,
			CubeShardCount = CubeShardCount,
			OtherCollectibleCount = OtherCollectibleCount,
			SplitUpCount = SplitUpCount,
			ScriptIds = new List<int>(ScriptIds),
			SecretCount = SecretCount
		};
	}

	public void CloneInto(WinConditions w)
	{
		w.LockedDoorCount = LockedDoorCount;
		w.UnlockedDoorCount = UnlockedDoorCount;
		w.ChestCount = ChestCount;
		w.CubeShardCount = CubeShardCount;
		w.OtherCollectibleCount = OtherCollectibleCount;
		w.SplitUpCount = SplitUpCount;
		w.SecretCount = SecretCount;
		w.ScriptIds.Clear();
		w.ScriptIds.AddRange(ScriptIds);
	}
}
