using System;
using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class GameLightingPostProcess : LightingPostProcess
{
	private bool hasTested;

	protected override bool SkipLighting
	{
		get
		{
			if (!SettingsManager.Settings.Lighting)
			{
				if (base.LevelManager.Sky != null)
				{
					return !(base.LevelManager.Sky.Name == "SEWER");
				}
				return true;
			}
			return false;
		}
	}

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	public GameLightingPostProcess(Game game)
		: base(game)
	{
	}

	protected override void DrawLightOccluders(GameTime gameTime)
	{
		if (!PlayerManager.Hidden && !GameState.InFpsMode)
		{
			PlayerManager.MeshHost.InterpolatePosition(gameTime);
			PlayerManager.MeshHost.PlayerMesh.Draw();
		}
	}

	protected override void DoSetup()
	{
		if (!PlayerManager.Hidden && !GameState.InFpsMode)
		{
			if (!base.CameraManager.Viewpoint.IsOrthographic() && base.CameraManager.LastViewpoint != 0)
			{
				PlayerManager.MeshHost.PlayerMesh.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, base.CameraManager.LastViewpoint.ToPhi());
			}
			else
			{
				PlayerManager.MeshHost.PlayerMesh.Rotation = base.CameraManager.Rotation;
			}
			if (PlayerManager.LookingDirection == HorizontalDirection.Left)
			{
				PlayerManager.MeshHost.PlayerMesh.Rotation *= FezMath.QuaternionFromPhi((float)Math.PI);
			}
		}
		if (hasTested)
		{
			return;
		}
		try
		{
			base.GraphicsDevice.SetRenderTarget(lightMapsRth.Target);
			base.GraphicsDevice.SetRenderTarget(null);
		}
		catch (InvalidOperationException e)
		{
			Logger.LogError(e);
			using ErrorDialog errorDialog = new ErrorDialog();
			errorDialog.ShowDialog();
			base.Game.Exit();
			return;
		}
		hasTested = true;
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (!GameState.Loading && base.LevelManager.BackgroundPlanes.ContainsKey(-1))
		{
			Vector3 vector = base.CameraManager.Viewpoint.RightVector() * PlayerManager.LookingDirection.Sign();
			Vector3 vector2 = ((PlayerManager.Action == ActionType.PullUpCornerLedge) ? (PlayerManager.Position + PlayerManager.Size * (vector + Vector3.UnitY) * 0.5f * Easing.EaseOut(PlayerManager.Animation.Timing.NormalizedStep, EasingType.Quadratic)) : ((PlayerManager.Action == ActionType.LowerToCornerLedge) ? (PlayerManager.Position + PlayerManager.Size * (-vector + Vector3.UnitY) * 0.5f * (1f - Easing.EaseOut(PlayerManager.Animation.Timing.NormalizedStep, EasingType.Quadratic))) : PlayerManager.Position));
			if (GameState.InFpsMode)
			{
				vector2 += base.CameraManager.InverseView.Forward;
			}
			base.LevelManager.BackgroundPlanes[-1].Position = (base.LevelManager.HaloFiltering ? vector2 : ((vector2 * 16f).Round() / 16f));
		}
	}

	public override void Draw(GameTime gameTime)
	{
		if (!SkipLighting)
		{
			base.Draw(gameTime);
		}
	}
}
