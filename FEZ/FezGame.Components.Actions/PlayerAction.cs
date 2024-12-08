using System;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components.Actions;

public abstract class PlayerAction : DrawableGameComponent
{
	private ActionType lastAction;

	private bool wasActive;

	private bool? overridden;

	public bool IsUpdateOverridden
	{
		get
		{
			if (overridden.HasValue)
			{
				return overridden.Value;
			}
			Action<GameTime> action = Update;
			bool? flag = (overridden = action.Method.DeclaringType != typeof(PlayerAction));
			return flag.Value;
		}
	}

	protected virtual bool ViewTransitionIndependent => false;

	[ServiceDependency]
	public ISoundManager SoundManager { protected get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { protected get; set; }

	[ServiceDependency]
	public IGomezService GomezService { protected get; set; }

	[ServiceDependency]
	public ICollisionManager CollisionManager { protected get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { protected get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { protected get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { protected get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { protected get; set; }

	[ServiceDependency]
	public IGamepadsManager GamepadsManager { protected get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { protected get; set; }

	[ServiceDependency]
	public IInputManager InputManager { protected get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { protected get; set; }

	[ServiceDependency]
	public IPhysicsManager PhysicsManager { protected get; set; }

	[ServiceDependency(Optional = true)]
	public IWalkToService WalkTo { protected get; set; }

	protected PlayerAction(Game game)
		: base(game)
	{
	}

	public void Reset()
	{
		wasActive = false;
		lastAction = ActionType.None;
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.Loading || GameState.InCutscene || GameState.InMap || GameState.InFpsMode || GameState.InMenuCube || (!ViewTransitionIndependent && !CameraManager.ActionRunning))
		{
			return;
		}
		if (!PlayerManager.CanControl)
		{
			InputManager.SaveState();
			InputManager.Reset();
		}
		TestConditions();
		bool flag = IsActionAllowed(PlayerManager.Action);
		SyncAnimation(flag);
		if (flag)
		{
			if (!wasActive)
			{
				Begin();
			}
			TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
			if (Act(elapsedGameTime))
			{
				PlayerManager.Animation.Timing.Update(elapsedGameTime, (1f + Math.Abs(CollisionManager.GravityFactor)) / 2f);
			}
		}
		else if (wasActive)
		{
			End();
		}
		SyncAnimation(flag);
		if (!PlayerManager.CanControl)
		{
			InputManager.RecoverState();
		}
		wasActive = flag;
	}

	public void LightUpdate(GameTime gameTime, bool actionNotRunning)
	{
		if (!ViewTransitionIndependent && actionNotRunning)
		{
			return;
		}
		TestConditions();
		bool flag = IsActionAllowed(PlayerManager.Action);
		SyncAnimation(flag);
		if (flag)
		{
			if (!wasActive)
			{
				Begin();
			}
			TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
			if (Act(elapsedGameTime))
			{
				PlayerManager.Animation.Timing.Update(elapsedGameTime, (1f + Math.Abs(CollisionManager.GravityFactor)) / 2f);
			}
		}
		else if (wasActive)
		{
			End();
		}
		SyncAnimation(flag);
		wasActive = flag;
	}

	protected void SyncAnimation(bool isActive)
	{
		if (isActive && lastAction != PlayerManager.Action)
		{
			AnimatedTexture animation = PlayerManager.Animation;
			AnimatedTexture animation2 = PlayerManager.GetAnimation(PlayerManager.Action);
			PlayerManager.Animation = animation2;
			animation2.Timing.StartFrame = PlayerManager.Action.GetStartFrame();
			int endFrame = PlayerManager.Action.GetEndFrame();
			animation2.Timing.EndFrame = ((endFrame != -1) ? endFrame : animation2.Timing.InitialEndFrame);
			animation2.Timing.Loop = PlayerManager.Action.IsAnimationLooping();
			if ((animation2 != animation || !animation2.Timing.Loop) && ((lastAction != ActionType.Pushing && lastAction != ActionType.DropHeavyTrile && lastAction != ActionType.DropTrile) || PlayerManager.Action != ActionType.Grabbing))
			{
				animation2.Timing.Restart();
			}
			if (PlayerManager.Action == ActionType.GrabCornerLedge && lastAction == ActionType.LowerToCornerLedge)
			{
				animation2.Timing.Step = animation2.Timing.EndStep - 0.001f;
			}
			else if (PlayerManager.Action == ActionType.ThrowingHeavy && PlayerManager.LastAction == ActionType.CarryHeavyJump)
			{
				animation2.Timing.Step = animation2.Timing.EndStep * (3f / (float)animation2.Timing.EndFrame);
			}
			else if ((PlayerManager.Action == ActionType.GrabLedgeFront || PlayerManager.Action == ActionType.GrabLedgeBack) && lastAction.IsOnLedge())
			{
				animation2.Timing.Step = animation2.Timing.EndStep - 0.001f;
			}
			else if (FezMath.In(lastAction, ActionType.ToCornerBack, ActionType.ToCornerFront, ActionType.GrabLedgeFront, ActionType.GrabLedgeBack, ActionTypeComparer.Default) && PlayerManager.Action == ActionType.GrabCornerLedge)
			{
				animation2.Timing.Step = animation2.Timing.EndStep - 0.001f;
			}
			else if (PlayerManager.Action == ActionType.GrabTombstone && PlayerManager.LastAction == ActionType.PivotTombstone)
			{
				animation2.Timing.Step = animation2.Timing.EndStep - 0.001f;
			}
			else if (PlayerManager.Action == ActionType.TurnToBell && PlayerManager.LastAction == ActionType.HitBell)
			{
				animation2.Timing.Step = animation2.Timing.EndStep - 0.001f;
			}
		}
		lastAction = PlayerManager.Action;
	}

	protected virtual void TestConditions()
	{
	}

	protected virtual void Begin()
	{
	}

	protected virtual void End()
	{
	}

	protected virtual bool Act(TimeSpan elapsed)
	{
		return false;
	}

	protected abstract bool IsActionAllowed(ActionType type);
}
