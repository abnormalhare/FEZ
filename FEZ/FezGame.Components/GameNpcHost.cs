using FezEngine.Components;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class GameNpcHost : NpcHost
{
	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public GameNpcHost(Game game)
		: base(game)
	{
	}

	protected override NpcState CreateNpcState(NpcInstance npc)
	{
		if (npc.ActorType == ActorType.Owl && GameState.SaveData.ThisLevel.InactiveNPCs.Contains(npc.Id))
		{
			return null;
		}
		return new GameNpcState(base.Game, npc);
	}
}
