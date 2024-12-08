using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class RopeBridgesHost : GameComponent
{
	private const float Downforce = 0.1f;

	private readonly Dictionary<TrileInstance, BridgeState> ActiveBridgeParts = new Dictionary<TrileInstance, BridgeState>();

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	public RopeBridgesHost(Game game)
		: base(game)
	{
		base.UpdateOrder = -2;
	}

	public override void Initialize()
	{
		LevelManager.LevelChanged += delegate
		{
			ActiveBridgeParts.Clear();
		};
	}

	public override void Update(GameTime gameTime)
	{
		if (PlayerManager.Grounded)
		{
			AddDownforce(PlayerManager.Ground.NearLow, 0.1f, apply: true, propagate: false);
			AddDownforce(PlayerManager.Ground.FarHigh, 0.1f, apply: true, propagate: false);
			AddDownforce(PlayerManager.Ground.NearLow, 0.1f, apply: false, propagate: true);
			AddDownforce(PlayerManager.Ground.FarHigh, 0.1f, apply: false, propagate: true);
		}
		foreach (BridgeState value in ActiveBridgeParts.Values)
		{
			value.Downforce *= 0.8f;
			value.Dirty = false;
		}
		foreach (BridgeState value2 in ActiveBridgeParts.Values)
		{
			Vector3 vector = new Vector3(value2.Instance.Position.X, value2.OriginalPosition.Y - value2.Downforce, value2.Instance.Position.Z);
			value2.Instance.PhysicsState.Velocity = vector - value2.Instance.Position;
			value2.Instance.Position = vector;
			LevelManager.UpdateInstance(value2.Instance);
		}
	}

	private void AddDownforce(TrileInstance instance, float factor, bool apply, bool propagate)
	{
		if (instance != null && instance.TrileId == 286)
		{
			if (!ActiveBridgeParts.TryGetValue(instance, out var value))
			{
				ActiveBridgeParts.Add(instance, value = new BridgeState(instance));
			}
			else if (apply && value.Dirty)
			{
				return;
			}
			Vector3 vector = CameraManager.Viewpoint.SideMask();
			if (apply)
			{
				value.Downforce = MathHelper.Clamp(value.Downforce + factor, 0f, 1f);
				value.Dirty = true;
			}
			if (propagate)
			{
				TrileEmplacement id = new TrileEmplacement(value.OriginalPosition - vector);
				AddDownforce(LevelManager.TrileInstanceAt(ref id), factor / 2f, apply: true, propagate: true);
				id = new TrileEmplacement(value.OriginalPosition + vector);
				AddDownforce(LevelManager.TrileInstanceAt(ref id), factor / 2f, apply: true, propagate: true);
			}
		}
	}
}
