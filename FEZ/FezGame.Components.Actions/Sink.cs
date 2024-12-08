using System;
using FezEngine.Components;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

internal class Sink : PlayerAction
{
	private SoundEffect burnSound;

	private SoundEffect drownSound;

	private TimeSpan sinceStarted;

	private ScreenFade fade;

	private bool doneFor;

	public Sink(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		burnSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/BurnInLava");
		drownSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/DrownToxic");
	}

	protected override void Begin()
	{
		base.PlayerManager.CarriedInstance = null;
		base.PlayerManager.Velocity = new Vector3(0f, -0.005f, 0f);
		if (base.LevelManager.WaterType == LiquidType.Lava)
		{
			burnSound.EmitAt(base.PlayerManager.Position);
		}
		else if (base.LevelManager.WaterType == LiquidType.Sewer)
		{
			drownSound.EmitAt(base.PlayerManager.Position);
		}
		sinceStarted = TimeSpan.Zero;
		doneFor = base.PlayerManager.RespawnPosition.Y < base.LevelManager.WaterHeight - 0.25f;
	}

	protected override void End()
	{
		fade = null;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		sinceStarted += elapsed;
		if (fade == null && sinceStarted.TotalSeconds > (double)(doneFor ? 1.25f : 2f))
		{
			if (doneFor)
			{
				fade = new ScreenFade(ServiceHelper.Game)
				{
					FromColor = ColorEx.TransparentBlack,
					ToColor = Color.Black,
					Duration = 1f
				};
				ServiceHelper.AddComponent(fade);
				ScreenFade screenFade = fade;
				screenFade.Faded = (Action)Delegate.Combine(screenFade.Faded, new Action(Respawn));
			}
			else
			{
				base.PlayerManager.Respawn();
			}
		}
		else
		{
			base.PlayerManager.BlinkSpeed = Easing.EaseIn(sinceStarted.TotalSeconds / (double)(doneFor ? 1.25f : 2f), EasingType.Cubic) * 1.5f;
		}
		return true;
	}

	private void Respawn()
	{
		ServiceHelper.AddComponent(new ScreenFade(ServiceHelper.Game)
		{
			FromColor = Color.Black,
			ToColor = ColorEx.TransparentBlack,
			Duration = 1.5f
		});
		base.GameState.LoadSaveFile(delegate
		{
			base.GameState.Loading = true;
			base.LevelManager.ChangeLevel(base.LevelManager.Name);
			base.GameState.ScheduleLoadEnd = true;
			base.PlayerManager.RespawnAtCheckpoint();
			base.LevelMaterializer.ForceCull();
		});
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.Sinking;
	}
}
