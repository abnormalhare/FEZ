using System;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class ToClimbTransition : PlayerAction
{
	private TimeSpan? sinceGrabbed;

	private SoundEffect grabLadderSound;

	private SoundEffect grabVineSound;

	public ToClimbTransition(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		grabLadderSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/GrabLadder");
		grabVineSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/GrabVine");
	}

	protected override void Begin()
	{
		base.PlayerManager.Velocity = Vector3.Zero;
		sinceGrabbed = TimeSpan.Zero;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (sinceGrabbed.HasValue)
		{
			sinceGrabbed += elapsed;
			bool flag = base.PlayerManager.Action == ActionType.JumpToClimb || base.PlayerManager.Action == ActionType.JumpToSideClimb;
			if (sinceGrabbed.Value.TotalSeconds >= (flag ? 0.16 : 0.32))
			{
				base.PlayerManager.Velocity = Vector3.Zero;
				if (base.PlayerManager.NextAction.IsClimbingLadder())
				{
					grabLadderSound.EmitAt(base.PlayerManager.Position);
				}
				else if (base.PlayerManager.NextAction.IsClimbingVine())
				{
					grabVineSound.EmitAt(base.PlayerManager.Position);
				}
				sinceGrabbed = null;
			}
		}
		if (base.PlayerManager.NextAction.IsClimbingLadder() || base.PlayerManager.NextAction == ActionType.SideClimbingVine)
		{
			base.PlayerManager.Position = Vector3.Lerp(base.PlayerManager.Position, base.PlayerManager.Position * Vector3.UnitY + (base.PlayerManager.HeldInstance.Position + FezMath.HalfVector) * FezMath.XZMask, Easing.EaseIn(FezMath.Saturate(base.PlayerManager.Animation.Timing.NormalizedStep * 2f), EasingType.Quadratic));
		}
		if (base.PlayerManager.Velocity.Y > 0f)
		{
			Vector3 vector = 3.15f * (float)Math.Sign(base.CollisionManager.GravityFactor) * 0.15f * (float)elapsed.TotalSeconds * -Vector3.UnitY;
			base.PlayerManager.Velocity += vector;
		}
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			if (base.PlayerManager.NextAction == ActionType.None)
			{
				throw new InvalidOperationException();
			}
			base.PlayerManager.Action = base.PlayerManager.NextAction;
			base.PlayerManager.NextAction = ActionType.None;
			return false;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.IdleToClimb && type != ActionType.JumpToClimb && type != ActionType.IdleToFrontClimb && type != ActionType.IdleToSideClimb)
		{
			return type == ActionType.JumpToSideClimb;
		}
		return true;
	}
}
