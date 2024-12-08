using System.Linq;
using Common;
using FezEngine.Components;
using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Structure.Input;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Structure;
using FezGame.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Services.Scripting;

internal class DotService : IDotService, IScriptingBase
{
	private SoundEffect sTextNext;

	private SoundEffect sDotTalk;

	private SoundEmitter eDotTalk;

	[ServiceDependency]
	public IGameService GameService { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public IInputManager InputManager { private get; set; }

	[ServiceDependency]
	public IGameCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IDotManager Dot { private get; set; }

	[ServiceDependency]
	public ISpeechBubbleManager SpeechBubble { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	public void ResetEvents()
	{
	}

	public LongRunningAction Say(string line, bool nearGomez, bool hideAfter)
	{
		if (sTextNext == null)
		{
			sTextNext = CMProvider.Global.Load<SoundEffect>("Sounds/Ui/TextNext");
		}
		if (sDotTalk == null)
		{
			sDotTalk = CMProvider.Global.Load<SoundEffect>("Sounds/Dot/Talk");
		}
		bool first = false;
		bool wasPreventing = false;
		IWaiter w = null;
		return new LongRunningAction(delegate
		{
			if (!first && LevelManager.Name == "VILLAGEVILLE_3D" && !line.StartsWith("DOT_INTRO") && GameService.GetLevelState != "INTRO_COMPLETE")
			{
				return false;
			}
			if (!first)
			{
				switch (PlayerManager.Action)
				{
				default:
					return false;
				case ActionType.Idle:
				case ActionType.Walking:
				case ActionType.Running:
				case ActionType.Sliding:
				case ActionType.Teetering:
					break;
				}
				if (Intro.Instance != null)
				{
					return false;
				}
				Dot.ComeOut();
				wasPreventing = Dot.PreventPoI;
				Dot.PreventPoI = true;
				if (nearGomez || Dot.Hidden || Dot.Burrowing || Dot.Behaviour == DotHost.BehaviourType.RoamInVolume)
				{
					Dot.Behaviour = DotHost.BehaviourType.ReadyToTalk;
				}
				string @string = GameText.GetString(line);
				SpeechBubble.Font = SpeechFont.Pixel;
				SpeechBubble.ChangeText(@string);
				PlayerManager.CanControl = false;
				SpeechBubble.Origin = Dot.Position;
				first = true;
				if (eDotTalk == null || eDotTalk.Dead)
				{
					eDotTalk = sDotTalk.EmitAt(Dot.Position, loop: true);
				}
				else
				{
					eDotTalk.Cue.Resume();
					eDotTalk.VolumeFactor = 1f;
					eDotTalk.Position = Dot.Position;
				}
				w = Waiters.Wait(0.075f * (float)@string.StripPunctuation().Length * (float)((!Culture.IsCJK) ? 1 : 2), delegate
				{
					if (eDotTalk != null)
					{
						eDotTalk.FadeOutAndPause(0.1f);
					}
				});
				w.AutoPause = true;
			}
			SpeechBubble.Origin = Dot.Position;
			if (eDotTalk != null && !eDotTalk.Dead)
			{
				eDotTalk.Position = Dot.Position;
			}
			if (InputManager.CancelTalk == FezButtonState.Pressed)
			{
				if (w.Alive)
				{
					w.Cancel();
				}
				if (eDotTalk != null && !eDotTalk.Dead && eDotTalk.Cue.State == SoundState.Playing)
				{
					eDotTalk.Cue.Pause();
				}
				sTextNext.Emit();
				SpeechBubble.Hide();
				return true;
			}
			return false;
		}, delegate
		{
			if (hideAfter)
			{
				Dot.Burrow();
				if (LevelManager.Name == "VILLAGEVILLE_3D")
				{
					CameraManager.Constrained = false;
				}
			}
			Dot.PreventPoI = wasPreventing;
			PlayerManager.CanControl = true;
		});
	}

	public LongRunningAction ComeBackAndHide(bool withCamera)
	{
		Dot.MoveWithCamera(PlayerManager.Position + new Vector3(0f, 1f, 0f), burrowAfter: true);
		return new LongRunningAction((float _, float __) => Dot.Burrowing, delegate
		{
			CameraManager.Constrained = false;
			Dot.PreventPoI = false;
		});
	}

	public LongRunningAction SpiralAround(bool withCamera, bool hideDot)
	{
		Dot.PreventPoI = true;
		Vector3 center = LevelManager.Triles.Values.Aggregate(Vector3.Zero, (Vector3 a, TrileInstance b) => a + b.Position) / LevelManager.Triles.Values.Count;
		Vector3 to = LevelManager.Size + 4f * Vector3.UnitY;
		if (LevelManager.Name == "SEWER_HUB")
		{
			to -= Vector3.UnitY * 20f;
		}
		if (LevelManager.Name.EndsWith("BIG_TOWER"))
		{
			to -= Vector3.UnitY * 13.5f;
		}
		Dot.SpiralAround(new Volume
		{
			From = Vector3.Zero,
			To = to
		}, center, hideDot);
		return new LongRunningAction((float _, float __) => Dot.Behaviour == DotHost.BehaviourType.ReadyToTalk, delegate
		{
			CameraManager.Constrained = false;
			Dot.PreventPoI = false;
		});
	}
}
