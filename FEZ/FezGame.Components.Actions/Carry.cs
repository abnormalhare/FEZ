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

public class Carry : PlayerAction
{
	private static readonly Vector2[] LightWalkOffset = new Vector2[8]
	{
		new Vector2(0f, 0f),
		new Vector2(0f, 2f),
		new Vector2(0f, 3f),
		new Vector2(0f, 2f),
		new Vector2(0f, -1f),
		new Vector2(0f, 2f),
		new Vector2(0f, 3f),
		new Vector2(0f, 2f)
	};

	private static readonly Vector2[] HeavyWalkOffset = new Vector2[8]
	{
		new Vector2(0f, -1f),
		new Vector2(0f, -3f),
		new Vector2(0f, -2f),
		new Vector2(0f, 0f),
		new Vector2(0f, -1f),
		new Vector2(0f, -3f),
		new Vector2(0f, -2f),
		new Vector2(0f, 0f)
	};

	private static readonly Vector2[] LightJumpOffset = new Vector2[8]
	{
		new Vector2(1f, -3f),
		new Vector2(0f, 3f),
		new Vector2(1f, 2f),
		new Vector2(1f, -2f),
		new Vector2(1f, 0f),
		new Vector2(1f, 2f),
		new Vector2(1f, -3f),
		new Vector2(1f, -2f)
	};

	private static readonly Vector2[] HeavyJumpOffset = new Vector2[8]
	{
		new Vector2(-1f, -3f),
		new Vector2(0f, 3f),
		new Vector2(0f, 2f),
		new Vector2(0f, 0f),
		new Vector2(0f, 0f),
		new Vector2(0f, 3f),
		new Vector2(-1f, 3f),
		new Vector2(-1f, -3f)
	};

	private const float CarryJumpStrength = 0.885f;

	private const float CarryWalkSpeed = 4.0869565f;

	private const float CarryHeavyWalkSpeed = 2.35f;

	private readonly MovementHelper movementHelper = new MovementHelper(4.0869565f, 0f, float.MaxValue);

	private SoundEffect jumpSound;

	private SoundEffect landSound;

	private bool jumpIsFall;

	private bool wasNotGrounded;

	private Vector3 offsetFromGomez;

	public Carry(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		movementHelper.Entity = base.PlayerManager;
		TimeInterpolation.RegisterCallback(AdjustCarriedInstance, 30);
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		jumpSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/Jump");
		landSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/Land");
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.CarryIdle:
		case ActionType.CarryWalk:
		case ActionType.CarryJump:
		case ActionType.CarrySlide:
		case ActionType.CarryHeavyIdle:
		case ActionType.CarryHeavyWalk:
		case ActionType.CarryHeavyJump:
		case ActionType.CarryHeavySlide:
		{
			if (base.PlayerManager.CarriedInstance == null)
			{
				break;
			}
			bool flag = base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsLight();
			bool flag2 = base.PlayerManager.Action == ActionType.CarryHeavyJump || base.PlayerManager.Action == ActionType.CarryJump;
			bool flag3 = flag2 && !base.PlayerManager.Animation.Timing.Ended;
			if (base.PlayerManager.Grounded && base.InputManager.Jump == FezButtonState.Pressed && base.InputManager.Down.IsDown() && base.PlayerManager.Ground.First.GetRotatedFace(base.CameraManager.VisibleOrientation) == CollisionType.TopOnly)
			{
				base.PlayerManager.Position -= Vector3.UnitY * base.CollisionManager.DistanceEpsilon * 2f;
				base.PlayerManager.Velocity -= 0.0075000003f * Vector3.UnitY;
				base.PlayerManager.Action = (flag ? ActionType.CarryJump : ActionType.CarryHeavyJump);
				base.PlayerManager.CanDoubleJump = false;
				break;
			}
			if ((base.PlayerManager.Grounded || base.PlayerManager.CanDoubleJump) && (!flag2 || base.PlayerManager.Animation.Timing.Frame != 0) && base.InputManager.Jump == FezButtonState.Pressed)
			{
				jumpIsFall = false;
				Jump(flag);
				break;
			}
			if (base.PlayerManager.Grounded && base.InputManager.Movement.X != 0f && !flag3)
			{
				base.PlayerManager.Action = (flag ? ActionType.CarryWalk : ActionType.CarryHeavyWalk);
				break;
			}
			if (base.PlayerManager.Action != ActionType.CarryHeavyJump && base.PlayerManager.Action != ActionType.CarryJump && !base.PlayerManager.Grounded)
			{
				jumpIsFall = true;
				base.PlayerManager.Action = (flag ? ActionType.CarryJump : ActionType.CarryHeavyJump);
			}
			if (wasNotGrounded && base.PlayerManager.Grounded)
			{
				landSound.EmitAt(base.PlayerManager.Position);
			}
			wasNotGrounded = !base.PlayerManager.Grounded;
			if (base.PlayerManager.Action != ActionType.CarryIdle && base.PlayerManager.Action != ActionType.CarryHeavyIdle && base.PlayerManager.Grounded && FezMath.AlmostEqual(base.PlayerManager.Velocity.XZ(), Vector2.Zero) && !flag3)
			{
				base.PlayerManager.Action = (flag ? ActionType.CarryIdle : ActionType.CarryHeavyIdle);
			}
			else if (base.PlayerManager.Action != ActionType.CarrySlide && base.PlayerManager.Grounded && !FezMath.AlmostEqual(base.PlayerManager.Velocity.XZ(), Vector2.Zero) && FezMath.AlmostEqual(base.InputManager.Movement, Vector2.Zero) && !flag3)
			{
				base.PlayerManager.Action = (flag ? ActionType.CarrySlide : ActionType.CarryHeavySlide);
				base.PlayerManager.Animation.Timing.Paused = false;
			}
			break;
		}
		case ActionType.CarryEnter:
			break;
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.CarriedInstance == null)
		{
			base.PlayerManager.Action = ActionType.Idle;
			return false;
		}
		bool flag = base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsLight();
		bool flag2 = base.PlayerManager.Action == ActionType.CarryHeavyJump || base.PlayerManager.Action == ActionType.CarryJump;
		if (base.PlayerManager.Action == ActionType.CarryWalk || base.PlayerManager.Action == ActionType.CarryHeavyWalk || (flag2 && base.PlayerManager.Grounded))
		{
			movementHelper.WalkAcceleration = (flag ? 4.0869565f : 2.35f);
			movementHelper.Update((float)elapsed.TotalSeconds);
		}
		float timeFactor = 1.2f;
		if (base.PlayerManager.Action == ActionType.CarryJump || base.PlayerManager.Action == ActionType.CarryHeavyJump)
		{
			timeFactor = 1f;
		}
		base.PlayerManager.Animation.Timing.Update(elapsed, timeFactor);
		if (base.PlayerManager.Action == ActionType.CarryJump || base.PlayerManager.Action == ActionType.CarryHeavyJump)
		{
			if (base.PlayerManager.Animation.Timing.Frame == 1 && base.PlayerManager.Grounded && !jumpIsFall)
			{
				jumpSound.EmitAt(base.PlayerManager.Position);
				base.PlayerManager.Velocity *= FezMath.XZMask;
				base.PlayerManager.Velocity += 0.13275f * Vector3.UnitY * (flag ? 1f : 0.75f);
			}
			else
			{
				JumpAftertouch((float)elapsed.TotalSeconds);
			}
		}
		MoveCarriedInstance();
		return false;
	}

	private void Jump(bool isLight)
	{
		base.PlayerManager.Action = (isLight ? ActionType.CarryJump : ActionType.CarryHeavyJump);
		base.PlayerManager.Animation.Timing.Restart();
		base.PlayerManager.CanDoubleJump = false;
	}

	private void JumpAftertouch(float secondsElapsed)
	{
		int frame = base.PlayerManager.Animation.Timing.Frame;
		int num = (base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsHeavy() ? 7 : 6);
		if (!base.PlayerManager.Grounded && base.PlayerManager.Velocity.Y < 0f)
		{
			base.PlayerManager.Animation.Timing.Step = Math.Max(base.PlayerManager.Animation.Timing.Step, 0.5f);
		}
		if (frame != 0 && frame < num && base.PlayerManager.Grounded)
		{
			base.PlayerManager.Animation.Timing.Step = (float)num / 8f;
		}
		else if (!base.PlayerManager.Grounded && base.PlayerManager.Velocity.Y < 0f)
		{
			base.PlayerManager.Animation.Timing.Step = Math.Min(base.PlayerManager.Animation.Timing.Step, (float)num / 8f - 0.001f);
		}
		else if (frame < num)
		{
			base.PlayerManager.Animation.Timing.Step = Math.Min(base.PlayerManager.Animation.Timing.Step, 0.499f);
		}
		if (frame != 0 && frame < num && base.InputManager.Jump == FezButtonState.Down)
		{
			base.PlayerManager.Velocity += secondsElapsed * 0.885f / 4f * Vector3.UnitY;
		}
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (type != ActionType.CarryWalk && type != ActionType.CarryIdle && type != ActionType.CarryJump && type != ActionType.CarrySlide && type != ActionType.CarryHeavyWalk && type != ActionType.CarryHeavyIdle && type != ActionType.CarryHeavyJump)
		{
			return type == ActionType.CarryHeavySlide;
		}
		return true;
	}

	private void MoveCarriedInstance()
	{
		Viewpoint view = (base.CameraManager.ActionRunning ? base.CameraManager.Viewpoint : base.CameraManager.LastViewpoint);
		Vector3 mask = view.VisibleAxis().GetMask();
		Vector3 vector = view.RightVector().Abs();
		float num = (float)base.PlayerManager.LookingDirection.Sign() * Vector3.Dot(base.CameraManager.Viewpoint.RightVector(), FezMath.XZMask);
		float num2 = -0.5f;
		float num3 = 2f - (base.PlayerManager.Size.Y / 2f + 0.001f);
		bool flag = base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsLight();
		int frame = base.PlayerManager.Animation.Timing.Frame;
		Vector3 vector3;
		switch (base.PlayerManager.Action)
		{
		case ActionType.CarryIdle:
			vector3 = Vector3.UnitY * (num3 - 0.3125f);
			num2 += num * 1f / 16f;
			break;
		case ActionType.CarryHeavyIdle:
			vector3 = Vector3.UnitY * (num3 - 0.625f);
			break;
		case ActionType.CarryHeavyJump:
		{
			Vector2 vector5 = HeavyJumpOffset[frame];
			vector3 = new Vector3(0f, num3 - 0.625f + vector5.Y * 1f / 16f, 0f);
			num2 += 0.0625f * vector5.X * num;
			break;
		}
		case ActionType.CarryJump:
		{
			Vector2 vector4 = LightJumpOffset[frame];
			vector3 = new Vector3(0f, num3 - 0.3125f + vector4.Y * 1f / 16f, 0f);
			num2 += 0.0625f * vector4.X * num;
			break;
		}
		default:
		{
			Vector2 vector2 = (flag ? LightWalkOffset[frame] : HeavyWalkOffset[frame]);
			vector3 = Vector3.UnitY * (num3 + (-7f + vector2.Y) / 16f);
			if (flag)
			{
				num2 += 0.0625f * num;
				vector3 += Vector3.UnitY * 2f / 16f;
			}
			num2 += vector2.X / 16f * num;
			break;
		}
		}
		offsetFromGomez = vector * num2 + mask * -0.5f + vector3;
	}

	private void AdjustCarriedInstance(GameTime _)
	{
		if (!base.GameState.Paused && !base.GameState.Loading && !base.GameState.InCutscene && !base.GameState.InMap && !base.GameState.InFpsMode && !base.GameState.InMenuCube && base.PlayerManager.CarriedInstance != null && IsActionAllowed(base.PlayerManager.Action))
		{
			base.PlayerManager.CarriedInstance.Position = GomezHost.Instance.InterpolatedPosition + offsetFromGomez;
			base.LevelManager.UpdateInstance(base.PlayerManager.CarriedInstance);
		}
	}
}
