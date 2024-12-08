using System;
using FezEngine;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class ReadSign : PlayerAction
{
	private string signText;

	private SoundEffect sTextNext;

	[ServiceDependency]
	public ISpeechBubbleManager SpeechBubble { private get; set; }

	public ReadSign(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		sTextNext = base.CMProvider.Global.Load<SoundEffect>("Sounds/Ui/TextNext");
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.Idle:
		case ActionType.LookingLeft:
		case ActionType.LookingRight:
		case ActionType.LookingUp:
		case ActionType.LookingDown:
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.Sliding:
		case ActionType.Landing:
		case ActionType.Teetering:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
			if (IsOnSign() && base.InputManager.CancelTalk == FezButtonState.Pressed)
			{
				SpeechBubble.Origin = base.PlayerManager.Position;
				SpeechBubble.ChangeText(GameText.GetString(signText));
				base.PlayerManager.Action = ActionType.ReadingSign;
				base.InputManager.PressedToDown();
			}
			break;
		}
	}

	protected override void Begin()
	{
		base.Begin();
		base.GomezService.OnReadSign();
		base.PlayerManager.Velocity *= Vector3.UnitY;
	}

	private bool IsOnSign()
	{
		if (!TestSignCollision(VerticalDirection.Up))
		{
			return TestSignCollision(VerticalDirection.Down);
		}
		return true;
	}

	private bool TestSignCollision(VerticalDirection direction)
	{
		TrileInstance surface = base.PlayerManager.AxisCollision[direction].Surface;
		if (surface == null)
		{
			return false;
		}
		Trile trile = surface.Trile;
		FaceOrientation faceOrientation = FezMath.OrientationFromPhi(trile.ActorSettings.Face.ToPhi() + surface.Phi);
		int num;
		if (trile.ActorSettings.Type == ActorType.Sign && faceOrientation == base.CameraManager.VisibleOrientation && surface.ActorSettings != null)
		{
			num = ((!string.IsNullOrEmpty(surface.ActorSettings.SignText)) ? 1 : 0);
			if (num != 0)
			{
				signText = surface.ActorSettings.SignText;
			}
		}
		else
		{
			num = 0;
		}
		return (byte)num != 0;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.InputManager.CancelTalk == FezButtonState.Pressed)
		{
			sTextNext.Emit();
			SpeechBubble.Hide();
			base.PlayerManager.Action = ActionType.Idle;
			base.InputManager.PressedToDown();
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.ReadingSign;
	}
}
