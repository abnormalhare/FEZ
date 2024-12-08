using FezEngine.Components;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class GameSequencer : Sequencer
{
	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	public GameSequencer(Game game)
		: base(game)
	{
	}

	protected override void OnDisappear(TrileInstance crystal)
	{
		if (PlayerManager.HeldInstance == crystal)
		{
			PlayerManager.HeldInstance = null;
			PlayerManager.Action = ActionType.Idle;
		}
	}
}
