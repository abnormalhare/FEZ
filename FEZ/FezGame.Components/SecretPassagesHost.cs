using System;
using System.Linq;
using FezEngine;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Structure.Scripting;
using FezEngine.Tools;
using FezGame.Components.Actions;
using FezGame.Services;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;

namespace FezGame.Components;

internal class SecretPassagesHost : GameComponent
{
	private ArtObjectInstance DoorAo;

	private Volume AssociatedVolume;

	private TrileGroup AttachedGroup;

	private bool Accessible;

	private BackgroundPlane GlowPlane;

	private Viewpoint ExpectedViewpoint;

	private bool MoveUp;

	private TimeSpan SinceStarted;

	private Vector3 AoOrigin;

	private Vector3 PlaneOrigin;

	private SoundEffect sRumble;

	private SoundEffect sLightUp;

	private SoundEffect sFadeOut;

	private SoundEmitter eRumble;

	private bool loop;

	[ServiceDependency]
	public IWalkToService WalkToService { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IGroupService GroupService { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public ILevelMaterializer LevelMaterializer { private get; set; }

	public SecretPassagesHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		LevelManager.LevelChanging += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		DoorAo = null;
		AttachedGroup = null;
		AssociatedVolume = null;
		base.Enabled = false;
		sRumble = null;
		sLightUp = null;
		sFadeOut = null;
		if (eRumble != null && !eRumble.Dead)
		{
			eRumble.Cue.Stop();
		}
		eRumble = null;
		foreach (ArtObjectInstance value in LevelManager.ArtObjects.Values)
		{
			if (value.ArtObject.ActorType == ActorType.SecretPassage)
			{
				DoorAo = value;
				base.Enabled = true;
				break;
			}
		}
		if (GlowPlane != null)
		{
			GlowPlane.Dispose();
			GlowPlane = null;
		}
		if (!base.Enabled)
		{
			return;
		}
		AttachedGroup = LevelManager.Groups[DoorAo.ActorSettings.AttachedGroup.Value];
		AssociatedVolume = LevelManager.Volumes.Values.FirstOrDefault((Volume x) => x.ActorSettings != null && x.ActorSettings.IsSecretPassage);
		string key = null;
		foreach (Script value2 in LevelManager.Scripts.Values)
		{
			foreach (ScriptAction action in value2.Actions)
			{
				if (!(action.Object.Type == "Level") || !action.Operation.Contains("Level"))
				{
					continue;
				}
				foreach (ScriptTrigger trigger in value2.Triggers)
				{
					if (trigger.Object.Type == "Volume" && trigger.Event == "Enter" && trigger.Object.Identifier.HasValue)
					{
						key = action.Arguments[0];
					}
				}
			}
		}
		Accessible = GameState.SaveData.World.ContainsKey(key);
		sRumble = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/MiscActors/Rumble");
		sLightUp = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Zu/DoorBitLightUp");
		sFadeOut = CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/Zu/DoorBitFadeOut");
		if (!Accessible)
		{
			base.Enabled = false;
			return;
		}
		ExpectedViewpoint = AssociatedVolume.Orientations.First().AsViewpoint();
		if (LevelManager.WentThroughSecretPassage)
		{
			MoveUp = true;
			SinceStarted = TimeSpan.Zero;
			AoOrigin = DoorAo.Position;
			PlaneOrigin = DoorAo.Position + AssociatedVolume.Orientations.First().AsVector() / 2.03125f;
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.Paused)
		{
			return;
		}
		if (GlowPlane == null)
		{
			if (!Accessible)
			{
				base.Enabled = false;
			}
			else
			{
				Texture2D texture = CMProvider.CurrentLevel.Load<Texture2D>("Other Textures/glow/secret_passage");
				GlowPlane = new BackgroundPlane(LevelMaterializer.StaticPlanesMesh, texture)
				{
					Fullbright = true,
					Opacity = (MoveUp ? 1 : 0),
					Position = DoorAo.Position + AssociatedVolume.Orientations.First().AsVector() / 2.03125f,
					Rotation = FezMath.QuaternionFromPhi(AssociatedVolume.Orientations.First().ToPhi()),
					AttachedGroup = AttachedGroup.Id
				};
				LevelManager.AddPlane(GlowPlane);
			}
		}
		if (MoveUp)
		{
			if (eRumble == null)
			{
				eRumble = sRumble.EmitAt(DoorAo.Position, loop: true, 0f, 0.625f);
				SoundEmitter rumbleEmitter = eRumble;
				Waiters.Wait(1.25, delegate
				{
					rumbleEmitter.FadeOutAndDie(0.25f);
				}).AutoPause = true;
			}
			DoMoveUp(gameTime.ElapsedGameTime);
		}
		else if (DoorAo.Visible && !DoorAo.ActorSettings.Inactive && CameraManager.Viewpoint == ExpectedViewpoint && !PlayerManager.Background)
		{
			Vector3 vector = (DoorAo.Position - PlayerManager.Position).Abs() * CameraManager.Viewpoint.ScreenSpaceMask();
			bool flag = vector.X + vector.Z < 0.75f && vector.Y < 2f && (DoorAo.Position - PlayerManager.Position).Dot(CameraManager.Viewpoint.ForwardVector()) >= 0f;
			float opacity = GlowPlane.Opacity;
			GlowPlane.Opacity = MathHelper.Lerp(GlowPlane.Opacity, flag.AsNumeric(), 0.05f);
			if (GlowPlane.Opacity > opacity && opacity > 0.1f && loop)
			{
				sLightUp.EmitAt(DoorAo.Position);
				loop = false;
			}
			else if (GlowPlane.Opacity < opacity && !loop)
			{
				sFadeOut.EmitAt(DoorAo.Position);
				loop = true;
			}
			if (flag && PlayerManager.Grounded && InputManager.ExactUp == FezButtonState.Pressed)
			{
				Open();
			}
		}
	}

	private void DoMoveUp(TimeSpan elapsed)
	{
		SinceStarted += elapsed;
		float num = Easing.EaseInOut(FezMath.Saturate((float)SinceStarted.TotalSeconds / 2f), EasingType.Quadratic);
		int num2 = Math.Sign(AttachedGroup.Path.Segments[0].Destination.Y);
		GlowPlane.Position = Vector3.Lerp(PlaneOrigin + Vector3.UnitY * 2f * num2, PlaneOrigin, num);
		DoorAo.Position = Vector3.Lerp(AoOrigin + Vector3.UnitY * 2f * num2, AoOrigin, num);
		if (num == 1f)
		{
			MoveUp = false;
		}
	}

	private void Open()
	{
		GroupService.RunPathOnce(AttachedGroup.Id, backwards: false);
		PlayerManager.CanControl = false;
		base.Enabled = false;
		PlayerManager.Action = ActionType.WalkingTo;
		WalkToService.Destination = GetDestination;
		WalkToService.NextAction = ActionType.Idle;
		eRumble = sRumble.EmitAt(DoorAo.Position, loop: true);
		BackgroundPlane glowPlane = GlowPlane;
		Waiters.Interpolate(0.5, delegate(float step)
		{
			glowPlane.Opacity = Easing.EaseOut(1f - step, EasingType.Quadratic);
		}).AutoPause = true;
		SoundEmitter rumbleEmitter = eRumble;
		Waiters.Wait(1.5, delegate
		{
			rumbleEmitter.FadeOutAndDie(0.5f);
		}).AutoPause = true;
		Waiters.Wait(2.0, delegate
		{
			PlayerManager.CanControl = true;
			PlayerManager.Action = ActionType.OpeningDoor;
			PlayerManager.Action = ActionType.Idle;
		}).AutoPause = true;
	}

	private Vector3 GetDestination()
	{
		Viewpoint viewpoint = CameraManager.Viewpoint;
		return PlayerManager.Position * (Vector3.UnitY + viewpoint.DepthMask()) + DoorAo.Position * viewpoint.SideMask();
	}
}
