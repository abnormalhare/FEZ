using System.Collections.Generic;
using System.Linq;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class BitDoorsHost : GameComponent
{
	private readonly List<BitDoorState> BitDoors = new List<BitDoorState>();

	private readonly List<int> ToReactivate = new List<int>();

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IBitDoorService BitDoorService { private get; set; }

	public BitDoorsHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanging += InitBitDoors;
		if (LevelManager.Name != null)
		{
			InitBitDoors();
		}
	}

	private void InitBitDoors()
	{
		ToReactivate.Clear();
		BitDoors.Clear();
		foreach (ArtObjectInstance item in LevelManager.ArtObjects.Values.Where((ArtObjectInstance x) => x.ArtObject.ActorType.IsBitDoor()))
		{
			BitDoors.Add(new BitDoorState(item));
		}
		foreach (int inactiveArtObject in GameState.SaveData.ThisLevel.InactiveArtObjects)
		{
			if (inactiveArtObject >= 0 && LevelManager.ArtObjects.TryGetValue(inactiveArtObject, out var door) && door.ArtObject.ActorType.IsBitDoor())
			{
				door.Position += BitDoors.First((BitDoorState x) => x.AoInstance == door).GetOpenOffset();
				door.ActorSettings.Inactive = true;
				ToReactivate.Add(inactiveArtObject);
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMap || GameState.Loading || !CameraManager.ActionRunning || !CameraManager.Viewpoint.IsOrthographic() || BitDoors.Count == 0)
		{
			return;
		}
		if (ToReactivate.Count > 0)
		{
			foreach (int item in ToReactivate)
			{
				BitDoorService.OnOpen(item);
			}
			ToReactivate.Clear();
		}
		foreach (BitDoorState bitDoor in BitDoors)
		{
			bitDoor.Update(gameTime.ElapsedGameTime);
		}
	}
}
