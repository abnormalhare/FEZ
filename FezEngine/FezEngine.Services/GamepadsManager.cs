using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public class GamepadsManager : GameComponent, IGamepadsManager
{
	private readonly Dictionary<PlayerIndex, GamepadState> gamepadStates = new Dictionary<PlayerIndex, GamepadState>(PlayerIndexComparer.Default);

	public GamepadState this[PlayerIndex index] => gamepadStates[index];

	public GamepadsManager(Game game, bool enabled = true)
		: base(game)
	{
		base.Enabled = enabled;
		if (base.Enabled)
		{
			gamepadStates.Add(PlayerIndex.One, new GamepadState(PlayerIndex.One));
			gamepadStates.Add(PlayerIndex.Two, new GamepadState(PlayerIndex.Two));
			gamepadStates.Add(PlayerIndex.Three, new GamepadState(PlayerIndex.Three));
			gamepadStates.Add(PlayerIndex.Four, new GamepadState(PlayerIndex.Four));
		}
	}

	public override void Update(GameTime gameTime)
	{
		TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
		GamepadState.AnyConnected = false;
		gamepadStates[PlayerIndex.One].Update(elapsedGameTime);
		gamepadStates[PlayerIndex.Two].Update(elapsedGameTime);
		gamepadStates[PlayerIndex.Three].Update(elapsedGameTime);
		gamepadStates[PlayerIndex.Four].Update(elapsedGameTime);
	}
}
