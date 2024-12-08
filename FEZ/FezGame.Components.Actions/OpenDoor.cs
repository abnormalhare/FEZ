using System;
using System.Linq;
using FezEngine;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components.Actions;

internal class OpenDoor : PlayerAction
{
	private static readonly TimeSpan OpeningDuration = TimeSpan.FromSeconds(1.25);

	private TimeSpan sinceOpened;

	private TrileInstance doorBottom;

	private TrileInstance doorTop;

	private TrileInstance holeBottom;

	private TrileInstance holeTop;

	private TrileInstance tempBottom;

	private TrileInstance tempTop;

	private ArtObjectInstance aoInstance;

	private Quaternion aoInitialRotation;

	private float initialPhi;

	private Vector3 initialPosition;

	private Vector3 initialAoPosition;

	private bool isUnlocked;

	private SoundEffect unlockSound;

	private SoundEffect openSound;

	private SoundEffect turnSound;

	private int lastFrame;

	[ServiceDependency]
	public IDotManager DotManager { private get; set; }

	public OpenDoor(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.LevelManager.LevelChanging += delegate
		{
			bool flag = false;
			foreach (TrileEmplacement inactiveTrile in base.GameState.SaveData.ThisLevel.InactiveTriles)
			{
				TrileEmplacement id = inactiveTrile;
				TrileInstance bottom = base.LevelManager.TrileInstanceAt(ref id);
				if (bottom != null && bottom.Trile.ActorSettings.Type.IsDoor())
				{
					id = bottom.Emplacement + Vector3.UnitY;
					TrileInstance instance = base.LevelManager.TrileInstanceAt(ref id);
					TrileGroup group;
					ArtObjectInstance artObjectInstance;
					if ((group = base.LevelManager.Groups.Values.FirstOrDefault((TrileGroup x) => x.Triles.Contains(bottom))) != null && (artObjectInstance = base.LevelManager.ArtObjects.Values.FirstOrDefault((ArtObjectInstance x) => x.ActorSettings.AttachedGroup == group.Id)) != null)
					{
						base.LevelManager.RemoveArtObject(artObjectInstance);
					}
					base.LevelManager.ClearTrile(bottom);
					base.LevelManager.ClearTrile(instance);
					flag = true;
				}
			}
			if (flag && !base.GameState.FarawaySettings.InTransition)
			{
				base.LevelMaterializer.CullInstances();
			}
		};
		base.Initialize();
	}

	protected override void LoadContent()
	{
		base.LoadContent();
		unlockSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/UnlockDoor");
		openSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/OpenDoor");
		turnSound = base.CMProvider.Global.Load<SoundEffect>("Sounds/Gomez/TurnAway");
	}

	protected override void TestConditions()
	{
		if (!base.PlayerManager.Grounded)
		{
			UnDotize();
			return;
		}
		if (base.PlayerManager.CarriedInstance != null)
		{
			UnDotize();
			return;
		}
		bool flag = false;
		foreach (NearestTriles value in base.PlayerManager.AxisCollision.Values)
		{
			TrileInstance surface = value.Surface;
			if (surface != null)
			{
				Trile trile = surface.Trile;
				FaceOrientation faceOrientation = FezMath.OrientationFromPhi(trile.ActorSettings.Face.ToPhi() + surface.Phi);
				if (trile.ActorSettings.Type.IsDoor() && faceOrientation == base.CameraManager.Viewpoint.VisibleOrientation())
				{
					flag = (base.GameState.SaveData.Keys > 0 && base.LevelManager.Name != "VILLAGEVILLE_2D") || trile.ActorSettings.Type == ActorType.UnlockedDoor;
					isUnlocked = trile.ActorSettings.Type == ActorType.UnlockedDoor;
					doorBottom = surface;
					break;
				}
			}
		}
		if (!flag)
		{
			UnDotize();
			return;
		}
		if (doorBottom.ActorSettings.Inactive)
		{
			UnDotize();
			return;
		}
		if (!base.PlayerManager.HideFez && base.PlayerManager.CanControl && !DotManager.PreventPoI && doorBottom.Trile.ActorSettings.Type == ActorType.Door)
		{
			DotManager.Behaviour = DotHost.BehaviourType.ThoughtBubble;
			DotManager.FaceButton = DotFaceButton.Up;
			DotManager.ComeOut();
			if (DotManager.Owner != this)
			{
				DotManager.Hey();
			}
			DotManager.Owner = this;
		}
		if ((base.GameState.IsTrialMode && base.LevelManager.Name == "trial/VILLAGEVILLE_2D") || base.InputManager.ExactUp != FezButtonState.Pressed)
		{
			return;
		}
		TrileGroup trileGroup = null;
		aoInstance = null;
		foreach (TrileGroup value2 in base.LevelManager.Groups.Values)
		{
			if (value2.Triles.Contains(doorBottom))
			{
				trileGroup = value2;
				break;
			}
		}
		if (trileGroup != null)
		{
			foreach (ArtObjectInstance value3 in base.LevelManager.ArtObjects.Values)
			{
				if (value3.ActorSettings.AttachedGroup == trileGroup.Id)
				{
					aoInstance = value3;
					break;
				}
			}
			if (aoInstance != null)
			{
				aoInitialRotation = aoInstance.Rotation;
				initialAoPosition = aoInstance.Position;
			}
		}
		base.WalkTo.Destination = GetDestination;
		base.PlayerManager.Action = ActionType.WalkingTo;
		base.WalkTo.NextAction = ActionType.OpeningDoor;
	}

	private void UnDotize()
	{
		if (DotManager.Owner == this)
		{
			DotManager.Owner = null;
			DotManager.Burrow();
		}
	}

	private Vector3 GetDestination()
	{
		Viewpoint viewpoint = base.CameraManager.Viewpoint;
		return base.PlayerManager.Position * (Vector3.UnitY + viewpoint.DepthMask()) + doorBottom.Center * viewpoint.SideMask();
	}

	protected override void Begin()
	{
		if (DotManager.Owner == this)
		{
			DotManager.Burrow();
		}
		base.PlayerManager.Velocity *= Vector3.UnitY;
		base.PlayerManager.LookingDirection = HorizontalDirection.Right;
		base.GameState.SaveData.ThisLevel.InactiveTriles.Add(doorBottom.Emplacement);
		doorBottom.ActorSettings.Inactive = true;
		TrileEmplacement id = doorBottom.Emplacement + Vector3.UnitY;
		doorTop = base.LevelManager.TrileInstanceAt(ref id);
		sinceOpened = TimeSpan.FromSeconds(-1.0);
		initialPhi = doorBottom.Phi;
		initialPosition = doorBottom.Position;
		if (doorBottom.Trile.ActorSettings.Type == ActorType.Door)
		{
			base.GameState.SaveData.ThisLevel.FilledConditions.LockedDoorCount++;
			if (doorTop.Trile.ActorSettings.Type == ActorType.Door)
			{
				base.GameState.SaveData.ThisLevel.FilledConditions.LockedDoorCount++;
			}
			base.GameState.SaveData.Keys--;
			base.GameState.OnHudElementChanged();
		}
		else
		{
			base.GameState.SaveData.ThisLevel.FilledConditions.UnlockedDoorCount++;
			if (doorTop.Trile.ActorSettings.Type == ActorType.UnlockedDoor)
			{
				base.GameState.SaveData.ThisLevel.FilledConditions.UnlockedDoorCount++;
			}
		}
		id = doorBottom.Emplacement + base.CameraManager.Viewpoint.ForwardVector();
		holeBottom = base.LevelManager.TrileInstanceAt(ref id);
		id = doorTop.Emplacement + base.CameraManager.Viewpoint.ForwardVector();
		holeTop = base.LevelManager.TrileInstanceAt(ref id);
		id = doorBottom.Emplacement + base.CameraManager.Viewpoint.ForwardVector() * 2f;
		tempBottom = base.LevelManager.TrileInstanceAt(ref id);
		id = doorTop.Emplacement + base.CameraManager.Viewpoint.ForwardVector() * 2f;
		tempTop = base.LevelManager.TrileInstanceAt(ref id);
		if (tempBottom != null)
		{
			base.LevelManager.ClearTrile(tempBottom);
		}
		if (tempTop != null)
		{
			base.LevelManager.ClearTrile(tempTop);
		}
		turnSound.EmitAt(base.PlayerManager.Position);
	}

	protected override bool Act(TimeSpan elapsed)
	{
		sinceOpened += elapsed;
		float num = FezMath.Saturate((float)sinceOpened.Ticks / (float)OpeningDuration.Ticks);
		if (doorBottom.InstanceId != -1)
		{
			float num2 = Easing.EaseInOut(num, EasingType.Sine);
			float num3 = num2 * ((float)Math.PI / 2f);
			TrileInstance trileInstance = doorBottom;
			float phi = (doorTop.Phi = initialPhi + num3);
			trileInstance.Phi = phi;
			if (aoInstance != null)
			{
				aoInstance.Rotation = Quaternion.CreateFromAxisAngle(Vector3.Up, num3) * aoInitialRotation;
			}
			Vector3 vector = base.CameraManager.Viewpoint.RightVector();
			Vector3 vector2 = base.CameraManager.Viewpoint.ForwardVector();
			Vector3 vector3 = base.CameraManager.Viewpoint.DepthMask();
			Vector3 vector4 = vector2 * 1.125f * num2 + vector * (float)(Math.Sin(num3 * 2f) * ((Math.Sqrt(2.0) - 1.0) / 2.0) - (double)(0.1875f * num2));
			doorBottom.Position = new Vector3(initialPosition.X, doorBottom.Position.Y, initialPosition.Z) + vector4;
			base.LevelManager.UpdateInstance(doorBottom);
			doorTop.Position = new Vector3(initialPosition.X, doorTop.Position.Y, initialPosition.Z) + vector4;
			base.LevelManager.UpdateInstance(doorTop);
			if (holeBottom != null)
			{
				holeBottom.Position = new Vector3(initialPosition.X, doorBottom.Position.Y, initialPosition.Z) + vector4 * vector3 + vector2;
				base.LevelManager.UpdateInstance(holeBottom);
			}
			if (holeTop != null)
			{
				holeTop.Position = new Vector3(initialPosition.X, doorTop.Position.Y, initialPosition.Z) + vector4 * vector3 + vector2;
				base.LevelManager.UpdateInstance(holeTop);
			}
			if (aoInstance != null)
			{
				vector4 = vector2 * 1.125f * num2 + vector * (float)(Math.Sin(num3 * 2f) * ((Math.Sqrt(2.0) - 1.0) / 2.0) + (double)(0.1875f * num2));
				aoInstance.Position = initialAoPosition + vector4;
			}
			if (num == 1f)
			{
				base.LevelManager.ClearTrile(doorBottom);
				base.LevelManager.ClearTrile(doorTop);
				if (holeBottom != null)
				{
					holeBottom.Position = new Vector3(initialPosition.X, doorBottom.Position.Y, initialPosition.Z) + vector2;
					base.LevelManager.UpdateInstance(holeBottom);
				}
				if (holeTop != null)
				{
					holeTop.Position = new Vector3(initialPosition.X, doorTop.Position.Y, initialPosition.Z) + vector2;
					base.LevelManager.UpdateInstance(holeTop);
				}
				if (tempBottom != null)
				{
					base.LevelManager.RestoreTrile(tempBottom);
				}
				if (tempTop != null)
				{
					base.LevelManager.RestoreTrile(tempTop);
				}
				base.LevelMaterializer.CullInstances();
			}
		}
		if (base.PlayerManager.Animation.Timing.Ended)
		{
			if (!isUnlocked)
			{
				base.PlayerManager.Action = ActionType.Walking;
			}
			base.PlayerManager.Action = ActionType.Idle;
		}
		int frame = base.PlayerManager.Animation.Timing.Frame;
		if (lastFrame != frame)
		{
			if (frame == 7)
			{
				if (doorBottom.Trile.ActorSettings.Type == ActorType.Door)
				{
					unlockSound.EmitAt(base.PlayerManager.Position);
				}
				else
				{
					openSound.EmitAt(base.PlayerManager.Position);
				}
			}
			lastFrame = frame;
		}
		return true;
	}

	protected override bool IsActionAllowed(ActionType type)
	{
		return type == ActionType.OpeningDoor;
	}
}
