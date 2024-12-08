using System.Collections.Generic;
using System.Linq;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class CryptHost : GameComponent
{
	private static readonly int[] VolumeSequence = new int[4] { 12, 36, 42, 14 };

	private readonly List<int> TraversedVolumes = new List<int>();

	private bool isHooked;

	[ServiceDependency]
	public ILevelService LevelService { get; set; }

	[ServiceDependency]
	public IGomezService Gomez { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	public CryptHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Enabled = false;
		TryInitialize();
		LevelManager.LevelChanging += TryInitialize;
		LevelManager.LevelChanged += TryHook;
	}

	private void TryInitialize()
	{
		if (isHooked)
		{
			Gomez.EnteredDoor -= CheckWinCondition;
			isHooked = false;
		}
		if (LevelManager.Name != "CRYPT")
		{
			TraversedVolumes.Clear();
			return;
		}
		if (LevelManager.LastLevelName == "CRYPT")
		{
			TraversedVolumes.Add(PlayerManager.DoorVolume.Value);
			if (TraversedVolumes.Count > 4)
			{
				TraversedVolumes.RemoveAt(0);
			}
			for (int i = 0; i < TraversedVolumes.Count; i++)
			{
				if (VolumeSequence[TraversedVolumes.Count - 1 - i] != TraversedVolumes[TraversedVolumes.Count - 1 - i])
				{
					TraversedVolumes.Clear();
					break;
				}
			}
		}
		else
		{
			TraversedVolumes.Clear();
		}
		int[] array = LevelManager.Scripts.Keys.Except(new int[4] { 0, 1, 2, 3 }).ToArray();
		foreach (int key in array)
		{
			LevelManager.Scripts.Remove(key);
		}
		foreach (Volume value in LevelManager.Volumes.Values)
		{
			if (value.Id <= 1 || (value.Id == 14 && TraversedVolumes.Count == 3))
			{
				continue;
			}
			int num = IdentifierPool.FirstAvailable(LevelManager.Scripts);
			int num2 = RandomHelper.InList(LevelManager.Volumes.Keys.Except(new int[3] { 0, 1, value.Id }));
			Script script = new Script();
			script.Id = num;
			script.Triggers.Add(new ScriptTrigger
			{
				Event = "Enter",
				Object = new Entity
				{
					Type = "Volume",
					Identifier = value.Id
				}
			});
			script.Actions.Add(new ScriptAction
			{
				Operation = "ChangeLevelToVolume",
				Arguments = new string[4]
				{
					"CRYPT",
					num2.ToString(),
					"True",
					"False"
				},
				Object = new Entity
				{
					Type = "Level"
				}
			});
			Script script2 = script;
			foreach (ScriptAction action in script2.Actions)
			{
				action.Process();
			}
			LevelManager.Scripts.Add(num, script2);
		}
		LevelManager.Scripts[2].Disabled = TraversedVolumes.Count != 3;
	}

	private void TryHook()
	{
		if (TraversedVolumes.Count == 3)
		{
			Gomez.EnteredDoor += CheckWinCondition;
			isHooked = true;
		}
	}

	private void CheckWinCondition()
	{
		if (PlayerManager.DoorVolume.Value == 14)
		{
			LevelManager.Scripts[3].ScheduleEvalulation = true;
		}
		Gomez.EnteredDoor -= CheckWinCondition;
		isHooked = false;
	}
}
