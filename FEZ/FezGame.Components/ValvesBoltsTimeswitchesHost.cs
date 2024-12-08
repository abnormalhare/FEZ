using System;
using System.Collections.Generic;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

public class ValvesBoltsTimeswitchesHost : DrawableGameComponent
{
	private class ValveState
	{
		private const float SpinTime = 0.75f;

		private readonly ValvesBoltsTimeswitchesHost Host;

		public readonly ArtObjectInstance TimeswitchScrewAo;

		private SpinAction State;

		private TimeSpan SinceChanged;

		private int SpinSign;

		private Vector3 OriginalPlayerPosition;

		private Vector3 OriginalAoPosition;

		private Quaternion OriginalAoRotation;

		private Quaternion OriginalScrewRotation;

		private Vector3[] OriginalGroupTrilePositions;

		private float ScrewHeight;

		private float RewindSpeed;

		private bool MovingToHeight;

		private readonly SoundEmitter eTimeswitchWindBack;

		private readonly bool IsBolt;

		private readonly bool IsTimeswitch;

		private readonly TrileGroup AttachedGroup;

		private readonly Vector3 CenterOffset;

		public readonly ArtObjectInstance ArtObject;

		[ServiceDependency]
		public ISoundManager SoundManager { private get; set; }

		[ServiceDependency]
		public IPhysicsManager PhysicsManager { private get; set; }

		[ServiceDependency]
		public ILevelManager LevelManager { private get; set; }

		[ServiceDependency]
		public IInputManager InputManager { private get; set; }

		[ServiceDependency]
		public IGameCameraManager CameraManager { private get; set; }

		[ServiceDependency]
		public IPlayerManager PlayerManager { private get; set; }

		[ServiceDependency]
		public IGameStateManager GameState { private get; set; }

		[ServiceDependency]
		public IValveService ValveService { private get; set; }

		[ServiceDependency]
		public ITimeswitchService TimeswitchService { private get; set; }

		[ServiceDependency]
		public IDebuggingBag DebuggingBag { private get; set; }

		public ValveState(ValvesBoltsTimeswitchesHost host, ArtObjectInstance ao)
		{
			ServiceHelper.InjectServices(this);
			Host = host;
			ArtObject = ao;
			IsBolt = ArtObject.ArtObject.ActorType == ActorType.BoltHandle;
			IsTimeswitch = ArtObject.ArtObject.ActorType == ActorType.Timeswitch;
			BoundingBox boundingBox = new BoundingBox(ArtObject.Position - ArtObject.ArtObject.Size / 2f, ArtObject.Position + ArtObject.ArtObject.Size / 2f);
			if (ArtObject.ActorSettings.AttachedGroup.HasValue)
			{
				AttachedGroup = LevelManager.Groups[ArtObject.ActorSettings.AttachedGroup.Value];
			}
			if (IsTimeswitch)
			{
				eTimeswitchWindBack = Host.TimeswitchWindBackSound.EmitAt(ao.Position, loop: true, paused: true);
				foreach (ArtObjectInstance value2 in LevelManager.ArtObjects.Values)
				{
					if (value2 != ao && value2.ArtObject.ActorType == ActorType.TimeswitchMovingPart)
					{
						BoundingBox box = new BoundingBox(value2.Position - value2.ArtObject.Size / 2f, value2.Position + value2.ArtObject.Size / 2f);
						if (boundingBox.Intersects(box))
						{
							TimeswitchScrewAo = value2;
							break;
						}
					}
				}
			}
			if (!IsBolt && !IsTimeswitch && GameState.SaveData.ThisLevel.PivotRotations.TryGetValue(ArtObject.Id, out var value) && value != 0)
			{
				int num = Math.Abs(value);
				int num2 = Math.Sign(value);
				for (int i = 0; i < num; i++)
				{
					float angle = (float)Math.PI / 2f * (float)num2;
					Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
					ArtObject.Rotation *= quaternion;
				}
			}
			if (IsBolt)
			{
				foreach (TrileInstance trile in AttachedGroup.Triles)
				{
					trile.PhysicsState = new InstancePhysicsState(trile);
				}
			}
			foreach (Volume value3 in LevelManager.Volumes.Values)
			{
				Vector3 vector = (value3.To - value3.From).Abs();
				if (vector.X == 3f && vector.Z == 3f && vector.Y == 1f && boundingBox.Contains(value3.BoundingBox) == ContainmentType.Contains)
				{
					CenterOffset = (value3.From + value3.To) / 2f - ArtObject.Position;
					break;
				}
			}
		}

		public bool Update(TimeSpan elapsed)
		{
			if (MovingToHeight)
			{
				return false;
			}
			SinceChanged += elapsed;
			Vector3 vector = CameraManager.Viewpoint.ScreenSpaceMask();
			switch (State)
			{
			case SpinAction.Idle:
			{
				Vector3 vector5 = (PlayerManager.Position - (ArtObject.Position - new Vector3(0f, 1f, 0f) + CenterOffset)) * vector;
				vector5.X += vector5.Z;
				Vector3 vector6 = vector5.Abs();
				bool flag = ((!IsBolt && !IsTimeswitch) ? (vector6.X < 1f && vector6.Y < 1f) : (vector6.X > 0.75f && vector6.X < 1.75f && vector6.Y < 1f));
				if (LevelManager.Flat)
				{
					flag = vector6.X < 1.5f && vector6.Y < 1f;
				}
				if (flag && PlayerManager.CarriedInstance == null && PlayerManager.Grounded && PlayerManager.Action != ActionType.GrabTombstone && InputManager.FpsToggle != FezButtonState.Pressed && InputManager.GrabThrow == FezButtonState.Pressed && PlayerManager.Action != ActionType.ReadingSign && PlayerManager.Action != ActionType.Dying && PlayerManager.Action != ActionType.FreeFalling)
				{
					Vector3 vector7 = CameraManager.Viewpoint.ForwardVector();
					Vector3 vector8 = CameraManager.Viewpoint.DepthMask();
					Vector3 vector9 = (ArtObject.Position + CenterOffset) * vector8;
					PlayerManager.Position = PlayerManager.Position * vector + vector8 * vector9 - vector7 * 1.5f;
					SinceChanged = TimeSpan.Zero;
					return true;
				}
				if (!IsTimeswitch || !(ScrewHeight >= 0f) || !(ScrewHeight <= 2f))
				{
					break;
				}
				float num4 = ((ArtObject.ActorSettings.TimeswitchWindBackSpeed == 0f) ? 4f : ArtObject.ActorSettings.TimeswitchWindBackSpeed);
				float num5 = (float)elapsed.TotalSeconds / (num4 - 0.25f) * 2f;
				RewindSpeed = ((SinceChanged.TotalSeconds < 0.5) ? MathHelper.Lerp(0f, num5, (float)SinceChanged.TotalSeconds * 2f) : num5);
				float screwHeight = ScrewHeight;
				ScrewHeight = MathHelper.Clamp(ScrewHeight - RewindSpeed, 0f, 2f);
				float num6 = screwHeight - ScrewHeight;
				if (ScrewHeight == 0f && num6 != 0f)
				{
					Host.TimeswitchEndWindBackSound.EmitAt(ArtObject.Position);
					TimeswitchService.OnHitBase(ArtObject.Id);
					if (eTimeswitchWindBack != null && !eTimeswitchWindBack.Dead && eTimeswitchWindBack.Cue.State == SoundState.Playing)
					{
						eTimeswitchWindBack.Cue.Pause();
					}
				}
				else if (num6 != 0f)
				{
					if (eTimeswitchWindBack != null && !eTimeswitchWindBack.Dead && eTimeswitchWindBack.Cue.State == SoundState.Paused)
					{
						eTimeswitchWindBack.Cue.Resume();
					}
					eTimeswitchWindBack.VolumeFactor = FezMath.Saturate(num6 * 20f * ArtObject.ActorSettings.TimeswitchWindBackSpeed);
				}
				else
				{
					eTimeswitchWindBack.VolumeFactor = 0f;
					if (eTimeswitchWindBack != null && !eTimeswitchWindBack.Dead && eTimeswitchWindBack.Cue.State == SoundState.Playing)
					{
						eTimeswitchWindBack.Cue.Pause();
					}
				}
				TimeswitchScrewAo.Position -= Vector3.UnitY * num6;
				TimeswitchScrewAo.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitY, num6 * ((float)Math.PI / 2f) * -4f);
				break;
			}
			case SpinAction.Grabbed:
				RewindSpeed = 0f;
				if (PlayerManager.Action != ActionType.GrabTombstone)
				{
					State = SpinAction.Idle;
				}
				if (IsTimeswitch)
				{
					SinceChanged = TimeSpan.Zero;
					eTimeswitchWindBack.VolumeFactor = 0f;
					if (eTimeswitchWindBack != null && !eTimeswitchWindBack.Dead && eTimeswitchWindBack.Cue.State == SoundState.Playing)
					{
						eTimeswitchWindBack.Cue.Pause();
					}
				}
				break;
			case SpinAction.Spinning:
			{
				float num = (float)FezMath.Saturate(SinceChanged.TotalSeconds / 0.75);
				float angle = num * ((float)Math.PI / 2f) * (float)SpinSign;
				Quaternion quaternion = Quaternion.CreateFromAxisAngle(Vector3.UnitY, angle);
				ArtObject.Rotation = OriginalAoRotation * quaternion;
				PlayerManager.Position = Vector3.Transform(OriginalPlayerPosition - ArtObject.Position, quaternion) + ArtObject.Position;
				if (IsBolt)
				{
					Vector3 vector2 = num * (float)((SpinSign == 1) ? 1 : (-1)) * Vector3.Up;
					ArtObject.Position = OriginalAoPosition + vector2;
					int num2 = 0;
					foreach (TrileInstance trile in AttachedGroup.Triles)
					{
						trile.Position = OriginalGroupTrilePositions[num2++] + vector2;
						LevelManager.UpdateInstance(trile);
					}
					PlayerManager.Position += vector2;
				}
				if (IsTimeswitch)
				{
					float num3 = num;
					if (SpinSign == -1 && ScrewHeight <= 0.5f)
					{
						num3 = Math.Min(ScrewHeight, num / 2f) * 2f;
					}
					else if (SpinSign == 1 && ScrewHeight >= 1.5f)
					{
						num3 = Math.Min(2f - ScrewHeight, num / 2f) * 2f;
					}
					Vector3 vector3 = num3 * (float)((SpinSign == 1) ? 1 : (-1)) * Vector3.Up / 2f;
					TimeswitchScrewAo.Position = OriginalAoPosition + vector3;
					TimeswitchScrewAo.Rotation = OriginalScrewRotation * Quaternion.CreateFromAxisAngle(Vector3.UnitY, num3 * ((float)Math.PI / 2f) * (float)SpinSign * 2f);
				}
				if (SinceChanged.TotalSeconds >= 0.75)
				{
					PlayerManager.Position += 0.5f * Vector3.UnitY;
					PlayerManager.Velocity -= Vector3.UnitY;
					PhysicsManager.Update(PlayerManager);
					CameraManager.Viewpoint.ForwardVector();
					Vector3 vector4 = CameraManager.Viewpoint.DepthMask();
					_ = (ArtObject.Position + CenterOffset) * vector4;
					ScrewHeight = MathHelper.Clamp(ScrewHeight + (float)SpinSign / 2f, 0f, 2f);
					if (ScrewHeight == 0f && SpinSign == -1)
					{
						TimeswitchService.OnHitBase(ArtObject.Id);
					}
					PlayerManager.Action = ActionType.GrabTombstone;
					SinceChanged -= TimeSpan.FromSeconds(0.75);
					State = SpinAction.Grabbed;
				}
				break;
			}
			}
			return false;
		}

		public void MoveToHeight()
		{
			if (MovingToHeight)
			{
				return;
			}
			MovingToHeight = true;
			float value = ArtObject.ActorSettings.ShouldMoveToHeight.Value;
			ArtObject.ActorSettings.ShouldMoveToHeight = null;
			Vector3 vector = ArtObject.Position + CenterOffset;
			Vector3 movement = (new Vector3(0f, value, 0f) - vector) * Vector3.UnitY + Vector3.UnitY / 2f;
			Vector3 origin = vector;
			Vector3 destination = vector + movement;
			float lastHeight = origin.Y;
			if (PlayerManager.Action == ActionType.PivotTombstone || (PlayerManager.Grounded && AttachedGroup.Triles.Contains(PlayerManager.Ground.First)))
			{
				MovingToHeight = false;
				return;
			}
			if (Math.Abs(movement.Y) < 1f)
			{
				MovingToHeight = false;
				return;
			}
			Waiters.Interpolate(Math.Abs(movement.Y / 2f), delegate(float step)
			{
				float num = Easing.EaseInOut(step, EasingType.Sine);
				ArtObject.Position = Vector3.Lerp(origin, destination, num);
				ArtObject.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, num * ((float)Math.PI / 2f) * (float)(int)Math.Round(movement.Y / 2f));
				foreach (TrileInstance trile in AttachedGroup.Triles)
				{
					trile.Position += Vector3.UnitY * (ArtObject.Position.Y - lastHeight);
					trile.PhysicsState.Velocity = Vector3.UnitY * (ArtObject.Position.Y - lastHeight);
					LevelManager.UpdateInstance(trile);
				}
				lastHeight = ArtObject.Position.Y;
			}, delegate
			{
				ArtObject.Position = destination;
				ArtObject.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, (float)Math.PI / 2f * (float)(int)Math.Round(movement.Y / 2f));
				foreach (TrileInstance trile2 in AttachedGroup.Triles)
				{
					trile2.PhysicsState.Velocity = Vector3.Zero;
				}
				MovingToHeight = false;
			}).AutoPause = true;
		}

		public void MoveToEnd()
		{
			ArtObject.ActorSettings.ShouldMoveToEnd = false;
			Vector3 vector = new Vector3(2f, 100f, 2f);
			Vector3 vector2 = ArtObject.Position + CenterOffset;
			BoundingBox box = new BoundingBox(vector2 - vector, vector2 + vector);
			foreach (ArtObjectInstance value in LevelManager.ArtObjects.Values)
			{
				if (value.ArtObject.ActorType != ActorType.BoltNutTop)
				{
					continue;
				}
				Vector3 vector3 = value.Position + Vector3.Up * 3.5f;
				vector = value.ArtObject.Size / 2f + Vector3.Up / 32f;
				if (!new BoundingBox(vector3 - vector, vector3 + vector).Intersects(box))
				{
					continue;
				}
				Vector3 vector4 = value.Position - vector2 + Vector3.UnitY / 2f;
				ArtObject.Position += vector4;
				{
					foreach (TrileInstance trile in AttachedGroup.Triles)
					{
						trile.Position += vector4;
						LevelManager.UpdateInstance(trile);
					}
					break;
				}
			}
		}

		public void GrabOnto()
		{
			PlayerManager.Action = ActionType.GrabTombstone;
			Waiters.Wait(0.4, (float _) => PlayerManager.Action != ActionType.GrabTombstone, delegate
			{
				if (PlayerManager.Action == ActionType.GrabTombstone)
				{
					Host.GrabSound.EmitAt(ArtObject.Position);
					State = SpinAction.Grabbed;
				}
			});
		}

		public void TrySpin()
		{
			if (State != SpinAction.Grabbed)
			{
				return;
			}
			if (PlayerManager.Action != ActionType.GrabTombstone)
			{
				State = SpinAction.Idle;
			}
			else
			{
				if (!PlayerManager.Animation.Timing.Ended || CameraManager.Viewpoint == Viewpoint.Perspective || CameraManager.LastViewpoint == CameraManager.Viewpoint)
				{
					return;
				}
				SpinSign = CameraManager.LastViewpoint.GetDistance(CameraManager.Viewpoint);
				if (IsBolt)
				{
					Vector3 vector = new Vector3(2f);
					Vector3 vector2 = ArtObject.Position + CenterOffset;
					BoundingBox box = new BoundingBox(vector2 - vector, vector2 + vector);
					foreach (ArtObjectInstance value2 in LevelManager.ArtObjects.Values)
					{
						if (value2.ArtObject.ActorType == ActorType.BoltNutBottom && SpinSign == -1)
						{
							vector = value2.ArtObject.Size / 2f + Vector3.Up / 32f;
							if (new BoundingBox(value2.Position - vector, value2.Position + vector).Intersects(box))
							{
								CameraManager.CancelViewTransition();
								return;
							}
						}
						else if (value2.ArtObject.ActorType == ActorType.BoltNutTop && SpinSign == 1)
						{
							Vector3 vector3 = value2.Position + Vector3.Up * 3.5f;
							vector = value2.ArtObject.Size / 2f + Vector3.Up / 32f;
							if (new BoundingBox(vector3 - vector, vector3 + vector).Intersects(box))
							{
								CameraManager.CancelViewTransition();
								return;
							}
						}
					}
				}
				if (IsTimeswitch && SpinSign == -1)
				{
					CameraManager.CancelViewTransition();
					return;
				}
				if (IsBolt)
				{
					if (SpinSign == 1)
					{
						Host.BoltScrew.EmitAt(ArtObject.Position);
					}
					else
					{
						Host.BoltUnscrew.EmitAt(ArtObject.Position);
					}
				}
				else if (IsTimeswitch)
				{
					Host.TimeSwitchWind.EmitAt(ArtObject.Position);
				}
				else if (SpinSign == 1)
				{
					Host.ValveScrew.EmitAt(ArtObject.Position);
				}
				else
				{
					Host.ValveUnscrew.EmitAt(ArtObject.Position);
				}
				if (!GameState.SaveData.ThisLevel.PivotRotations.TryGetValue(ArtObject.Id, out var value))
				{
					GameState.SaveData.ThisLevel.PivotRotations.Add(ArtObject.Id, SpinSign);
				}
				else
				{
					GameState.SaveData.ThisLevel.PivotRotations[ArtObject.Id] = value + SpinSign;
				}
				Viewpoint lastViewpoint = CameraManager.LastViewpoint;
				Vector3 vector4 = lastViewpoint.ScreenSpaceMask();
				Vector3 vector5 = lastViewpoint.ForwardVector();
				Vector3 vector6 = lastViewpoint.DepthMask();
				Vector3 vector7 = (ArtObject.Position + CenterOffset) * vector6;
				Vector3 originalPlayerPosition = (PlayerManager.Position = PlayerManager.Position * vector4 + vector6 * vector7 - vector5 * 2f);
				OriginalPlayerPosition = originalPlayerPosition;
				OriginalAoRotation = ArtObject.Rotation;
				OriginalAoPosition = (IsTimeswitch ? TimeswitchScrewAo.Position : ArtObject.Position);
				if (IsTimeswitch)
				{
					OriginalScrewRotation = TimeswitchScrewAo.Rotation;
				}
				if (AttachedGroup != null)
				{
					OriginalGroupTrilePositions = AttachedGroup.Triles.Select((TrileInstance x) => x.Position).ToArray();
				}
				SinceChanged = TimeSpan.Zero;
				State = SpinAction.Spinning;
				PlayerManager.Action = ActionType.PivotTombstone;
				if (IsTimeswitch)
				{
					if (SpinSign == 1 && ScrewHeight <= 0f)
					{
						TimeswitchService.OnScrewedOut(ArtObject.Id);
					}
				}
				else if (SpinSign == -1)
				{
					ValveService.OnUnscrew(ArtObject.Id);
				}
				else
				{
					ValveService.OnScrew(ArtObject.Id);
				}
			}
		}
	}

	private readonly List<ValveState> TrackedValves = new List<ValveState>();

	private SoundEffect GrabSound;

	private SoundEffect ValveUnscrew;

	private SoundEffect ValveScrew;

	private SoundEffect BoltScrew;

	private SoundEffect BoltUnscrew;

	private SoundEffect TimeSwitchWind;

	private SoundEffect TimeswitchWindBackSound;

	private SoundEffect TimeswitchEndWindBackSound;

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public ValvesBoltsTimeswitchesHost(Game game)
		: base(game)
	{
		base.DrawOrder = 6;
	}

	public override void Initialize()
	{
		base.Initialize();
		GrabSound = CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/GrabLever");
		ValveUnscrew = CMProvider.Global.Load<SoundEffect>("Sounds/Sewer/ValveUnscrew");
		ValveScrew = CMProvider.Global.Load<SoundEffect>("Sounds/Sewer/ValveScrew");
		BoltUnscrew = CMProvider.Global.Load<SoundEffect>("Sounds/Industrial/BoltUnscrew");
		BoltScrew = CMProvider.Global.Load<SoundEffect>("Sounds/Industrial/BoltScrew");
		TimeswitchWindBackSound = CMProvider.Global.Load<SoundEffect>("Sounds/Nature/TimeswitchWindBack");
		TimeswitchEndWindBackSound = CMProvider.Global.Load<SoundEffect>("Sounds/Nature/TimeswitchEndWindBack");
		TimeSwitchWind = CMProvider.Global.Load<SoundEffect>("Sounds/Industrial/TimeswitchWindUp");
		CameraManager.PreViewpointChanged += OnViewpointChanged;
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void OnViewpointChanged()
	{
		foreach (ValveState trackedValf in TrackedValves)
		{
			trackedValf.TrySpin();
		}
	}

	private void TryInitialize()
	{
		TrackedValves.Clear();
		foreach (ArtObjectInstance value in LevelManager.ArtObjects.Values)
		{
			if (value.ArtObject.ActorType == ActorType.Valve || value.ArtObject.ActorType == ActorType.BoltHandle || value.ArtObject.ActorType == ActorType.Timeswitch)
			{
				TrackedValves.Add(new ValveState(this, value));
			}
		}
		bool enabled = (base.Visible = TrackedValves.Count > 0);
		base.Enabled = enabled;
	}

	public override void Update(GameTime gameTime)
	{
		if (EngineState.Loading || EngineState.InMap || EngineState.Paused || !CameraManager.Viewpoint.IsOrthographic())
		{
			return;
		}
		float num = float.MaxValue;
		ValveState valveState = null;
		foreach (ValveState trackedValf in TrackedValves)
		{
			if (trackedValf.ArtObject.ActorSettings.ShouldMoveToEnd)
			{
				trackedValf.MoveToEnd();
			}
			if (trackedValf.ArtObject.ActorSettings.ShouldMoveToHeight.HasValue)
			{
				trackedValf.MoveToHeight();
			}
			if (trackedValf.Update(gameTime.ElapsedGameTime))
			{
				float num2 = trackedValf.ArtObject.Position.Dot(CameraManager.Viewpoint.ForwardVector());
				if (num2 < num)
				{
					valveState = trackedValf;
					num = num2;
				}
			}
		}
		valveState?.GrabOnto();
	}
}
