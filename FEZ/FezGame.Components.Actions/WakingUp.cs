using System;
using FezEngine;
using FezEngine.Effects;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.Actions;

internal class WakingUp : PlayerAction
{
	private static readonly TimeSpan FadeTime = TimeSpan.FromSeconds(1.0);

	private readonly Mesh fadePlane;

	private TimeSpan sinceStarted;

	private bool respawned;

	private bool diedByLava;

	public WakingUp(Game game)
		: base(game)
	{
		fadePlane = new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false
		};
		fadePlane.AddFace(Vector3.One * 2f, Vector3.Zero, FaceOrientation.Front, Color.Black, centeredOnOrigin: true);
		DrawActionScheduler.Schedule(delegate
		{
			fadePlane.Effect = new DefaultEffect.VertexColored
			{
				ForcedViewMatrix = Matrix.Identity,
				ForcedProjectionMatrix = Matrix.Identity
			};
		});
		base.Visible = false;
		base.DrawOrder = 101;
	}

	public override void Initialize()
	{
		base.Initialize();
		base.LevelManager.LevelChanged += ReInitialize;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			ReInitialize();
			base.PlayerManager.Action = ActionType.Idle;
		}
		return true;
	}

	private void ReInitialize()
	{
		if (!base.PlayerManager.Action.IsIdle() || !diedByLava)
		{
			diedByLava = false;
			base.Visible = false;
			respawned = false;
			sinceStarted = TimeSpan.FromSeconds(-1.0);
			base.GameState.SkipFadeOut = false;
		}
	}

	public override void Draw(GameTime gameTime)
	{
		TimeSpan elapsedGameTime = gameTime.ElapsedGameTime;
		if (base.CameraManager.ActionRunning)
		{
			sinceStarted += elapsedGameTime;
		}
		if (sinceStarted >= FadeTime && !respawned)
		{
			base.PlayerManager.RespawnAtCheckpoint();
			if (!base.GameState.SkipFadeOut)
			{
				base.CameraManager.Constrained = false;
			}
			respawned = true;
		}
		if (!base.GameState.SkipFadeOut)
		{
			GraphicsDevice graphicsDevice = base.GraphicsDevice;
			graphicsDevice.PrepareStencilRead(CompareFunction.Always, StencilMask.None);
			fadePlane.Material.Opacity = 1f - Math.Abs(1f - (float)sinceStarted.Ticks / (float)FadeTime.Ticks);
			fadePlane.Draw();
			graphicsDevice.PrepareStencilWrite(StencilMask.None);
		}
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.WakingUp;
	}
}
