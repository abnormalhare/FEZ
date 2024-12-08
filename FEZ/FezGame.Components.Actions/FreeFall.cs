using System;
using System.Collections.Generic;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class FreeFall : PlayerAction
{
	private const float FreeFallEnd = 36f;

	private const float CamPanUp = 5f;

	private const float CamFollowEnd = 27f;

	private SoundEffect thudSound;

	private SoundEffect panicSound;

	private SoundEmitter panicEmitter;

	private bool WasConstrained;

	private Vector3 OldConstrainedCenter;

	private int? CapEnd;

	private static readonly Dictionary<string, int> EndCaps = new Dictionary<string, int>
	{
		{ "INDUSTRIAL_SUPERSPIN", 0 },
		{ "PIVOT_ONE", 0 },
		{ "PIVOT_TWO", 7 },
		{ "INDUSTRIAL_HUB", 5 },
		{ "GRAVE_TREASURE_A", 0 },
		{ "WELL_2", 0 },
		{ "TREE_SKY", 0 },
		{ "PIVOT_THREE_CAVE", 40 },
		{ "ZU_BRIDGE", 22 },
		{ "LIGHTHOUSE_SPIN", 4 },
		{ "FRACTAL", 0 },
		{ "MINE_A", 0 },
		{ "MINE_WRAP", 4 },
		{ "BIG_TOWER", 0 },
		{ "ZU_CITY_RUINS", 5 },
		{ "CODE_MACHINE", 3 },
		{ "TELESCOPE", 5 },
		{ "GLOBE", 5 },
		{ "MEMORY_CORE", 10 },
		{ "ZU_CITY", 6 }
	};

	public float FreeFallStart
	{
		get
		{
			if (!(base.LevelManager.Name == "CLOCK") || !(base.PlayerManager.LeaveGroundPosition.Y >= 68f))
			{
				return 8f;
			}
			return 10f;
		}
	}

	public FreeFall(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		thudSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/CrashLand");
		panicSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/AirPanic");
	}

	protected override void TestConditions()
	{
		float num = base.PlayerManager.Position.Y - base.CameraManager.ViewOffset.Y;
		if (!base.PlayerManager.IgnoreFreefall && !base.PlayerManager.Grounded && !base.PlayerManager.Action.PreventsFall() && base.PlayerManager.Action != ActionType.SuckedIn && (float)Math.Sign(base.CollisionManager.GravityFactor) * (base.PlayerManager.LeaveGroundPosition.Y - base.PlayerManager.OffsetAtLeaveGround - num) > FreeFallStart)
		{
			base.PlayerManager.Action = ActionType.FreeFalling;
		}
	}

	protected override void Begin()
	{
		base.Begin();
		if (panicEmitter != null)
		{
			panicEmitter.FadeOutAndDie(0f);
			panicEmitter = null;
		}
		(panicEmitter = panicSound.EmitAt(base.PlayerManager.Position)).NoAttenuation = true;
		if (base.PlayerManager.CarriedInstance != null)
		{
			base.PlayerManager.CarriedInstance.PhysicsState.Velocity = base.PlayerManager.Velocity * 0.95f;
			base.PlayerManager.CarriedInstance = null;
		}
		WasConstrained = base.CameraManager.Constrained;
		if (WasConstrained)
		{
			OldConstrainedCenter = base.CameraManager.Center;
		}
		base.CameraManager.Constrained = true;
		if (EndCaps.TryGetValue(base.LevelManager.Name, out var value))
		{
			CapEnd = value;
		}
		else
		{
			CapEnd = null;
		}
	}

	protected override void End()
	{
		base.End();
		if (!WasConstrained)
		{
			base.CameraManager.Constrained = false;
		}
		else
		{
			base.CameraManager.Center = OldConstrainedCenter;
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		float num = base.PlayerManager.Position.Y - base.CameraManager.ViewOffset.Y;
		float num2 = (float)Math.Sign(base.CollisionManager.GravityFactor) * (base.PlayerManager.LeaveGroundPosition.Y - base.PlayerManager.OffsetAtLeaveGround - num);
		float num3 = base.CameraManager.Radius / base.CameraManager.AspectRatio;
		float num4 = 36f;
		if (CapEnd.HasValue)
		{
			num4 = Math.Min(36f, base.PlayerManager.RespawnPosition.Y - (float)CapEnd.Value);
		}
		if (!base.GameState.SkipFadeOut && num2 < 27f && (!CapEnd.HasValue || base.CameraManager.Center.Y - num3 / 2f > (float)(CapEnd.Value + 1)))
		{
			base.CameraManager.Center = base.CameraManager.Center * (base.CameraManager.Viewpoint.SideMask() + base.CameraManager.Viewpoint.DepthMask()) + (base.PlayerManager.Position.Y - (num2 - FreeFallStart) / 27f * 5f) * Vector3.UnitY;
		}
		if (base.PlayerManager.Grounded)
		{
			panicEmitter.FadeOutAndDie(0f);
			panicEmitter = null;
			thudSound.EmitAt(base.PlayerManager.Position).NoAttenuation = true;
			base.InputManager.ActiveGamepad.Vibrate(VibrationMotor.RightHigh, 1.0, TimeSpan.FromSeconds(0.5), EasingType.Quadratic);
			base.InputManager.ActiveGamepad.Vibrate(VibrationMotor.LeftLow, 1.0, TimeSpan.FromSeconds(0.3499999940395355));
			base.PlayerManager.Action = ActionType.Dying;
			base.PlayerManager.Velocity *= Vector3.UnitY;
		}
		if (!base.GameState.SkipFadeOut && num2 > num4)
		{
			if (!WasConstrained)
			{
				base.CameraManager.Constrained = false;
			}
			else
			{
				base.CameraManager.Center = OldConstrainedCenter;
			}
			base.PlayerManager.Respawn();
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.FreeFalling;
	}
}
