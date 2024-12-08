using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine;
using FezEngine.Components;
using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using FezGame.Components;
using FezGame.Components.Scripting;
using FezGame.Structure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Services.Scripting;

public class LevelService : ILevelService, IScriptingBase
{
	private SoundEffect sewageLevelSound;

	private SoundEffect sSolvedSecret;

	private SoundEmitter sewageLevelEmitter;

	private readonly Stack<object> waterStopStack = new Stack<object>();

	public bool FirstVisit => GameState.SaveData.ThisLevel.FirstVisit;

	[ServiceDependency]
	public IGameService GameService { private get; set; }

	[ServiceDependency]
	public IGameLevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public IPlayerManager PlayerManager { private get; set; }

	[ServiceDependency]
	public IGameStateManager GameState { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	internal IScriptingManager ScriptingManager { private get; set; }

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	public event Action Start = Util.NullAction;

	public void OnStart()
	{
		if (sSolvedSecret == null)
		{
			sSolvedSecret = CMProvider.Global.Load<SoundEffect>("Sounds/MiscActors/SecretSolved");
		}
		this.Start();
	}

	public LongRunningAction ExploChangeLevel(string levelName)
	{
		return null;
	}

	public LongRunningAction SetWaterHeight(float height)
	{
		float sign = Math.Sign(height - LevelManager.WaterHeight);
		TrileInstance[] buoyantTriles = LevelManager.Triles.Values.Where((TrileInstance x) => x.Trile.ActorSettings.Type.IsBuoyant()).ToArray();
		return new LongRunningAction(delegate(float elapsed, float __)
		{
			if (GameState.Paused || GameState.InMap || !CameraManager.ActionRunning || CameraManager.Viewpoint == Viewpoint.Perspective)
			{
				return false;
			}
			if ((float)Math.Sign(height - LevelManager.WaterHeight) != sign)
			{
				return true;
			}
			LevelManager.WaterSpeed = 1.2f * sign;
			LevelManager.WaterHeight += LevelManager.WaterSpeed * elapsed;
			if (LevelManager.WaterType != LiquidType.Lava)
			{
				TrileInstance[] array = buoyantTriles;
				foreach (TrileInstance trileInstance in array)
				{
					if (trileInstance.Center.Y < LevelManager.WaterHeight)
					{
						trileInstance.PhysicsState.Velocity = new Vector3(0f, 0.01f, 0f);
					}
				}
			}
			return false;
		}, delegate
		{
			LevelManager.WaterSpeed = 0f;
			if (LevelManager.WaterType == LiquidType.Water)
			{
				GameState.SaveData.GlobalWaterLevelModifier = LevelManager.WaterHeight - LevelManager.OriginalWaterHeight;
			}
			else
			{
				GameState.SaveData.ThisLevel.LastStableLiquidHeight = LevelManager.WaterHeight;
			}
		});
	}

	public LongRunningAction RaiseWater(float unitsPerSecond, float toHeight)
	{
		float sign = Math.Sign(toHeight - LevelManager.WaterHeight);
		TrileInstance[] buoyantTriles = LevelManager.Triles.Values.Where((TrileInstance x) => x.Trile.ActorSettings.Type.IsBuoyant()).ToArray();
		if (LevelManager.WaterSpeed != 0f)
		{
			waterStopStack.Push(new object());
		}
		bool flag = false;
		if (LevelManager.WaterType == LiquidType.Sewer || LevelManager.Name == "WATER_WHEEL")
		{
			if (sewageLevelEmitter != null && !sewageLevelEmitter.Dead)
			{
				sewageLevelEmitter.Cue.Stop();
				flag = true;
			}
			if (sewageLevelSound == null)
			{
				sewageLevelSound = CMProvider.Global.Load<SoundEffect>("Sounds/Sewer/SewageLevelChange");
			}
			sewageLevelEmitter = sewageLevelSound.Emit(loop: true, !flag);
			if (!flag)
			{
				sewageLevelEmitter.VolumeFactor = 0f;
				sewageLevelEmitter.Cue.Resume();
			}
		}
		return new LongRunningAction(delegate(float elapsed, float __)
		{
			if (GameState.Paused || GameState.InMap || GameState.ForceTimePaused || !CameraManager.ActionRunning || CameraManager.Viewpoint == Viewpoint.Perspective)
			{
				return false;
			}
			if ((float)Math.Sign(toHeight - LevelManager.WaterHeight) != sign || waterStopStack.Count > 0)
			{
				return true;
			}
			if (sewageLevelEmitter != null && !sewageLevelEmitter.Dead)
			{
				sewageLevelEmitter.VolumeFactor = FezMath.Saturate(sewageLevelEmitter.VolumeFactor + elapsed * 3f);
			}
			LevelManager.WaterSpeed = unitsPerSecond * sign;
			LevelManager.WaterHeight += LevelManager.WaterSpeed * elapsed;
			TrileInstance[] array = buoyantTriles;
			foreach (TrileInstance trileInstance in array)
			{
				if (!trileInstance.PhysicsState.Floating && trileInstance.PhysicsState.Static && trileInstance.Center.Y < LevelManager.WaterHeight - 0.5f)
				{
					trileInstance.PhysicsState.ForceNonStatic = true;
					trileInstance.PhysicsState.Ground = default(MultipleHits<TrileInstance>);
				}
			}
			return false;
		}, delegate
		{
			LevelManager.WaterSpeed = 0f;
			if (LevelManager.WaterType == LiquidType.Water)
			{
				GameState.SaveData.GlobalWaterLevelModifier = LevelManager.WaterHeight - LevelManager.OriginalWaterHeight;
			}
			else
			{
				GameState.SaveData.ThisLevel.LastStableLiquidHeight = LevelManager.WaterHeight;
			}
			if (waterStopStack.Count == 0)
			{
				if (sewageLevelEmitter != null && !sewageLevelEmitter.Dead)
				{
					sewageLevelEmitter.FadeOutAndDie(0.75f);
				}
				sewageLevelEmitter = null;
			}
			if (waterStopStack.Count > 0)
			{
				waterStopStack.Pop();
			}
		});
	}

	public void StopWater()
	{
		waterStopStack.Push(new object());
	}

	public LongRunningAction AllowPipeChangeLevel(string levelName)
	{
		PlayerManager.NextLevel = levelName;
		PlayerManager.PipeVolume = ScriptingManager.EvaluatedScript.InitiatingTrigger.Object.Identifier;
		return new LongRunningAction(delegate
		{
			PlayerManager.NextLevel = null;
			PlayerManager.PipeVolume = null;
		});
	}

	public LongRunningAction ChangeLevel(string levelName, bool asDoor, bool spin, bool trialEnding)
	{
		if (asDoor)
		{
			PlayerManager.SpinThroughDoor = spin;
			PlayerManager.NextLevel = levelName;
			PlayerManager.DoorVolume = ScriptingManager.EvaluatedScript.InitiatingTrigger.Object.Identifier;
			PlayerManager.DoorEndsTrial = trialEnding && GameState.IsTrialMode;
			LevelManager.WentThroughSecretPassage |= PlayerManager.DoorVolume.HasValue && LevelManager.Volumes[PlayerManager.DoorVolume.Value].ActorSettings != null && LevelManager.Volumes[PlayerManager.DoorVolume.Value].ActorSettings.IsSecretPassage;
			return new LongRunningAction(delegate
			{
				PlayerManager.NextLevel = null;
				PlayerManager.DoorVolume = null;
				PlayerManager.DoorEndsTrial = false;
				if (!PlayerManager.Action.IsEnteringDoor())
				{
					LevelManager.WentThroughSecretPassage = false;
				}
			});
		}
		ServiceHelper.AddComponent(new LevelTransition(ServiceHelper.Game, levelName));
		return new LongRunningAction();
	}

	public LongRunningAction ChangeLevelToVolume(string levelName, int toVolume, bool asDoor, bool spin, bool trialEnding)
	{
		if (PlayerManager.Action.IsEnteringDoor())
		{
			return null;
		}
		LevelManager.DestinationVolumeId = toVolume;
		LongRunningAction lra = ChangeLevel(levelName, asDoor, spin, trialEnding);
		return new LongRunningAction(delegate
		{
			if (lra.OnDispose != null)
			{
				lra.OnDispose();
			}
			if (!PlayerManager.Action.IsEnteringDoor())
			{
				LevelManager.DestinationVolumeId = null;
			}
		});
	}

	public LongRunningAction ReturnToLastLevel(bool asDoor, bool spin)
	{
		return ChangeLevel(LevelManager.LastLevelName, asDoor, spin, trialEnding: false);
	}

	public LongRunningAction ChangeToFarAwayLevel(string levelName, int toVolume, bool trialEnding)
	{
		LevelManager.DestinationIsFarAway = true;
		LevelManager.DestinationVolumeId = toVolume;
		LongRunningAction lra = ChangeLevel(levelName, asDoor: true, spin: false, trialEnding);
		return new LongRunningAction(delegate
		{
			if (lra.OnDispose != null)
			{
				lra.OnDispose();
			}
			LevelManager.DestinationIsFarAway = false;
			if (!PlayerManager.Action.IsEnteringDoor())
			{
				LevelManager.DestinationVolumeId = null;
			}
		});
	}

	public void ResolvePuzzle()
	{
		GameState.SaveData.ThisLevel.FilledConditions.SecretCount++;
		Volume volume;
		if ((volume = PlayerManager.CurrentVolumes.FirstOrDefault((Volume x) => x.ActorSettings != null && x.ActorSettings.IsPointOfInterest)) != null && volume.Enabled)
		{
			volume.Enabled = false;
			GameState.SaveData.ThisLevel.InactiveVolumes.Add(volume.Id);
		}
		GameState.Save();
		sSolvedSecret.Play();
		SoundManager.MusicVolumeFactor = 0.125f;
		Waiters.Wait(2.75, delegate
		{
			SoundManager.FadeVolume(0.125f, 1f, 3f);
		}).AutoPause = true;
	}

	public void ResolvePuzzleSilent()
	{
		GameState.SaveData.ThisLevel.FilledConditions.SecretCount++;
		GameState.Save();
	}

	public void ResolvePuzzleSoundOnly()
	{
		sSolvedSecret.Play();
		SoundManager.MusicVolumeFactor = 0.125f;
		Waiters.Wait(2.75, delegate
		{
			SoundManager.FadeVolume(0.125f, 1f, 3f);
		}).AutoPause = true;
	}

	public void ResetEvents()
	{
		this.Start = Util.NullAction;
	}
}
