using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Services;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

internal class PushSwitchesHost : GameComponent
{
	private class SwitchState
	{
		private enum SwitchPermanence
		{
			Volatile,
			Sticky,
			Permanent
		}

		private enum SwitchAction
		{
			Up,
			HalfPush,
			HeldAtHalf,
			FullPush,
			HeldDown,
			ComingBack,
			BackToHalf
		}

		private static readonly TimeSpan HalfPushDuration = TimeSpan.FromSeconds(0.15000000596046448);

		private static readonly TimeSpan FullPushDuration = TimeSpan.FromSeconds(0.44999998807907104);

		private static readonly TimeSpan ComeBackDuration = TimeSpan.FromSeconds(0.75);

		private const float HalfPushHeight = 0.1875f;

		private const float FullPushHeight = 0.8125f;

		private readonly SoundEffect ClickSound;

		private readonly SoundEffect ThudSound;

		private readonly SoundEffect ReleaseSound;

		private readonly TrileGroup Group;

		private readonly float OriginalHeight;

		private readonly SwitchPermanence Permanence;

		private SwitchAction action;

		private TimeSpan sinceActionStarted;

		private float lastStep;

		private TrileInstance[] Hits = new TrileInstance[5];

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		[ServiceDependency]
		public ILevelManager LevelManager { private get; set; }

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public ICollisionManager CollisionManager { private get; set; }

		[ServiceDependency]
		public ISwitchService SwitchService { private get; set; }

		public SwitchState(TrileGroup group, SoundEffect clickSound, SoundEffect thudSound, SoundEffect releaseSound)
		{
			foreach (TrileInstance item in group.Triles.Where((TrileInstance x) => x.PhysicsState == null))
			{
				item.PhysicsState = new InstancePhysicsState(item)
				{
					Sticky = true
				};
			}
			ClickSound = clickSound;
			ThudSound = thudSound;
			ReleaseSound = releaseSound;
			Permanence = ((group.ActorType == ActorType.PushSwitchPermanent) ? SwitchPermanence.Permanent : ((group.ActorType == ActorType.PushSwitchSticky) ? SwitchPermanence.Sticky : SwitchPermanence.Volatile));
			Group = group;
			OriginalHeight = group.Triles[0].Position.Y;
			ServiceHelper.InjectServices(this);
			if (Permanence == SwitchPermanence.Permanent && GameState.SaveData.ThisLevel.InactiveGroups.Contains(Group.Id))
			{
				action = SwitchAction.HeldDown;
			}
		}

		public void Update(TimeSpan elapsed)
		{
			bool flag = false;
			bool flag2 = false;
			if (PlayerManager.Grounded && (Group.Triles.Contains(PlayerManager.Ground.NearLow) || Group.Triles.Contains(PlayerManager.Ground.FarHigh)))
			{
				flag = true;
				flag2 = PlayerManager.CarriedInstance != null;
			}
			foreach (TrileInstance trile in Group.Triles)
			{
				Vector3 transformedSize = trile.TransformedSize;
				Vector3 center = trile.Center;
				Vector3 vector = new Vector3(0f, 0.5f, 0f);
				Array.Clear(Hits, 0, 5);
				int num = 0;
				TrileInstance trileInstance = LevelManager.ActualInstanceAt(center + transformedSize * new Vector3(0f, 0.5f, 0f) + vector);
				if (trileInstance != null && Array.IndexOf(Hits, trileInstance) == -1)
				{
					Hits[num++] = trileInstance;
				}
				trileInstance = LevelManager.ActualInstanceAt(center + transformedSize * new Vector3(0.5f, 0.5f, 0f) + vector);
				if (trileInstance != null && Array.IndexOf(Hits, trileInstance) == -1)
				{
					Hits[num++] = trileInstance;
				}
				trileInstance = LevelManager.ActualInstanceAt(center + transformedSize * new Vector3(-0.5f, 0.5f, 0f) + vector);
				if (trileInstance != null && Array.IndexOf(Hits, trileInstance) == -1)
				{
					Hits[num++] = trileInstance;
				}
				trileInstance = LevelManager.ActualInstanceAt(center + transformedSize * new Vector3(0f, 0.5f, 0.5f) + vector);
				if (trileInstance != null && Array.IndexOf(Hits, trileInstance) == -1)
				{
					Hits[num++] = trileInstance;
				}
				trileInstance = LevelManager.ActualInstanceAt(center + transformedSize * new Vector3(0f, 0.5f, -0.5f) + vector);
				if (trileInstance != null && Array.IndexOf(Hits, trileInstance) == -1)
				{
					Hits[num] = trileInstance;
				}
				if (num == 0 && Hits[0] == null)
				{
					continue;
				}
				TrileInstance[] hits = Hits;
				foreach (TrileInstance trileInstance2 in hits)
				{
					if (trileInstance2 != null && trileInstance2.PhysicsState != null && (trileInstance2.PhysicsState.Ground.NearLow == trile || trileInstance2.PhysicsState.Ground.FarHigh == trile))
					{
						if (trileInstance2.Trile.ActorSettings.Type.IsHeavy() || flag)
						{
							flag2 = true;
						}
						flag = true;
					}
				}
			}
			float num2 = 0f;
			SwitchAction switchAction = action;
			switch (action)
			{
			case SwitchAction.Up:
				num2 = 0f;
				if (flag)
				{
					switchAction = SwitchAction.HalfPush;
				}
				break;
			case SwitchAction.HalfPush:
				num2 = (float)sinceActionStarted.Ticks / (float)HalfPushDuration.Ticks * 0.1875f;
				if (!flag)
				{
					ReleaseSound.EmitAt(Group.Triles.First().Center);
					switchAction = SwitchAction.ComingBack;
				}
				if (sinceActionStarted.Ticks >= HalfPushDuration.Ticks)
				{
					switchAction = SwitchAction.HeldAtHalf;
					ClickSound.EmitAt(Group.Triles.First().Center);
				}
				break;
			case SwitchAction.HeldAtHalf:
				num2 = 0.1875f;
				if (!flag)
				{
					ReleaseSound.EmitAt(Group.Triles.First().Center);
					switchAction = SwitchAction.ComingBack;
				}
				if (flag && flag2)
				{
					switchAction = SwitchAction.FullPush;
				}
				break;
			case SwitchAction.FullPush:
			{
				float num3 = (float)sinceActionStarted.Ticks / (float)FullPushDuration.Ticks;
				num2 = 0.1875f + Easing.EaseIn(num3, EasingType.Quadratic) * 0.625f;
				if (!flag && Permanence == SwitchPermanence.Volatile)
				{
					switchAction = SwitchAction.ComingBack;
				}
				if (sinceActionStarted.Ticks >= FullPushDuration.Ticks)
				{
					switchAction = SwitchAction.HeldDown;
					SwitchService.OnPush(Group.Id);
					ThudSound.EmitAt(Group.Triles.First().Center);
					if (Permanence == SwitchPermanence.Permanent)
					{
						GameState.SaveData.ThisLevel.InactiveGroups.Add(Group.Id);
					}
				}
				break;
			}
			case SwitchAction.HeldDown:
				num2 = 0.8125f;
				if (!flag && Permanence == SwitchPermanence.Volatile)
				{
					SwitchService.OnLift(Group.Id);
					switchAction = SwitchAction.ComingBack;
				}
				if (flag && !flag2 && Permanence == SwitchPermanence.Volatile)
				{
					SwitchService.OnLift(Group.Id);
					switchAction = SwitchAction.BackToHalf;
				}
				break;
			case SwitchAction.ComingBack:
				num2 = lastStep - (float)sinceActionStarted.Ticks / ((float)ComeBackDuration.Ticks * lastStep) * lastStep;
				if ((float)sinceActionStarted.Ticks >= (float)ComeBackDuration.Ticks * lastStep)
				{
					switchAction = SwitchAction.Up;
				}
				break;
			case SwitchAction.BackToHalf:
				num2 = lastStep - (float)sinceActionStarted.Ticks / ((float)ComeBackDuration.Ticks * lastStep) * lastStep;
				if ((float)sinceActionStarted.Ticks >= (float)ComeBackDuration.Ticks * lastStep)
				{
					switchAction = SwitchAction.Up;
				}
				break;
			}
			if (switchAction != action)
			{
				action = switchAction;
				if (switchAction == SwitchAction.ComingBack || switchAction == SwitchAction.BackToHalf)
				{
					lastStep = num2;
				}
				sinceActionStarted = TimeSpan.Zero;
			}
			sinceActionStarted += elapsed;
			float y = Group.Triles[0].Position.Y;
			foreach (TrileInstance trile2 in Group.Triles)
			{
				trile2.Position = new Vector3(trile2.Position.X, OriginalHeight - num2, trile2.Position.Z);
			}
			float num4 = Group.Triles[0].Position.Y - y;
			foreach (TrileInstance trile3 in Group.Triles)
			{
				trile3.PhysicsState.Velocity = new Vector3(0f, num4, 0f);
				if (num4 != 0f)
				{
					LevelManager.UpdateInstance(trile3);
				}
			}
		}
	}

	private readonly Dictionary<int, SwitchState> trackedSwitches = new Dictionary<int, SwitchState>();

	private SoundEffect chick;

	private SoundEffect poum;

	private SoundEffect release;

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public PushSwitchesHost(Game game)
		: base(game)
	{
		base.UpdateOrder = -2;
	}

	public override void Initialize()
	{
		LevelManager.LevelChanging += TrackSwitches;
		TrackSwitches();
		chick = CMProvider.Global.Load<SoundEffect>("Sounds/Industrial/SwitchHalfPress");
		poum = CMProvider.Global.Load<SoundEffect>("Sounds/Industrial/SwitchPress");
		release = CMProvider.Global.Load<SoundEffect>("Sounds/Industrial/SwitchHalfRelease");
	}

	private void TrackSwitches()
	{
		trackedSwitches.Clear();
		foreach (TrileGroup item in LevelManager.Groups.Values.Where((TrileGroup x) => x.ActorType.IsPushSwitch()))
		{
			trackedSwitches.Add(item.Id, new SwitchState(item, chick, poum, release));
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Paused || GameState.InMap || !CameraManager.ActionRunning || GameState.Loading)
		{
			return;
		}
		foreach (SwitchState value in trackedSwitches.Values)
		{
			value.Update(gameTime.ElapsedGameTime);
		}
	}
}
