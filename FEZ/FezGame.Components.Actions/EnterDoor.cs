using System;
using FezEngine;
using FezEngine.Components;
using FezEngine.Effects;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components.Actions;

public class EnterDoor : PlayerAction
{
	private Mesh fadeOutQuad;

	private Mesh trileFadeQuad;

	private bool hasFlipped;

	private bool hasChangedLevel;

	private string newLevel;

	private float step = -1f;

	private Vector3 spinOrigin;

	private Vector3 spinDestination;

	private SoundEffect sound;

	private bool skipFade;

	private TimeSpan transitionTime;

	private bool skipPreview;

	protected override bool ViewTransitionIndependent => true;

	[ServiceDependency]
	public IThreadPool ThreadPool { private get; set; }

	[ServiceDependency]
	public IDotManager DotManager { private get; set; }

	[ServiceDependency]
	public IGameService GameService { private get; set; }

	[ServiceDependency]
	public IVolumeService VolumeService { private get; set; }

	public EnterDoor(Game game)
		: base(game)
	{
	}

	protected override void LoadContent()
	{
		fadeOutQuad = new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false
		};
		fadeOutQuad.AddFace(Vector3.One * 2f, Vector3.Zero, FaceOrientation.Front, Color.Black, centeredOnOrigin: true);
		trileFadeQuad = new Mesh
		{
			AlwaysOnTop = true,
			DepthWrites = false
		};
		trileFadeQuad.AddFace(Vector3.One, Vector3.Zero, FaceOrientation.Front, Color.Black, centeredOnOrigin: true);
		DrawActionScheduler.Schedule(delegate
		{
			fadeOutQuad.Effect = new DefaultEffect.VertexColored
			{
				ForcedViewMatrix = Matrix.Identity,
				ForcedProjectionMatrix = Matrix.Identity
			};
			trileFadeQuad.Effect = new DefaultEffect.VertexColored();
		});
		sound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/EnterDoor");
		base.LevelManager.LevelChanged += delegate
		{
			skipPreview = true;
			VolumeService.Exit += RevertSkipPreview;
		};
		base.DrawOrder = 901;
	}

	private void RevertSkipPreview(int id)
	{
		if (base.PlayerManager.CanControl)
		{
			skipPreview = false;
			VolumeService.Exit -= RevertSkipPreview;
		}
	}

	protected override void TestConditions()
	{
		switch (base.PlayerManager.Action)
		{
		case ActionType.Idle:
		case ActionType.LookingLeft:
		case ActionType.LookingRight:
		case ActionType.LookingUp:
		case ActionType.LookingDown:
		case ActionType.Walking:
		case ActionType.Running:
		case ActionType.Dropping:
		case ActionType.Sliding:
		case ActionType.Landing:
		case ActionType.Teetering:
		case ActionType.IdlePlay:
		case ActionType.IdleSleep:
		case ActionType.IdleLookAround:
		case ActionType.IdleYawn:
		{
			string text = base.PlayerManager.NextLevel;
			if (base.PlayerManager.NextLevel == "CABIN_INTERIOR_A")
			{
				text = "CABIN_INTERIOR_B";
			}
			if (base.InputManager.RotateLeft == FezButtonState.Down || base.InputManager.RotateRight == FezButtonState.Down)
			{
				UnDotize();
				break;
			}
			if (base.PlayerManager.NextLevel == "SKULL_B" && ServiceHelper.Get<ITombstoneService>().get_AlignedCount() < 4)
			{
				UnDotize();
				break;
			}
			if (base.PlayerManager.NextLevel == "ZU_HEADS" && !base.GameState.SaveData.World.ContainsKey("ZU_HEADS"))
			{
				ISuckBlockService suckBlockService = ServiceHelper.Get<ISuckBlockService>();
				bool flag = false;
				for (int i = 2; i < 6; i++)
				{
					if (!suckBlockService.get_IsSucked(i))
					{
						flag = true;
					}
				}
				if (flag)
				{
					UnDotize();
					break;
				}
			}
			if (base.PlayerManager.DoorVolume.HasValue && base.PlayerManager.Grounded && !base.PlayerManager.HideFez && base.PlayerManager.CanControl && !base.PlayerManager.Background && !DotManager.PreventPoI && base.GameState.SaveData.World.ContainsKey(text) && !skipPreview && text != base.LevelManager.Name && base.LevelManager.Name != "CRYPT" && base.LevelManager.Name != "PYRAMID")
			{
				if (MemoryContentManager.AssetExists("Other Textures\\map_screens\\" + text.Replace('/', '\\')))
				{
					Texture2D destinationVignette = base.CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/map_screens/" + text);
					DotManager.Behaviour = DotHost.BehaviourType.ThoughtBubble;
					DotManager.DestinationVignette = destinationVignette;
					if (text == "SEWER_QR" || text == "ZU_HOUSE_QR")
					{
						DotManager.DestinationVignetteSony = base.CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/map_screens/" + text + "_SONY");
					}
					DotManager.ComeOut();
					if (DotManager.Owner != this)
					{
						DotManager.Hey();
					}
					DotManager.Owner = this;
				}
				else
				{
					UnDotize();
				}
			}
			else
			{
				UnDotize();
			}
			if (step != -1f || (base.InputManager.ExactUp != FezButtonState.Pressed && base.PlayerManager.LastAction != ActionType.OpeningDoor) || !base.PlayerManager.Grounded || !base.PlayerManager.DoorVolume.HasValue || base.PlayerManager.Background)
			{
				break;
			}
			UnDotize();
			base.GameState.SkipLoadScreen = (skipFade = base.LevelManager.DestinationVolumeId.HasValue && base.PlayerManager.NextLevel == base.LevelManager.Name);
			bool spinThroughDoor = base.PlayerManager.SpinThroughDoor;
			if (spinThroughDoor)
			{
				Vector3 vector = base.CameraManager.Viewpoint.ForwardVector();
				Vector3 vector2 = base.CameraManager.Viewpoint.DepthMask();
				Volume volume = base.LevelManager.Volumes[base.PlayerManager.DoorVolume.Value];
				Vector3 vector3 = (volume.From + volume.To) / 2f;
				Vector3 vector4 = (volume.To - volume.From) / 2f;
				Vector3 vector5 = vector3 - vector4 * vector - vector;
				if (base.PlayerManager.Position.Dot(vector) < vector5.Dot(vector))
				{
					base.PlayerManager.Position = base.PlayerManager.Position * (Vector3.One - vector2) + vector5 * vector2;
				}
				spinOrigin = GetDestination();
				spinDestination = GetDestination() + vector * 1.5f;
			}
			if (base.PlayerManager.CarriedInstance != null)
			{
				bool flag2 = base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsLight();
				base.PlayerManager.Position = GetDestination();
				base.PlayerManager.Action = ((!flag2) ? (spinThroughDoor ? ActionType.EnterDoorSpinCarryHeavy : ActionType.CarryHeavyEnter) : (spinThroughDoor ? ActionType.EnterDoorSpinCarry : ActionType.CarryEnter));
			}
			else
			{
				base.WalkTo.Destination = GetDestination;
				base.PlayerManager.Action = ActionType.WalkingTo;
				base.WalkTo.NextAction = (spinThroughDoor ? ActionType.EnterDoorSpin : ActionType.EnteringDoor);
			}
			break;
		}
		default:
			UnDotize();
			break;
		}
	}

	private void UnDotize()
	{
		if (DotManager.Owner == this)
		{
			DotManager.Behaviour = DotHost.BehaviourType.FollowGomez;
			DotManager.Owner = null;
			DotManager.DestinationVignette = null;
			DotManager.DestinationVignetteSony = null;
			DotManager.Burrow();
		}
	}

	private Vector3 GetDestination()
	{
		if (!base.PlayerManager.DoorVolume.HasValue || !base.LevelManager.Volumes.ContainsKey(base.PlayerManager.DoorVolume.Value))
		{
			return base.PlayerManager.Position;
		}
		Volume volume = base.LevelManager.Volumes[base.PlayerManager.DoorVolume.Value];
		Vector3 vector = (volume.From + volume.To) / 2f;
		return base.PlayerManager.Position * (Vector3.UnitY + base.CameraManager.Viewpoint.DepthMask()) + vector * base.CameraManager.Viewpoint.SideMask();
	}

	protected override void Begin()
	{
		base.Begin();
		if (base.GameState.IsTrialMode && base.PlayerManager.DoorEndsTrial)
		{
			GameService.EndTrial(forceRestart: false);
			base.PlayerManager.Action = ActionType.ExitDoor;
			base.PlayerManager.Action = ActionType.Idle;
			return;
		}
		if (!base.PlayerManager.DoorVolume.HasValue || !base.LevelManager.Volumes.ContainsKey(base.PlayerManager.DoorVolume.Value))
		{
			base.PlayerManager.Action = ActionType.Idle;
			return;
		}
		if (!base.PlayerManager.SpinThroughDoor)
		{
			sound.EmitAt(base.PlayerManager.Position);
		}
		hasFlipped = (hasChangedLevel = false);
		newLevel = base.PlayerManager.NextLevel;
		step = 0f;
		base.PlayerManager.InDoorTransition = true;
		transitionTime = default(TimeSpan);
		base.PlayerManager.Velocity *= Vector3.UnitY;
		if (base.PlayerManager.SpinThroughDoor)
		{
			base.CameraManager.ChangeViewpoint(base.CameraManager.Viewpoint.GetRotatedView(1), 2f);
			base.PlayerManager.LookingDirection = HorizontalDirection.Right;
		}
		if (base.LevelManager.DestinationIsFarAway)
		{
			Vector3 center = base.CameraManager.Center;
			float num = 4f * (float)((!base.LevelManager.Descending) ? 1 : (-1)) / base.CameraManager.PixelsPerTrixel;
			base.CameraManager.StickyCam = false;
			base.CameraManager.Constrained = true;
			Volume volume = base.LevelManager.Volumes[base.PlayerManager.DoorVolume.Value];
			Vector2 vector = ((volume.ActorSettings == null) ? Vector2.Zero : volume.ActorSettings.FarawayPlaneOffset);
			if (volume.ActorSettings != null && volume.ActorSettings.WaterLocked)
			{
				vector.Y = volume.ActorSettings.WaterOffset / 2f;
			}
			if (volume.ActorSettings != null)
			{
				base.GameState.FarawaySettings.DestinationOffset = volume.ActorSettings.DestinationOffset;
			}
			Vector3 vector2 = base.CameraManager.Viewpoint.RightVector() * vector.X + Vector3.Up * vector.Y;
			Vector3 destinationCenter = new Vector3(base.PlayerManager.Position.X, base.PlayerManager.Position.Y + num, base.PlayerManager.Position.Z) + vector2 * 2f;
			StartTransition(center, destinationCenter);
		}
		base.GomezService.OnEnterDoor();
	}

	private void StartTransition(Vector3 originalCenter, Vector3 destinationCenter)
	{
		Waiters.Interpolate(1.75, delegate(float s)
		{
			base.CameraManager.Center = Vector3.Lerp(originalCenter, destinationCenter, Easing.EaseInOut(s, EasingType.Quadratic));
		}).AutoPause = true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		if (!type.IsEnteringDoor())
		{
			return step != -1f;
		}
		return true;
	}

	protected override bool Act(TimeSpan elapsed)
	{
		if (base.GameState.Loading)
		{
			return false;
		}
		if (base.PlayerManager.Action.IsEnteringDoor())
		{
			base.PlayerManager.Animation.Timing.Update(elapsed);
		}
		if (step < 1f && !hasFlipped && base.PlayerManager.SpinThroughDoor)
		{
			Vector3 position = base.PlayerManager.Position;
			base.PlayerManager.Position = Vector3.Lerp(spinOrigin, spinDestination, step);
			if (base.PlayerManager.CarriedInstance != null)
			{
				base.PlayerManager.CarriedInstance.Position += base.PlayerManager.Position - position;
			}
		}
		if (string.IsNullOrEmpty(newLevel))
		{
			base.PlayerManager.Action = ActionType.Idle;
			step = -1f;
			base.PlayerManager.InDoorTransition = false;
			return false;
		}
		if (base.PlayerManager.SpinThroughDoor && hasFlipped && !hasChangedLevel)
		{
			step = 0f;
		}
		else
		{
			transitionTime += elapsed;
			float num = (base.PlayerManager.SpinThroughDoor ? 0.75f : 1.25f);
			if (hasChangedLevel)
			{
				num *= 0.75f;
			}
			step = (float)transitionTime.TotalSeconds / num;
		}
		if (step >= 1f && !hasFlipped)
		{
			if (base.LevelManager.DestinationIsFarAway)
			{
				ServiceHelper.AddComponent(new FarawayTransition(base.Game));
				base.PlayerManager.Action = ActionType.Idle;
				step = -1f;
				base.PlayerManager.InDoorTransition = false;
				return false;
			}
			if (skipFade)
			{
				DoLoad(dummy: false);
			}
			else
			{
				base.GameState.Loading = true;
				Worker<bool> worker = ThreadPool.Take<bool>(DoLoad);
				worker.Finished += delegate
				{
					ThreadPool.Return(worker);
				};
				worker.Start(context: false);
			}
			transitionTime = default(TimeSpan);
			step = 0f;
			hasFlipped = true;
		}
		else if (step >= 1f && hasFlipped)
		{
			step = -1f;
			base.PlayerManager.SpinThroughDoor = false;
			base.PlayerManager.InDoorTransition = false;
			if (base.PlayerManager.Action.IsEnteringDoor())
			{
				base.PlayerManager.Action = ActionType.Idle;
			}
		}
		if (base.PlayerManager.SpinThroughDoor && hasFlipped && base.CameraManager.ActionRunning && !hasChangedLevel)
		{
			hasChangedLevel = true;
			base.GameState.SkipRendering = true;
			base.CameraManager.ChangeViewpoint(base.CameraManager.Viewpoint.GetRotatedView(-1), 0f);
			base.CameraManager.SnapInterpolation();
			base.CameraManager.ChangeViewpoint(base.CameraManager.Viewpoint.GetRotatedView(1));
			base.GameState.SkipRendering = false;
		}
		return false;
	}

	private void DoLoad(bool dummy)
	{
		base.LevelManager.ChangeLevel(newLevel);
		base.PlayerManager.ForceOverlapsDetermination();
		TrileInstance surface = base.PlayerManager.AxisCollision[VerticalDirection.Up].Surface;
		if (surface != null && surface.Trile.ActorSettings.Type == ActorType.UnlockedDoor && FezMath.OrientationFromPhi(surface.Trile.ActorSettings.Face.ToPhi() + surface.Phi) == base.CameraManager.Viewpoint.VisibleOrientation())
		{
			base.GameState.SaveData.ThisLevel.FilledConditions.UnlockedDoorCount++;
			TrileEmplacement id = surface.Emplacement + Vector3.UnitY;
			TrileInstance trileInstance = base.LevelManager.TrileInstanceAt(ref id);
			if (trileInstance.Trile.ActorSettings.Type == ActorType.UnlockedDoor)
			{
				base.GameState.SaveData.ThisLevel.FilledConditions.UnlockedDoorCount++;
			}
			base.LevelManager.ClearTrile(surface);
			base.LevelManager.ClearTrile(trileInstance);
			base.GameState.SaveData.ThisLevel.InactiveTriles.Add(surface.Emplacement);
			surface.ActorSettings.Inactive = true;
		}
		if (!base.PlayerManager.SpinThroughDoor)
		{
			if (base.PlayerManager.CarriedInstance != null)
			{
				base.PlayerManager.Action = (base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type.IsHeavy() ? ActionType.ExitDoorCarryHeavy : ActionType.ExitDoorCarry);
			}
			else
			{
				base.PlayerManager.Action = ActionType.ExitDoor;
			}
		}
		if (!skipFade)
		{
			base.GameState.ScheduleLoadEnd = true;
		}
		base.GameState.SkipLoadScreen = false;
	}

	public override void Draw(GameTime gameTime)
	{
		if (!IsActionAllowed(base.PlayerManager.Action) || base.LevelManager.DestinationIsFarAway || skipFade)
		{
			return;
		}
		float num = (float)Math.Pow(FezMath.Saturate(step), base.PlayerManager.SpinThroughDoor ? 2 : 3);
		if (base.PlayerManager.CarriedInstance != null && !base.PlayerManager.SpinThroughDoor)
		{
			trileFadeQuad.Rotation = base.CameraManager.Rotation;
			trileFadeQuad.Position = base.PlayerManager.CarriedInstance.Center;
			switch (base.PlayerManager.CarriedInstance.Trile.ActorSettings.Type)
			{
			case ActorType.Bomb:
			case ActorType.BigBomb:
				trileFadeQuad.Scale = new Vector3(0.75f, 1f, 0.75f);
				break;
			case ActorType.Vase:
				trileFadeQuad.Scale = new Vector3(0.875f, 1f, 0.875f);
				break;
			default:
				trileFadeQuad.Scale = Vector3.One;
				break;
			}
			trileFadeQuad.Material.Opacity = (hasFlipped ? (1f - step) : num);
			base.GraphicsDevice.PrepareStencilRead(CompareFunction.Equal, StencilMask.NoSilhouette);
			trileFadeQuad.Draw();
			base.GraphicsDevice.PrepareStencilWrite(StencilMask.None);
		}
		fadeOutQuad.Material.Opacity = (hasFlipped ? (1f - num) : num);
		fadeOutQuad.Draw();
	}
}
