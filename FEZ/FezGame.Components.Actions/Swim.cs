using System;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

internal class Swim : PlayerAction
{
	private const float PulseDelay = 0.5f;

	private const float Buoyancy = 0.006f;

	private const float MaxSubmergedPortion = 0.5f;

	private TimeSpan sincePulsed;

	private SoundEmitter treadInstance;

	private SoundEffect swimSound;

	private readonly MovementHelper movementHelper = new MovementHelper(4.7f, 4.7f, 0f);

	private float SubmergedPortion
	{
		get
		{
			if (!base.LevelManager.WaterType.IsWater())
			{
				return 0.25f;
			}
			return 0.5f;
		}
	}

	[ServiceDependency]
	public IPlaneParticleSystems PlaneParticleSystems { get; set; }

	public Swim(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		swimSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/Swim");
	}

	public override void Initialize()
	{
		base.Initialize();
		movementHelper.Entity = base.PlayerManager;
		base.LevelManager.LevelChanged += delegate
		{
			if (base.LevelManager.WaterType != 0)
			{
				treadInstance = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/Tread").Emit(loop: true, paused: true);
			}
		};
	}

	protected override void TestConditions()
	{
		if (base.PlayerManager.Action != ActionType.Swimming && base.PlayerManager.Action != ActionType.Dying && base.PlayerManager.Action != ActionType.Sinking && base.PlayerManager.Action != ActionType.Treading && base.PlayerManager.Action != ActionType.HurtSwim && base.PlayerManager.Action != ActionType.SuckedIn && (!base.PlayerManager.Grounded || !base.LevelManager.PickupGroups.TryGetValue(base.PlayerManager.Ground.First, out var value) || value.ActorType != ActorType.Geyser) && base.LevelManager.WaterType != 0 && base.PlayerManager.Position.Y < base.LevelManager.WaterHeight - SubmergedPortion && base.PlayerManager.Action != ActionType.Jumping)
		{
			base.PlayerManager.RecordRespawnInformation();
			ActionType action = base.PlayerManager.Action;
			base.PlayerManager.Action = ActionType.Treading;
			if (action != ActionType.Flying)
			{
				PlaneParticleSystems.Splash(base.PlayerManager, outwards: false);
			}
			base.PlayerManager.Velocity *= new Vector3(1f, 0.5f, 1f);
		}
	}

	protected override void Begin()
	{
		base.PlayerManager.CarriedInstance = null;
	}

	protected override void End()
	{
		sincePulsed = TimeSpan.Zero;
		if (base.PlayerManager.Action != ActionType.Suffering && base.PlayerManager.Action != ActionType.Sinking && base.LevelManager.WaterType != 0 && base.PlayerManager.Action != ActionType.Flying)
		{
			if (base.PlayerManager.Action != ActionType.Jumping)
			{
				base.PlayerManager.Velocity *= new Vector3(1f, 0.5f, 1f);
			}
			PlaneParticleSystems.Splash(base.PlayerManager, outwards: true);
		}
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.PlayerManager.Position.Y < base.LevelManager.WaterHeight - SubmergedPortion)
		{
			if (base.LevelManager.WaterType == LiquidType.Lava || base.LevelManager.WaterType == LiquidType.Sewer)
			{
				base.PlayerManager.Action = ActionType.Sinking;
				return false;
			}
			float num = base.LevelManager.WaterHeight - SubmergedPortion;
			float num2 = num - base.PlayerManager.Position.Y;
			base.PlayerManager.Velocity += 0.47250003f * (float)elapsed.TotalSeconds * Vector3.UnitY;
			if (num2 > 0.025f)
			{
				base.PlayerManager.Velocity += num2 * 0.006f * Vector3.UnitY;
			}
			else
			{
				base.PlayerManager.Position = base.PlayerManager.Position * FezMath.XZMask + Vector3.UnitY * num;
			}
		}
		else if (Math.Abs(base.PlayerManager.Velocity.Y) > 0.02f || base.PlayerManager.Grounded)
		{
			base.PlayerManager.Action = ActionType.Falling;
			return true;
		}
		sincePulsed -= elapsed;
		if (base.InputManager.Movement.X == 0f)
		{
			if (base.PlayerManager.Action != ActionType.HurtSwim)
			{
				base.PlayerManager.Action = ActionType.Treading;
			}
		}
		else
		{
			if (base.PlayerManager.Action != ActionType.HurtSwim)
			{
				base.PlayerManager.Action = ActionType.Swimming;
			}
			if (sincePulsed.TotalSeconds <= 0.0)
			{
				PlaneParticleSystems.Splash(base.PlayerManager, outwards: true);
				sincePulsed = TimeSpan.FromSeconds(0.5);
				swimSound.EmitAt(base.PlayerManager.Position);
			}
			float num3 = Easing.EaseIn(sincePulsed.TotalSeconds, EasingType.Sine);
			movementHelper.Update((float)elapsed.TotalSeconds * num3);
			TrileInstance destination = base.PlayerManager.WallCollision.First.Destination;
			if (destination != null && destination.Trile.ActorSettings.Type.IsPickable())
			{
				base.PlayerManager.Velocity *= new Vector3(0.9f, 1f, 0.9f);
				base.DebuggingBag.Add("##. player vel", base.PlayerManager.Velocity);
				destination.PhysicsState.Velocity += base.PlayerManager.Velocity * FezMath.XZMask;
			}
			base.PlayerManager.GroundedVelocity = base.PlayerManager.Velocity;
		}
		if (base.PlayerManager.Action == ActionType.Treading && treadInstance != null && !treadInstance.Dead)
		{
			if (treadInstance.Cue.State != 0)
			{
				treadInstance.Cue.Resume();
			}
			treadInstance.Position = base.PlayerManager.Position;
		}
		return true;
	}

	public override void Update(GameTime gameTime)
	{
		base.Update(gameTime);
		if (!base.GameState.Loading && base.PlayerManager.Action != ActionType.Treading && treadInstance != null && !treadInstance.Dead && treadInstance.Cue.State == SoundState.Playing)
		{
			treadInstance.Cue.Pause();
		}
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type.IsSwimming();
	}
}
