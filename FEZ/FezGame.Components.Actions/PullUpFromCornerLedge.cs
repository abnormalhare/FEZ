using System;
using FezEngine.Components;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

public class PullUpFromCornerLedge : PlayerAction
{
	private Vector3 camOrigin;

	private Vector3 gomezDelta;

	private SoundEffect pullSound;

	private SoundEffect landSound;

	public PullUpFromCornerLedge(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		pullSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/CornerLedgeHoist");
		landSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/LedgeLand");
	}

	protected override void TestConditions()
	{
		ActionType action = base.PlayerManager.Action;
		if (action == ActionType.GrabCornerLedge && ((base.InputManager.Jump == FezButtonState.Pressed && base.InputManager.Movement.X != (float)(-base.PlayerManager.LookingDirection.Sign()) && !base.InputManager.Down.IsDown()) || base.InputManager.Up == FezButtonState.Pressed || (base.InputManager.Up == FezButtonState.Down && base.PlayerManager.Animation.Timing.Ended && base.PlayerManager.LastAction != ActionType.SideClimbingVine)) && base.LevelManager.NearestTrile(base.PlayerManager.HeldInstance.Center).Deep != null)
		{
			base.PlayerManager.Action = ActionType.PullUpCornerLedge;
		}
	}

	protected override void Begin()
	{
		base.Begin();
		pullSound.EmitAt(base.PlayerManager.Position);
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		camOrigin = base.CameraManager.Center;
		gomezDelta = base.PlayerManager.Size * (vector + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor)) / 2f;
		Waiters.Wait(0.5799999833106995, delegate
		{
			landSound.EmitAt(base.PlayerManager.Position);
		});
		base.GomezService.OnHoist();
	}

	protected override bool Act(TimeSpan elapsed)
	{
		Vector3 vector = base.CameraManager.Viewpoint.RightVector() * base.PlayerManager.LookingDirection.Sign();
		float num = 4f * (float)((!base.LevelManager.Descending) ? 1 : (-1)) / base.CameraManager.PixelsPerTrixel;
		Vector3 vector2 = (-vector + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor)) * base.PlayerManager.HeldInstance.TransformedSize / 2f + gomezDelta + num * Vector3.UnitY;
		Vector3 value = base.PlayerManager.HeldInstance.Center + vector2;
		if (!base.CameraManager.StickyCam && !base.CameraManager.Constrained && !base.CameraManager.PanningConstraints.HasValue)
		{
			base.CameraManager.Center = Vector3.Lerp(camOrigin, value, base.PlayerManager.Animation.Timing.NormalizedStep);
		}
		base.PlayerManager.SplitUpCubeCollectorOffset = gomezDelta * base.PlayerManager.Animation.Timing.NormalizedStep;
		Vector3 vector3 = (-vector + Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor)) * base.PlayerManager.HeldInstance.TransformedSize / 2f;
		base.PlayerManager.Position = base.PlayerManager.HeldInstance.Center + vector3;
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			base.PlayerManager.Position += gomezDelta;
			base.PlayerManager.HeldInstance = null;
			base.PlayerManager.SplitUpCubeCollectorOffset = Vector3.Zero;
			base.PhysicsManager.DetermineInBackground(base.PlayerManager, !base.PlayerManager.IsOnRotato, viewpointChanged: true, !base.PlayerManager.Climbing);
			base.PlayerManager.Position += 0.5f * Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor);
			base.PlayerManager.Velocity -= Vector3.UnitY * Math.Sign(base.CollisionManager.GravityFactor);
			base.PhysicsManager.Update(base.PlayerManager);
			base.PlayerManager.Action = ActionType.Idle;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.PullUpCornerLedge;
	}
}
