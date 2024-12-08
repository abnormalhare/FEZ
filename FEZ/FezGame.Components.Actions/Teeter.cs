using System;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class Teeter : PlayerAction
{
	private SoundEffect sBegin;

	private SoundEffect sMouthOpen;

	private SoundEffect sMouthClose;

	private SoundEmitter eLast;

	private int lastFrame;

	public Teeter(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		sBegin = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/TeeterBegin");
		sMouthOpen = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/TeeterMouthOpen");
		sMouthClose = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/TeeterMouthClose");
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.Idle:
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.Dropping:
		case ActionType.Sliding:
		case ActionType.Grabbing:
		case ActionType.Pushing:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
			if (base.PlayerManager.PushedInstance == null && base.PlayerManager.CarriedInstance == null && base.PlayerManager.Grounded && base.PlayerManager.Ground.FarHigh == null && base.InputManager.Movement.X == 0f)
			{
				Vector3 b = base.CameraManager.Viewpoint.SideMask();
				TrileInstance nearLow = base.PlayerManager.Ground.NearLow;
				float num = Math.Abs(nearLow.Center.Dot(b) - base.PlayerManager.Position.Dot(b));
				if (!(num > 1f) && num > 0.45f && !base.CollisionManager.CollideEdge(nearLow.Center, Vector3.Down * Math.Sign(base.CollisionManager.GravityFactor), base.PlayerManager.Size * FezMath.XZMask / 2f, Direction2D.Vertical).AnyHit())
				{
					base.PlayerManager.Velocity *= new Vector3(0.5f, 1f, 0.5f);
					base.PlayerManager.Action = ActionType.Teetering;
				}
			}
			break;
		}
	}

	protected override void Begin()
	{
		lastFrame = -1;
		base.Begin();
	}

	protected override bool Act(TimeSpan elapsed)
	{
		int frame = base.PlayerManager.Animation.Timing.Frame;
		if (lastFrame != frame)
		{
			switch (frame)
			{
			case 0:
				eLast = sBegin.EmitAt(base.PlayerManager.Position);
				break;
			case 6:
				eLast = sMouthOpen.EmitAt(base.PlayerManager.Position);
				break;
			case 9:
				eLast = sMouthClose.EmitAt(base.PlayerManager.Position);
				break;
			}
		}
		lastFrame = frame;
		if (eLast != null && !eLast.Dead)
		{
			eLast.Position = base.PlayerManager.Position;
		}
		return true;
	}

	protected override void End()
	{
		if (eLast != null && !eLast.Dead)
		{
			eLast.Cue.Stop();
			eLast = null;
		}
		base.End();
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.Teetering;
	}
}
