using System.Linq;
using Common;
using FezEngine.Components;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Services;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Components;

internal class PointsOfInterestHost : GameComponent
{
	private Volume[] PointsList;

	private bool InGroup;

	private SoundEffect sDotTalk;

	private SoundEmitter eDotTalk;

	private IWaiter talkWaiter;

	[ServiceDependency]
	public IDotManager Dot { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public ISpeechBubbleManager SpeechBubble { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	public PointsOfInterestHost(Game game)
		: base(game)
	{
	}

	public override void Initialize()
	{
		base.Initialize();
		sDotTalk = CMProvider.Global.Load<SoundEffect>("Sounds/Dot/Talk");
		LevelManager.LevelChanged += TryInitialize;
		TryInitialize();
	}

	private void TryInitialize()
	{
		Dot.Reset();
		PointsList = LevelManager.Volumes.Values.Where((Volume x) => x.ActorSettings != null && x.Enabled && x.ActorSettings.IsPointOfInterest).ToArray();
		SyncTutorials();
	}

	private void SyncTutorials()
	{
		Volume[] pointsList = PointsList;
		foreach (Volume volume in pointsList)
		{
			foreach (DotDialogueLine item in volume.ActorSettings.DotDialogue)
			{
				if (GameState.SaveData.OneTimeTutorials.TryGetValue(item.ResourceText, out var value) && value)
				{
					volume.ActorSettings.PreventHey = true;
					break;
				}
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		if (GameState.Loading || GameState.InMap || PointsList.Length == 0 || Dot.PreventPoI || GameState.Paused || GameState.InMenuCube || (Dot.Owner != null && Dot.Owner != this) || GameState.FarawaySettings.InTransition || PlayerManager.Action.IsEnteringDoor())
		{
			return;
		}
		Vector3 vector = CameraManager.Viewpoint.DepthMask();
		BoundingBox boundingBox = new BoundingBox(PlayerManager.Center - new Vector3(6f), PlayerManager.Center + new Vector3(6f));
		Volume volume = null;
		Volume[] pointsList = PointsList;
		foreach (Volume volume2 in pointsList)
		{
			if (!volume2.Enabled)
			{
				continue;
			}
			BoundingBox boundingBox2 = volume2.BoundingBox;
			boundingBox2.Min -= vector * 1000f;
			boundingBox2.Max += vector * 1000f;
			if (boundingBox.Contains(boundingBox2) != 0)
			{
				Dot.ComeOut();
				Dot.Behaviour = DotHost.BehaviourType.RoamInVolume;
				volume = (Dot.RoamingVolume = volume2);
			}
			if (SpeechBubble.Hidden)
			{
				if (talkWaiter != null && talkWaiter.Alive)
				{
					talkWaiter.Cancel();
				}
				if (eDotTalk != null && !eDotTalk.Dead)
				{
					eDotTalk.FadeOutAndPause(0.1f);
				}
			}
			if (!SpeechBubble.Hidden && PlayerManager.CurrentVolumes.Contains(volume2) && volume2.ActorSettings.DotDialogue.Count > 0 && (PlayerManager.Action == ActionType.Suffering || PlayerManager.Action == ActionType.SuckedIn || PlayerManager.Action == ActionType.LesserWarp || PlayerManager.Action == ActionType.GateWarp))
			{
				SpeechBubble.Hide();
				Dot.Behaviour = DotHost.BehaviourType.ReadyToTalk;
				InGroup = false;
			}
			if (GameState.InFpsMode || !PlayerManager.CurrentVolumes.Contains(volume2) || volume2.ActorSettings.DotDialogue.Count <= 0)
			{
				continue;
			}
			if (SpeechBubble.Hidden && (InputManager.CancelTalk == FezButtonState.Pressed || InGroup))
			{
				switch (PlayerManager.Action)
				{
				case ActionType.Idle:
				case ActionType.Walking:
				case ActionType.Running:
				case ActionType.Sliding:
				case ActionType.Landing:
				case ActionType.IdlePlay:
				case ActionType.IdleSleep:
				case ActionType.IdleLookAround:
				case ActionType.IdleYawn:
				{
					volume2.ActorSettings.NextLine = (volume2.ActorSettings.NextLine + 1) % volume2.ActorSettings.DotDialogue.Count;
					int num = (volume2.ActorSettings.NextLine + 1) % volume2.ActorSettings.DotDialogue.Count;
					InGroup = volume2.ActorSettings.DotDialogue[volume2.ActorSettings.NextLine].Grouped && volume2.ActorSettings.DotDialogue[num].Grouped && num != 0;
					volume2.ActorSettings.PreventHey = true;
					string resourceText = volume2.ActorSettings.DotDialogue[volume2.ActorSettings.NextLine].ResourceText;
					if (GameState.SaveData.OneTimeTutorials.TryGetValue(resourceText, out var value) && !value)
					{
						GameState.SaveData.OneTimeTutorials[resourceText] = true;
						SyncTutorials();
					}
					if (talkWaiter != null && talkWaiter.Alive)
					{
						talkWaiter.Cancel();
					}
					string @string = GameText.GetString(resourceText);
					SpeechBubble.ChangeText(@string);
					PlayerManager.Action = ActionType.ReadingSign;
					if (eDotTalk == null || eDotTalk.Dead)
					{
						eDotTalk = sDotTalk.EmitAt(Dot.Position, loop: true);
					}
					else
					{
						eDotTalk.Position = Dot.Position;
						Waiters.Wait(0.10000000149011612, delegate
						{
							eDotTalk.Cue.Resume();
							eDotTalk.VolumeFactor = 1f;
						}).AutoPause = true;
					}
					talkWaiter = Waiters.Wait(0.1f + 0.075f * (float)@string.StripPunctuation().Length * (float)((!Culture.IsCJK) ? 1 : 2), delegate
					{
						if (eDotTalk != null)
						{
							eDotTalk.FadeOutAndPause(0.1f);
						}
					});
					talkWaiter.AutoPause = true;
					break;
				}
				}
			}
			if (SpeechBubble.Hidden && !volume2.ActorSettings.PreventHey)
			{
				if (PlayerManager.Grounded)
				{
					switch (PlayerManager.Action)
					{
					case ActionType.Idle:
					case ActionType.Walking:
					case ActionType.Running:
					case ActionType.Sliding:
					case ActionType.Landing:
					case ActionType.IdlePlay:
					case ActionType.IdleSleep:
					case ActionType.IdleLookAround:
					case ActionType.IdleYawn:
						Dot.Behaviour = DotHost.BehaviourType.ThoughtBubble;
						Dot.FaceButton = DotFaceButton.B;
						if (Dot.Owner != this)
						{
							Dot.Hey();
						}
						Dot.Owner = this;
						break;
					default:
						Dot.Behaviour = DotHost.BehaviourType.ReadyToTalk;
						break;
					}
				}
			}
			else
			{
				Dot.Behaviour = DotHost.BehaviourType.ReadyToTalk;
			}
			if (!SpeechBubble.Hidden)
			{
				SpeechBubble.Origin = Dot.Position;
			}
			if (Dot.Behaviour == DotHost.BehaviourType.ReadyToTalk || Dot.Behaviour == DotHost.BehaviourType.ThoughtBubble)
			{
				break;
			}
		}
		if (Dot.Behaviour != DotHost.BehaviourType.ThoughtBubble && Dot.Owner == this && SpeechBubble.Hidden)
		{
			Dot.Owner = null;
		}
		if (volume == null && Dot.RoamingVolume != null)
		{
			Dot.Burrow();
		}
	}
}
