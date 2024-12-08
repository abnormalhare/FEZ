using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class IdleRestarter : GameComponent
{
	private const float Timeout = 1f;

	private float counter;

	[ServiceDependency]
	public ISpeechBubbleManager SpeechBubble { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	public IdleRestarter(Game game)
		: base(game)
	{
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (InputManager.AnyButtonPressed() || InputManager.Down != 0 || InputManager.Up != 0 || InputManager.Left != 0 || InputManager.Right != 0)
		{
			counter = 0f;
		}
		else if (Intro.Instance == null && !GameState.InCutscene && (PlayerManager.CanControl || !SpeechBubble.Hidden || GameState.InMenuCube || GameState.InMap))
		{
			counter += (float)gameTime.ElapsedGameTime.TotalMinutes;
			if (counter >= 1f)
			{
				GameState.Restart();
				counter = 0f;
			}
		}
	}
}
