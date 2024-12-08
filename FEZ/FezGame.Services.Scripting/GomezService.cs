using System;
using Common;
using FezEngine.Components.Scripting;
using FezEngine.Services.Scripting;
using FezEngine.Tools;
using FezGame.Components.Scripting;
using FezGame.Structure;

namespace FezGame.Services.Scripting;

public class GomezService : IGomezService, IScriptingBase
{
	public int CollectedCubes => GameState.SaveData.CubeShards;

	public int CollectedSplits => GameState.SaveData.CollectedParts;

	public bool Grounded => PlayerManager.Grounded;

	public bool IsOnLadder => PlayerManager.Action.IsClimbingLadder();

	public bool CanControl => PlayerManager.CanControl;

	public bool Visible => !PlayerManager.Hidden;

	public bool Alive
	{
		get
		{
			if (PlayerManager.Action != ActionType.Dying && PlayerManager.Action != ActionType.FreeFalling && PlayerManager.Action != ActionType.SuckedIn)
			{
				return PlayerManager.Action != ActionType.Sinking;
			}
			return false;
		}
	}

	[ServiceDependency]
	internal IScriptingManager ScriptingManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	public event Action EnteredDoor = Util.NullAction;

	public event Action Jumped = Util.NullAction;

	public event Action ClimbedLadder = Util.NullAction;

	public event Action ClimbedVine = Util.NullAction;

	public event Action LookedAround = Util.NullAction;

	public event Action LiftedObject = Util.NullAction;

	public event Action ThrewObject = Util.NullAction;

	public event Action OpenedMenuCube = Util.NullAction;

	public event Action ReadSign = Util.NullAction;

	public event Action GrabbedLedge = Util.NullAction;

	public event Action DropObject = Util.NullAction;

	public event Action DroppedLedge = Util.NullAction;

	public event Action Hoisted = Util.NullAction;

	public event Action ClimbedOverLadder = Util.NullAction;

	public event Action DroppedFromLadder = Util.NullAction;

	public event Action ReadMail = Util.NullAction;

	public event Action CollectedSplitUpCube = Util.NullAction;

	public event Action CollectedShard = Util.NullAction;

	public event Action OpenedTreasure = Util.NullAction;

	public event Action CollectedAnti = Util.NullAction;

	public event Action CollectedGlobalAnti = Util.NullAction;

	public event Action CollectedPieceOfHeart = Util.NullAction;

	public event Action Landed = Util.NullAction;

	public void OnEnterDoor()
	{
		this.EnteredDoor();
	}

	public void OnJump()
	{
		this.Jumped();
	}

	public void OnClimbLadder()
	{
		this.ClimbedLadder();
	}

	public void OnClimbVine()
	{
		this.ClimbedVine();
	}

	public void OnLookAround()
	{
		this.LookedAround();
	}

	public void OnLiftObject()
	{
		this.LiftedObject();
	}

	public void OnThrowObject()
	{
		this.ThrewObject();
	}

	public void OnDropObject()
	{
		this.DropObject();
	}

	public void OnOpenMenuCube()
	{
		this.OpenedMenuCube();
	}

	public void OnReadSign()
	{
		this.ReadSign();
	}

	public void OnGrabLedge()
	{
		this.GrabbedLedge();
	}

	public void OnHoist()
	{
		this.Hoisted();
	}

	public void OnDropLedge()
	{
		this.DroppedLedge();
	}

	public void OnClimbOverLadder()
	{
		this.ClimbedOverLadder();
	}

	public void OnDropFromLadder()
	{
		this.DroppedFromLadder();
	}

	public void OnReadMail()
	{
		this.ReadMail();
	}

	public void OnCollectedSplitUpCube()
	{
		this.CollectedSplitUpCube();
	}

	public void OnCollectedShard()
	{
		this.CollectedShard();
	}

	public void OnOpenTreasure()
	{
		this.OpenedTreasure();
	}

	public void OnCollectedPieceOfHeart()
	{
		this.CollectedPieceOfHeart();
	}

	public void OnCollectedGlobalAnti()
	{
		this.CollectedGlobalAnti();
	}

	public void OnCollectedAnti()
	{
		this.CollectedAnti();
	}

	public void OnLand()
	{
		this.Landed();
	}

	public void SetCanControl(bool controllable)
	{
		PlayerManager.CanControl = controllable;
	}

	public void SetAction(string actionName)
	{
		PlayerManager.Action = (ActionType)Enum.Parse(typeof(ActionType), actionName, ignoreCase: false);
	}

	public void SetFezVisible(bool visible)
	{
		PlayerManager.HideFez = !visible;
	}

	public void SetGomezVisible(bool visible)
	{
		PlayerManager.Hidden = !visible;
	}

	public LongRunningAction AllowEnterTunnel()
	{
		PlayerManager.TunnelVolume = ScriptingManager.EvaluatedScript.InitiatingTrigger.Object.Identifier;
		return new LongRunningAction(delegate
		{
			PlayerManager.TunnelVolume = null;
		});
	}

	public void ResetEvents()
	{
		this.EnteredDoor = Util.NullAction;
		this.Jumped = Util.NullAction;
		this.ClimbedLadder = Util.NullAction;
		this.ClimbedVine = Util.NullAction;
		this.LookedAround = Util.NullAction;
		this.LiftedObject = Util.NullAction;
		this.ThrewObject = Util.NullAction;
		this.OpenedMenuCube = Util.NullAction;
		this.ReadSign = Util.NullAction;
		this.GrabbedLedge = Util.NullAction;
		this.DropObject = Util.NullAction;
		this.DroppedLedge = Util.NullAction;
		this.Hoisted = Util.NullAction;
		this.ClimbedOverLadder = Util.NullAction;
		this.DroppedFromLadder = Util.NullAction;
		this.ReadMail = Util.NullAction;
		this.CollectedSplitUpCube = Util.NullAction;
		this.CollectedShard = Util.NullAction;
		this.OpenedTreasure = Util.NullAction;
		this.CollectedAnti = Util.NullAction;
		this.CollectedPieceOfHeart = Util.NullAction;
		this.Landed = Util.NullAction;
	}
}
