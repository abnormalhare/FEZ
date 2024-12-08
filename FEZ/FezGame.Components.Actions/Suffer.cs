using System;
using FezEngine.Components;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

public class Suffer : PlayerAction
{
	private static readonly TimeSpan HurtTime = TimeSpan.FromSeconds(1.0);

	private const float RepelStrength = 0.0625f;

	private TimeSpan sinceHurt;

	private bool causedByHurtActor;

	private bool doneFor;

	private ScreenFade fade;

	public Suffer(Game game)
		: base(game)
	{
	}

	protected override void TestConditions()
	{
		ActionType action = base.PlayerManager.Action;
		if (action == ActionType.Dying || action == ActionType.Suffering || action == ActionType.SuckedIn)
		{
			return;
		}
		bool flag = false;
		PointCollision[] cornerCollision = base.PlayerManager.CornerCollision;
		for (int i = 0; i < cornerCollision.Length; i++)
		{
			PointCollision pointCollision = cornerCollision[i];
			flag |= pointCollision.Instances.Surface != null && pointCollision.Instances.Surface.Trile.ActorSettings.Type == ActorType.Hurt;
			if (flag)
			{
				break;
			}
		}
		if (flag)
		{
			base.PlayerManager.Action = ActionType.Suffering;
			causedByHurtActor = true;
			doneFor = base.PlayerManager.RespawnPosition.Y < base.LevelManager.WaterHeight - 0.25f;
			fade = null;
		}
	}

	protected override void Begin()
	{
		base.Begin();
		if (base.PlayerManager.CanControl)
		{
			if (base.PlayerManager.HeldInstance != null)
			{
				base.PlayerManager.HeldInstance = null;
				base.PlayerManager.Action = ActionType.Idle;
				base.PlayerManager.Action = ActionType.Suffering;
			}
			base.PlayerManager.CarriedInstance = null;
			if (!causedByHurtActor)
			{
				base.PlayerManager.Velocity = Vector3.Zero;
			}
			else
			{
				base.PlayerManager.Velocity = 0.0625f * (base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.GetOpposite().Sign() + Vector3.UnitY);
			}
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (!base.PlayerManager.CanControl)
		{
			return true;
		}
		if (fade == null && sinceHurt.TotalSeconds > (double)(doneFor ? 1.25f : 1f))
		{
			sinceHurt = TimeSpan.Zero;
			causedByHurtActor = false;
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
				base.PlayerManager.Action = ActionType.Idle;
			}
		}
		else
		{
			sinceHurt += elapsed;
			base.PlayerManager.BlinkSpeed = Easing.EaseIn(sinceHurt.TotalSeconds / 1.25, EasingType.Cubic) * 1.5f;
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
		return type == ActionType.Suffering;
	}
}
