using System;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class Fall : PlayerAction
{
	private static readonly TimeSpan DoubleJumpTime = TimeSpan.FromSeconds(0.1);

	private const float MaxVelocity = 5.0936246f;

	public const float AirControl = 0.15f;

	public const float Gravity = 3.15f;

	private SoundEffect sFall;

	private SoundEmitter eFall;

	public Fall(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		sFall = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/FallThroughAir");
	}

	public override void Update(GameTime gameTime)
	{
		if (base.GameState.Loading)
		{
			return;
		}
		if (FezMath.AlmostEqual(base.PlayerManager.Velocity.Y, 0f))
		{
			if (eFall != null && !eFall.Dead)
			{
				eFall.FadeOutAndDie(0.1f);
				eFall = null;
			}
		}
		else
		{
			if (eFall == null || eFall.Dead)
			{
				eFall = sFall.EmitAt(base.PlayerManager.Position, loop: true, 0f, 0f);
			}
			eFall.Position = base.PlayerManager.Position;
			eFall.VolumeFactor = Easing.EaseIn(FezMath.Saturate((0f - base.PlayerManager.Velocity.Y) / 0.4f), EasingType.Quadratic);
		}
		base.Update(gameTime);
	}

	protected override bool Act(TimeSpan elapsed)
	{
		base.PlayerManager.AirTime += elapsed;
		bool flag = base.CollisionManager.GravityFactor < 0f;
		Vector3 vector = 3.15f * base.CollisionManager.GravityFactor * 0.15f * (float)elapsed.TotalSeconds * -Vector3.UnitY;
		if (base.PlayerManager.Action == ActionType.Suffering)
		{
			vector /= 2f;
		}
		base.PlayerManager.Velocity += vector;
		bool flag2 = base.PlayerManager.CarriedInstance != null;
		if (!base.PlayerManager.Grounded && base.PlayerManager.Action != ActionType.Suffering)
		{
			float x = base.InputManager.Movement.X;
			base.PlayerManager.Velocity += Vector3.Transform(Vector3.UnitX * x, base.CameraManager.Rotation) * 0.15f * 4.7f * (float)elapsed.TotalSeconds * 0.15f;
			if (flag ? (base.PlayerManager.Velocity.Y > 0f) : (base.PlayerManager.Velocity.Y < 0f))
			{
				base.PlayerManager.CanDoubleJump &= base.PlayerManager.AirTime < DoubleJumpTime;
			}
		}
		else
		{
			base.PlayerManager.CanDoubleJump = true;
			base.PlayerManager.AirTime = TimeSpan.Zero;
		}
		if (!base.PlayerManager.Grounded && (flag ? (base.PlayerManager.Velocity.Y > 0f) : (base.PlayerManager.Velocity.Y < 0f)) && !flag2 && !base.PlayerManager.Action.PreventsFall() && base.PlayerManager.Action != ActionType.Falling)
		{
			base.PlayerManager.Action = ActionType.Falling;
		}
		if (base.PlayerManager.GroundedVelocity.HasValue)
		{
			float num = 5.0936246f * (float)elapsed.TotalSeconds;
			float num2 = num / 1.5f * (0.5f + Math.Abs(base.CollisionManager.GravityFactor) * 1.5f) / 2f;
			if (base.PlayerManager.CarriedInstance != null && base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsHeavy())
			{
				num *= 0.7f;
				num2 *= 0.7f;
			}
			Vector3 vector2 = new Vector3(Math.Min(num, Math.Max(Math.Max(Math.Abs(base.PlayerManager.GroundedVelocity.Value.X), num2), Math.Max(Math.Abs(base.PlayerManager.GroundedVelocity.Value.Z), num2))));
			base.PlayerManager.Velocity = new Vector3(MathHelper.Clamp(base.PlayerManager.Velocity.X, 0f - vector2.X, vector2.X), base.PlayerManager.Velocity.Y, MathHelper.Clamp(base.PlayerManager.Velocity.Z, 0f - vector2.Z, vector2.Z));
		}
		return base.PlayerManager.Action == ActionType.Falling;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (!type.DefiesGravity())
		{
			return !base.PlayerManager.Hidden;
		}
		return false;
	}
}
