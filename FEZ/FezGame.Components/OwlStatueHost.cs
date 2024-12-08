using System;
using System.Globalization;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

internal class OwlStatueHost : GameComponent
{
	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IOwlService OwlService { get; set; }

	public OwlStatueHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		base.Enabled = LevelManager.Name == "OWL";
		if (!base.Enabled)
		{
			return;
		}
		int num;
		try
		{
			num = int.Parse(GameState.SaveData.ThisLevel.ScriptingState);
		}
		catch (Exception)
		{
			num = 0;
		}
		int collectedOwls = GameState.SaveData.CollectedOwls;
		int num2 = 0;
		foreach (NpcInstance value in LevelManager.NonPlayerCharacters.Values)
		{
			if (value.ActorType == ActorType.Owl)
			{
				if (collectedOwls <= num2)
				{
					ServiceHelper.RemoveComponent(value.State);
				}
				else
				{
					(value.State as GameNpcState).ForceVisible = true;
					(value.State as GameNpcState).IsNightForOwl = num2 < num;
				}
				num2++;
			}
		}
		if (num == 4 && GameState.SaveData.ThisLevel.FilledConditions.SecretCount == 0)
		{
			Waiters.Wait(() => !GameState.Loading && !GameState.FarawaySettings.InTransition, delegate
			{
				OwlService.OnOwlLanded();
			});
			base.Enabled = false;
		}
		GameState.SaveData.ThisLevel.ScriptingState = collectedOwls.ToString(CultureInfo.InvariantCulture);
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap)
		{
			return;
		}
		foreach (NpcInstance value in LevelManager.NonPlayerCharacters.Values)
		{
			if (value.State.CurrentAction == NpcAction.Land)
			{
				OwlService.OnOwlLanded();
				base.Enabled = false;
				break;
			}
		}
	}
}
