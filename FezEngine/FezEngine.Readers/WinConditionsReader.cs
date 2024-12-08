using FezEngine.Structure;
using Microsoft.Xna.Framework.Content;

namespace FezEngine.Readers;

public class WinConditionsReader : ContentTypeReader<WinConditions>
{
	protected override WinConditions Read(ContentReader input, WinConditions existingInstance)
	{
		if (existingInstance == null)
		{
			existingInstance = new WinConditions();
		}
		existingInstance.ChestCount = input.ReadInt32();
		existingInstance.LockedDoorCount = input.ReadInt32();
		existingInstance.UnlockedDoorCount = input.ReadInt32();
		existingInstance.ScriptIds = input.ReadObject(existingInstance.ScriptIds);
		existingInstance.CubeShardCount = input.ReadInt32();
		existingInstance.OtherCollectibleCount = input.ReadInt32();
		existingInstance.SplitUpCount = input.ReadInt32();
		existingInstance.SecretCount = input.ReadInt32();
		return existingInstance;
	}
}
