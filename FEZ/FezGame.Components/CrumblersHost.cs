using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

public class CrumblersHost : GameComponent
{
	private class CrumblerState
	{
		public readonly TrileInstance Instance;

		private readonly List<TrileInstance> InstancesToClear = new List<TrileInstance>();

		private readonly Vector3 OriginalCenter;

		private readonly CrumblersHost Host;

		public TrixelParticleSystem System;

		private Vector3 lastJitter;

		private int c;

		public bool Dead { get; private set; }

		[ServiceDependency]
		public ISoundManager SoundManager { get; set; }

		[ServiceDependency]
		public ILevelMaterializer LevelMaterializer { get; set; }

		[ServiceDependency]
		public IPlayerManager PlayerManager { get; set; }

		[ServiceDependency]
		public IGameLevelManager LevelManager { get; set; }

		[ServiceDependency]
		public ITrixelParticleSystems ParticleSystems { get; set; }

		[ServiceDependency]
		public IGameCameraManager CameraManager { get; set; }

		public CrumblerState(TrileInstance instance, CrumblersHost host)
		{
			ServiceHelper.InjectServices(this);
			Host = host;
			Instance = instance;
			OriginalCenter = instance.PhysicsState.Center;
			Waiters.Wait(0.5, StartCrumbling).AutoPause = true;
			Waiters.Wait(2.5, Respawn).AutoPause = true;
			host.sWarning.EmitAt(OriginalCenter, RandomHelper.Centered(0.009999999776482582));
		}

		public void Rumble()
		{
			if (c++ != 2)
			{
				Instance.PhysicsState.Velocity = Vector3.Zero;
				if (System != null)
				{
					System.Offset = Vector3.Zero;
				}
				return;
			}
			c = 0;
			Vector3 vector = new Vector3(RandomHelper.Centered(0.03999999910593033), 0f, RandomHelper.Centered(0.03999999910593033));
			Vector3 center = Instance.PhysicsState.Center;
			Instance.PhysicsState.Center += -lastJitter + vector;
			if (System != null)
			{
				System.Offset = -lastJitter + vector;
			}
			Instance.PhysicsState.Velocity = Instance.PhysicsState.Center - center;
			Instance.PhysicsState.UpdateInstance();
			lastJitter = vector;
			LevelManager.UpdateInstance(Instance);
		}

		private void StartCrumbling()
		{
			Host.sCrumble.EmitAt(OriginalCenter, RandomHelper.Centered(0.009999999776482582));
			Vector3 vector = CameraManager.Viewpoint.SideMask();
			Vector3 vector2 = CameraManager.Viewpoint.ForwardVector();
			bool flag = vector.X != 0f;
			bool flag2 = flag;
			int num = (flag2 ? ((int)vector2.Z) : ((int)vector2.X));
			Point key = new Point(flag ? Instance.Emplacement.X : Instance.Emplacement.Z, Instance.Emplacement.Y);
			LevelManager.WaitForScreenInvalidation();
			if (LevelManager.ScreenSpaceLimits.TryGetValue(key, out var value))
			{
				value.End += num;
				TrileEmplacement id = new TrileEmplacement(flag ? key.X : value.Start, key.Y, flag2 ? value.Start : key.X);
				while ((flag2 ? id.Z : id.X) != value.End)
				{
					TrileInstance trileInstance = LevelManager.TrileInstanceAt(ref id);
					if (trileInstance != null && !trileInstance.Hidden && trileInstance.Trile.ActorSettings.Type == ActorType.Crumbler)
					{
						trileInstance.Hidden = true;
						LevelMaterializer.CullInstanceOut(trileInstance);
						InstancesToClear.Add(trileInstance);
						ParticleSystems.Add(System = new TrixelParticleSystem(ServiceHelper.Game, new TrixelParticleSystem.Settings
						{
							BaseVelocity = Vector3.Zero,
							Energy = 0.1f,
							ParticleCount = (int)(20f / (float)InstancesToClear.Count),
							GravityModifier = 0.6f,
							Crumble = true,
							ExplodingInstance = trileInstance
						}));
					}
					if (flag2)
					{
						id.Z += num;
					}
					else
					{
						id.X += num;
					}
				}
			}
			Waiters.Wait(1.0, ClearTriles).AutoPause = true;
		}

		private void ClearTriles()
		{
			Instance.PhysicsState.Center = OriginalCenter;
			Instance.PhysicsState.Velocity = Vector3.Zero;
			Instance.PhysicsState.UpdateInstance();
			LevelManager.UpdateInstance(Instance);
			lastJitter = Vector3.Zero;
			foreach (TrileInstance item in InstancesToClear)
			{
				if (PlayerManager.Action.IsOnLedge() && PlayerManager.HeldInstance == item)
				{
					PlayerManager.HeldInstance = null;
					PlayerManager.Action = ActionType.Falling;
				}
				LevelManager.ClearTrile(item, skipRecull: true);
			}
			ParticleSystems.UnGroundAll();
			Dead = true;
		}

		private void Respawn()
		{
			foreach (TrileInstance item in InstancesToClear)
			{
				ServiceHelper.AddComponent(new GlitchyRespawner(ServiceHelper.Game, item));
			}
		}
	}

	private readonly List<CrumblerState> States = new List<CrumblerState>();

	private bool AnyCrumblers;

	private SoundEffect sCrumble;

	private SoundEffect sWarning;

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IGamepadsManager Gamepads { get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public CrumblersHost(Game game)
		: base(game)
	{
		base.UpdateOrder = -2;
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
		sCrumble = CMProvider.Global.Load<SoundEffect>("Sounds/Nature/CrumblerCrumble");
		sWarning = CMProvider.Global.Load<SoundEffect>("Sounds/Nature/CrumblerWarning");
	}

	private void TryInitialize()
	{
		States.Clear();
		AnyCrumblers = false;
		foreach (TrileInstance item in LevelManager.Triles.Values.Where((TrileInstance x) => x.Trile.ActorSettings.Type == ActorType.Crumbler))
		{
			AnyCrumblers = true;
			item.PhysicsState = new InstancePhysicsState(item)
			{
				Sticky = true
			};
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused || GameState.InMap || !CameraManager.ActionRunning || !CameraManager.Viewpoint.IsOrthographic() || !AnyCrumblers)
		{
			return;
		}
		TestForCrumblers();
		for (int num = States.Count - 1; num >= 0; num--)
		{
			if (States[num].Dead)
			{
				States.RemoveAt(num);
			}
			else
			{
				States[num].Rumble();
			}
		}
	}

	private void TestForCrumblers()
	{
		TrileInstance trileInstance = null;
		bool flag = false;
		if (PlayerManager.Grounded)
		{
			TrileInstance nl = PlayerManager.Ground.NearLow;
			TrileInstance fh = PlayerManager.Ground.FarHigh;
			if (nl != null && (flag |= nl.Trile.ActorSettings.Type == ActorType.Crumbler) && !States.Any((CrumblerState x) => x.Instance == nl))
			{
				trileInstance = nl;
			}
			else if (fh != null && (flag |= fh.Trile.ActorSettings.Type == ActorType.Crumbler) && !States.Any((CrumblerState x) => x.Instance == fh))
			{
				trileInstance = fh;
			}
		}
		else if (PlayerManager.Action.IsOnLedge())
		{
			TrileInstance hi = PlayerManager.HeldInstance;
			if (hi != null && (flag |= hi.Trile.ActorSettings.Type == ActorType.Crumbler) && !States.Any((CrumblerState x) => x.Instance == hi))
			{
				trileInstance = hi;
			}
		}
		if (trileInstance != null)
		{
			States.Add(new CrumblerState(trileInstance, this));
		}
		if (flag && !SettingsManager.Settings.DisableController)
		{
			Gamepads[GameState.ActivePlayer].Vibrate(VibrationMotor.LeftLow, 0.25, TimeSpan.FromSeconds(0.10000000149011612));
			Gamepads[GameState.ActivePlayer].Vibrate(VibrationMotor.RightHigh, 0.25, TimeSpan.FromSeconds(0.10000000149011612));
		}
	}
}
