using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

public class NpcHost : GameComponent
{
	protected readonly List<NpcState> NpcStates = new List<NpcState>();

	[ServiceDependency]
	public ILevelManager LevelManager { protected get; set; }

	protected NpcHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += LoadCharacters;
		LoadCharacters();
	}

	private void LoadCharacters()
	{
		foreach (NpcState npcState2 in NpcStates)
		{
			ServiceHelper.RemoveComponent(npcState2);
		}
		NpcStates.Clear();
		foreach (NpcInstance value in LevelManager.NonPlayerCharacters.Values)
		{
			NpcState npcState = CreateNpcState(value);
			if (npcState != null)
			{
				ServiceHelper.AddComponent(npcState);
				npcState.Initialize();
				NpcStates.Add(npcState);
			}
		}
	}

	protected virtual NpcState CreateNpcState(NpcInstance npc)
	{
		return new NpcState(base.Game, npc);
	}
}
