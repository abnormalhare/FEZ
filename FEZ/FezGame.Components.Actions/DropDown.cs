using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class DropDown : PlayerAction
{
	public const float DroppingSpeed = 0.05f;

	private SoundEffect dropLedgeSound;

	private SoundEffect dropVineSound;

	private SoundEffect dropLadderSound;

	public DropDown(Game game)
		: base(game)
	{
		base.UpdateOrder = 2;
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		dropLedgeSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LedgeDrop");
		dropVineSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/DropFromVine");
		dropLadderSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/DropFromLadder");
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.FrontClimbingLadder:
		case ActionType.BackClimbingLadder:
		case ActionType.SideClimbingLadder:
		case ActionType.FrontClimbingVine:
		case ActionType.SideClimbingVine:
		case ActionType.BackClimbingVine:
		case ActionType.GrabCornerLedge:
		case ActionType.GrabLedgeFront:
		case ActionType.GrabLedgeBack:
		case ActionType.LowerToLedge:
		case ActionType.ToCornerFront:
		case ActionType.ToCornerBack:
		case ActionType.FromCornerBack:
			if (((base.InputManager.Jump == FezButtonState.Pressed && base.InputManager.Down.IsDown()) || (base.PlayerManager.Action.IsOnLedge() && base.InputManager.Down == FezButtonState.Pressed)) && FezMath.AlmostEqual(base.InputManager.Movement.X, 0f))
			{
				base.PlayerManager.HeldInstance = null;
				base.PlayerManager.Action = ActionType.Dropping;
				base.PlayerManager.CanDoubleJump = false;
			}
			break;
		}
	}

	protected override void Begin()
	{
		base.Begin();
		if (base.PlayerManager.LastAction.IsClimbingLadder())
		{
			dropLadderSound.EmitAt(base.PlayerManager.Position);
		}
		else if (base.PlayerManager.LastAction.IsClimbingVine())
		{
			dropVineSound.EmitAt(base.PlayerManager.Position);
		}
		else if (base.PlayerManager.LastAction.IsOnLedge())
		{
			dropLedgeSound.EmitAt(base.PlayerManager.Position);
			base.GomezService.OnDropLedge();
			Vector3 position = base.PlayerManager.Position;
			if (base.PlayerManager.LastAction == ActionType.GrabCornerLedge || base.PlayerManager.LastAction == ActionType.LowerToCornerLedge)
			{
				base.PlayerManager.Position += base.CameraManager.Viewpoint.RightVector() * -base.PlayerManager.LookingDirection.Sign() * 0.5f;
			}
			base.PhysicsManager.DetermineInBackground(base.PlayerManager, allowEnterInBackground: true, viewpointChanged: false, keepInFront: false);
			base.PlayerManager.Position = position;
		}
		if (base.PlayerManager.Grounded)
		{
			base.PlayerManager.Position -= Vector3.UnitY * 0.01f;
			base.PlayerManager.Velocity -= 0.0075000003f * Vector3.UnitY;
		}
		else
		{
			base.PlayerManager.Velocity = Vector3.Zero;
		}
		if (base.PlayerManager.LastAction == ActionType.GrabCornerLedge)
		{
			Vector3 vector = base.CameraManager.Viewpoint.RightVector();
			base.PlayerManager.Position += vector * -base.PlayerManager.LookingDirection.Sign() * (15f / 32f);
			base.PlayerManager.ForceOverlapsDetermination();
			NearestTriles nearestTriles = base.LevelManager.NearestTrile(base.PlayerManager.Position - 0.002f * Vector3.UnitY);
			if (nearestTriles.Surface != null && nearestTriles.Surface.Trile.ActorSettings.Type == ActorType.Vine)
			{
				base.PlayerManager.Action = ActionType.SideClimbingVine;
			}
		}
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.Dropping;
	}
}
