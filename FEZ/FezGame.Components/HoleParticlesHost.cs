using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;

namespace FezGame.Components;

public class HoleParticlesHost : GameComponent
{
	[ServiceDependency]
	public IPhysicsManager PhysicsManager { private get; set; }

	[ServiceDependency]
	public ITrixelParticleSystems ParticleSystems { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	public HoleParticlesHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
		base.Enabled = false;
	}

	private void TryInitialize()
	{
		if (LevelManager.Name != null && LevelManager.Name.StartsWith("HOLE"))
		{
			GameState.SkipFadeOut = true;
			PlayerManager.CanControl = false;
			CameraManager.Constrained = true;
			CameraManager.PixelsPerTrixel = 3f;
			CameraManager.Center = new Vector3(13f, 20f, 27.875f);
			PlayerManager.Position = new Vector3(13.5f, 15.47f, 28.5f);
			PlayerManager.Ground = default(MultipleHits<TrileInstance>);
			PlayerManager.Velocity = 0.007875001f * -Vector3.UnitY;
			PhysicsManager.Update(PlayerManager);
			PlayerManager.Velocity = 0.007875001f * -Vector3.UnitY;
			PlayerManager.RecordRespawnInformation(markCheckpoint: true);
			PlayerManager.Position = new Vector3(13.5f, 50f, 28.5f);
			PlayerManager.Ground = default(MultipleHits<TrileInstance>);
			PlayerManager.Action = ActionType.FreeFalling;
			for (int i = 0; i < 4; i++)
			{
				TrileInstance trileInstance = new TrileInstance(new Vector3(13.5f, 30 + i * 5, 27 - i), 354);
				TrixelParticleSystem trixelParticleSystem = new TrixelParticleSystem(base.Game, new TrixelParticleSystem.Settings
				{
					ExplodingInstance = trileInstance,
					EnergySource = trileInstance.Center,
					ParticleCount = 25,
					MinimumSize = 1,
					MaximumSize = 6,
					GravityModifier = 1.5f,
					Darken = true,
					Energy = (float)(i + 2) / 4f
				});
				ParticleSystems.Add(trixelParticleSystem);
				trixelParticleSystem.Initialize();
			}
			base.Enabled = true;
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (PlayerManager.Action.IsIdle())
		{
			PlayerManager.CanControl = true;
			ServiceHelper.RemoveComponent(this);
		}
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		LevelManager.LevelChanged -= TryInitialize;
	}
}
