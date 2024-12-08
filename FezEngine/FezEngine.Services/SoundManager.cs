using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using Common;
using ContentSerialization;
using FezEngine.Components;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;

namespace FezEngine.Services;

public class SoundManager : GameComponent, ISoundManager
{
	private const float VolumeFadeSeconds = 2f;

	private const float LowFrequency = 0.025f;

	private const float MasterMusicVolume = 1f;

	public static bool NoMusic;

	private Dictionary<string, byte[]> MusicCache;

	private ActiveTrackedSong ActiveSong;

	private ActiveTrackedSong ShelvedSong;

	private ActiveAmbience ActiveAmbience;

	private VolumeLevels VolumeLevels;

	private float FrequencyStep;

	public static bool NoMoreSounds;

	private bool initialized;

	private const int SoundPerUpdate = 5;

	private readonly List<SoundEmitter> toUpdate = new List<SoundEmitter>(5);

	private int firstIndex;

	private float musicVolume;

	private float musicVolumeFactor;

	private float soundEffectVolume;

	private float globalVolumeFactor;

	private static readonly MethodInfo applyFilter = typeof(SoundEffectInstance).GetMethod("INTERNAL_applyLowPassFilter", BindingFlags.Instance | BindingFlags.NonPublic);

	private object[] gainContainer = new object[1] { 1f };

	private IWaiter freqTransitionWaiter;

	private IWaiter volTransitionWaiter;

	public bool IsLowPass { get; private set; }

	public List<SoundEmitter> Emitters { get; private set; }

	public Vector2 LimitDistance { get; private set; }

	public bool ScriptChangedSong { get; set; }

	public float MusicVolume
	{
		get
		{
			return musicVolume;
		}
		set
		{
			musicVolume = FezMath.Saturate(value);
			OggStream.SyncAllVolume();
		}
	}

	public float MusicVolumeFactor
	{
		get
		{
			return musicVolumeFactor;
		}
		set
		{
			musicVolumeFactor = FezMath.Saturate(value);
			MusicVolume = musicVolume;
			OggStream.SyncAllVolume();
		}
	}

	public float SoundEffectVolume
	{
		get
		{
			return soundEffectVolume;
		}
		set
		{
			soundEffectVolume = FezMath.Saturate(value);
			SoundEffect.MasterVolume = FezMath.Saturate(Easing.EaseIn(soundEffectVolume * globalVolumeFactor, EasingType.Quadratic));
			OggStream.SyncAllVolume();
		}
	}

	public float GlobalVolumeFactor
	{
		get
		{
			return globalVolumeFactor;
		}
		set
		{
			globalVolumeFactor = value;
			SoundEffectVolume = soundEffectVolume;
			MusicVolume = musicVolume;
		}
	}

	public float LowPassHFGain
	{
		get
		{
			return (float)gainContainer[0];
		}
		set
		{
			gainContainer[0] = MathHelper.Clamp(value, 0f, 1f);
			lock (Emitters)
			{
				foreach (SoundEmitter emitter in Emitters)
				{
					if (emitter.Cue != null && emitter.LowPass)
					{
						applyFilter.Invoke(emitter.Cue, gainContainer);
					}
				}
			}
			OggStream.SyncAllFilter(applyFilter, gainContainer);
		}
	}

	public TimeSpan PlayPosition
	{
		get
		{
			if (ActiveSong != null)
			{
				return ActiveSong.PlayPosition;
			}
			return TimeSpan.Zero;
		}
	}

	public TrackedSong CurrentlyPlayingSong
	{
		get
		{
			if (ActiveSong != null)
			{
				return ActiveSong.Song;
			}
			return null;
		}
	}

	[ServiceDependency]
	public IContentManagerProvider CMProvider { private get; set; }

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	[ServiceDependency]
	public IDefaultCameraManager CameraManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { protected get; set; }

	[ServiceDependency]
	public IDebuggingBag DebuggingBag { private get; set; }

	public event Action SongChanged;

	public SoundManager(Game game, bool noMusic = false)
		: base(game)
	{
		Emitters = new List<SoundEmitter>();
		base.UpdateOrder = 100;
		NoMusic = noMusic;
	}

	public override void Initialize()
	{
		musicVolumeFactor = 1f;
		MusicVolume = SettingsManager.Settings.MusicVolume;
		SoundEffectVolume = SettingsManager.Settings.SoundVolume;
		GlobalVolumeFactor = 1f;
		CameraManager.ProjectionChanged += UpdateLimitDistance;
		UpdateLimitDistance();
		LevelManager.LevelChanging += KillSounds;
		EngineState.PauseStateChanged += delegate
		{
			if (EngineState.Paused)
			{
				Pause();
			}
			else
			{
				Resume();
			}
		};
	}

	public void InitializeLibrary()
	{
		if (initialized)
		{
			return;
		}
		initialized = true;
		using FileStream input = File.OpenRead(Path.Combine("Content", "Music.pak"));
		using BinaryReader binaryReader = new BinaryReader(input);
		int num = binaryReader.ReadInt32();
		MusicCache = new Dictionary<string, byte[]>(num);
		for (int i = 0; i < num; i++)
		{
			string text = binaryReader.ReadString();
			int count = binaryReader.ReadInt32();
			if (MusicCache.ContainsKey(text))
			{
				Logger.Log("SoundManager", "Skipped " + text + " track because it was already loaded");
			}
			else
			{
				MusicCache.Add(text, binaryReader.ReadBytes(count));
			}
		}
	}

	private void UpdateLimitDistance()
	{
		LimitDistance = new Vector2(1f, 1f / CameraManager.AspectRatio) * CameraManager.Radius / 2f;
	}

	public override void Update(GameTime gameTime)
	{
		if (!EngineState.Loading)
		{
			UpdateEmitters();
		}
	}

	private void UpdateEmitters()
	{
		lock (Emitters)
		{
			for (int num = Emitters.Count - 1; num >= 0; num--)
			{
				if (Emitters[num].Dead)
				{
					Emitters[num].Dispose();
					Emitters.RemoveAt(num);
				}
			}
			FactorizeVolume();
		}
	}

	public void FactorizeVolume()
	{
		if (Emitters.Count == 0)
		{
			return;
		}
		float num = 0f;
		int num2 = 0;
		firstIndex %= Emitters.Count;
		toUpdate.Clear();
		int num3 = firstIndex;
		int num4 = 0;
		while (num4 < Emitters.Count)
		{
			if (num3 == Emitters.Count)
			{
				num3 -= Emitters.Count;
			}
			SoundEmitter soundEmitter = Emitters[num3];
			num += soundEmitter.NonFactorizedVolume;
			if (soundEmitter.NonFactorizedVolume != 0f)
			{
				num2++;
			}
			if (num4 < 5 || soundEmitter.New)
			{
				toUpdate.Add(soundEmitter);
				soundEmitter.New = false;
			}
			num4++;
			num3++;
		}
		firstIndex += 5;
		float num5 = -1f / (float)((num2 == 0) ? 1 : num2) + 2f;
		foreach (SoundEmitter item in toUpdate)
		{
			if (item.FactorizeVolume)
			{
				item.VolumeFactor = FezMath.Saturate(num5 / ((num == 0f) ? 1f : num));
			}
			item.Update();
		}
	}

	public void RegisterLowPass(SoundEffectInstance sfi)
	{
		applyFilter.Invoke(sfi, gainContainer);
	}

	public void FadeFrequencies(bool lowPass)
	{
		FadeFrequencies(lowPass, 2f);
	}

	public void FadeFrequencies(bool toLowPass, float fadeDuration)
	{
		if (!IsLowPass && toLowPass)
		{
			float originalStep = FrequencyStep;
			IWaiter thisWaiter = null;
			freqTransitionWaiter = (thisWaiter = Waiters.Interpolate(fadeDuration * (1f - originalStep), delegate(float s)
			{
				if (freqTransitionWaiter == thisWaiter)
				{
					FrequencyStep = originalStep + s * (1f - originalStep);
					LowPassHFGain = MathHelper.Lerp(1f, 0.025f, Easing.EaseOut(FrequencyStep, EasingType.Cubic));
				}
			}, delegate
			{
				if (freqTransitionWaiter == thisWaiter)
				{
					FrequencyStep = 1f;
					LowPassHFGain = 0.025f;
				}
			}));
		}
		else if (IsLowPass && !toLowPass)
		{
			float originalStep2 = FrequencyStep;
			IWaiter thisWaiter2 = null;
			freqTransitionWaiter = (thisWaiter2 = Waiters.Interpolate(fadeDuration * originalStep2, delegate(float s)
			{
				if (freqTransitionWaiter == thisWaiter2)
				{
					FrequencyStep = originalStep2 * (1f - s);
					LowPassHFGain = MathHelper.Lerp(1f, 0.025f, Easing.EaseIn(FrequencyStep, EasingType.Cubic));
				}
			}, delegate
			{
				if (freqTransitionWaiter == thisWaiter2)
				{
					FrequencyStep = 0f;
					LowPassHFGain = 1f;
				}
			}));
		}
		IsLowPass = toLowPass;
	}

	public void FadeVolume(float fromVolume, float toVolume, float overSeconds)
	{
		IWaiter thisWaiter = null;
		volTransitionWaiter = (thisWaiter = Waiters.Interpolate(overSeconds, delegate(float step)
		{
			if (volTransitionWaiter == thisWaiter && !EngineState.DotLoading)
			{
				MusicVolumeFactor = MathHelper.Lerp(fromVolume, toVolume, step);
			}
		}, delegate
		{
			if (volTransitionWaiter == thisWaiter)
			{
				if (!EngineState.DotLoading)
				{
					MusicVolumeFactor = toVolume;
				}
				volTransitionWaiter = null;
			}
		}));
	}

	public void Pause()
	{
		lock (Emitters)
		{
			foreach (SoundEmitter emitter in Emitters)
			{
				if (!emitter.Dead && emitter.Cue.State == SoundState.Playing)
				{
					emitter.Cue.Pause();
					emitter.WasPlaying = true;
				}
			}
		}
		if (ActiveSong != null)
		{
			ActiveSong.Pause();
		}
		if (ActiveAmbience != null)
		{
			ActiveAmbience.Pause();
		}
	}

	public void Resume()
	{
		lock (Emitters)
		{
			foreach (SoundEmitter emitter in Emitters)
			{
				if (!emitter.Dead && emitter.WasPlaying && emitter.Cue.State == SoundState.Paused)
				{
					emitter.Cue.Resume();
				}
			}
		}
		if (ActiveSong != null)
		{
			ActiveSong.Resume();
		}
		if (ActiveAmbience != null)
		{
			ActiveAmbience.Resume();
		}
	}

	public void KillSounds()
	{
		lock (Emitters)
		{
			SoundEmitter[] array = Emitters.ToArray();
			foreach (SoundEmitter soundEmitter in array)
			{
				if (!soundEmitter.Persistent)
				{
					if (!soundEmitter.Dead)
					{
						soundEmitter.Dispose();
					}
					Emitters.Remove(soundEmitter);
				}
			}
		}
	}

	public void KillSounds(float fadeDuration)
	{
		lock (Emitters)
		{
			SoundEmitter[] array = Emitters.ToArray();
			foreach (SoundEmitter soundEmitter in array)
			{
				if (!soundEmitter.Persistent)
				{
					soundEmitter.FadeOutAndDie(fadeDuration, autoPause: false);
					Emitters.Remove(soundEmitter);
				}
			}
		}
	}

	public OggStream GetCue(string name, bool asyncPrecache = false)
	{
		OggStream oggStream = null;
		try
		{
			string text = name.Replace(" ^ ", "\\");
			bool flag = name.Contains("Ambience");
			byte[] array = MusicCache[text.ToLower(CultureInfo.InvariantCulture)];
			oggStream = new OggStream(new MemoryStream(array, 0, array.Length, writable: false, publiclyVisible: true))
			{
				Category = (flag ? "Ambience" : "Music"),
				IsLooped = flag
			};
			oggStream.RealName = name;
			if (name.Contains("Gomez"))
			{
				oggStream.LowPass = false;
			}
		}
		catch (Exception ex)
		{
			Logger.Log("SoundManager", LogSeverity.Error, "Failed for '" + name + "' ('" + name.Replace(" ^ ", "\\").ToLower(CultureInfo.InvariantCulture) + "' : " + ex);
			Logger.Log("SoundManager", "Music Cache contained : " + Util.DeepToString(MusicCache.Keys));
		}
		return oggStream;
	}

	public void UpdateSongActiveTracks()
	{
		if (ActiveSong != null)
		{
			ActiveSong.SetMutedLoops(LevelManager.MutedLoops, 6f);
		}
	}

	public void UpdateSongActiveTracks(float fadeDuration)
	{
		if (ActiveSong != null)
		{
			ActiveSong.SetMutedLoops(LevelManager.MutedLoops, fadeDuration);
		}
	}

	public void PlayNewSong()
	{
		if (!NoMusic)
		{
			TrackedSong currentlyPlayingSong = CurrentlyPlayingSong;
			if (ActiveSong != null)
			{
				ActiveSong.FadeOutAndRemoveComponent();
			}
			if (LevelManager.Song == null)
			{
				ActiveSong = null;
			}
			else
			{
				ServiceHelper.AddComponent(ActiveSong = new ActiveTrackedSong(base.Game));
			}
			if (currentlyPlayingSong != CurrentlyPlayingSong)
			{
				this.SongChanged();
			}
		}
	}

	public void PlayNewSong(float fadeDuration)
	{
		if (!NoMusic)
		{
			TrackedSong currentlyPlayingSong = CurrentlyPlayingSong;
			if (ActiveSong != null)
			{
				ActiveSong.FadeOutAndRemoveComponent(fadeDuration);
			}
			if (LevelManager.Song == null)
			{
				ActiveSong = null;
			}
			else
			{
				ServiceHelper.AddComponent(ActiveSong = new ActiveTrackedSong(base.Game));
			}
			if (currentlyPlayingSong != CurrentlyPlayingSong)
			{
				this.SongChanged();
			}
		}
	}

	public void PlayNewSong(string name)
	{
		if (!NoMusic)
		{
			PlayNewSong(name, interrupt: true);
		}
	}

	public void PlayNewSong(string name, bool interrupt)
	{
		if (!NoMusic)
		{
			TrackedSong currentlyPlayingSong = CurrentlyPlayingSong;
			if (!interrupt)
			{
				ShelvedSong = ActiveSong;
			}
			else if (ActiveSong != null)
			{
				ActiveSong.FadeOutAndRemoveComponent();
			}
			if (string.IsNullOrEmpty(name))
			{
				ActiveSong = null;
			}
			else
			{
				TrackedSong trackedSong = CMProvider.CurrentLevel.Load<TrackedSong>("Music/" + name);
				trackedSong.Initialize();
				ServiceHelper.AddComponent(ActiveSong = new ActiveTrackedSong(base.Game, trackedSong, LevelManager.MutedLoops));
			}
			if (currentlyPlayingSong != CurrentlyPlayingSong)
			{
				this.SongChanged();
			}
		}
	}

	public void PlayNewSong(string name, float fadeDuration)
	{
		if (!NoMusic)
		{
			PlayNewSong(name, fadeDuration, interrupt: true);
		}
	}

	public void PlayNewSong(string name, float fadeDuration, bool interrupt)
	{
		if (!NoMusic)
		{
			TrackedSong currentlyPlayingSong = CurrentlyPlayingSong;
			if (!interrupt)
			{
				ShelvedSong = ActiveSong;
			}
			else if (ActiveSong != null)
			{
				ActiveSong.FadeOutAndRemoveComponent(fadeDuration);
			}
			if (string.IsNullOrEmpty(name))
			{
				ActiveSong = null;
			}
			else
			{
				TrackedSong trackedSong = CMProvider.CurrentLevel.Load<TrackedSong>("Music/" + name);
				trackedSong.Initialize();
				ServiceHelper.AddComponent(ActiveSong = new ActiveTrackedSong(base.Game, trackedSong, LevelManager.MutedLoops));
			}
			if (currentlyPlayingSong != CurrentlyPlayingSong)
			{
				this.SongChanged();
			}
		}
	}

	public void PlayNewAmbience()
	{
		if (ActiveAmbience == null)
		{
			ServiceHelper.AddComponent(ActiveAmbience = new ActiveAmbience(base.Game, LevelManager.AmbienceTracks));
		}
		else
		{
			ActiveAmbience.ChangeTracks(LevelManager.AmbienceTracks);
		}
	}

	public void UnmuteAmbienceTracks()
	{
		if (ActiveAmbience != null)
		{
			ActiveAmbience.UnmuteTracks(apply: false);
		}
	}

	public void UnmuteAmbienceTracks(bool apply)
	{
		if (ActiveAmbience != null)
		{
			ActiveAmbience.UnmuteTracks(apply);
		}
	}

	public void MuteAmbienceTracks()
	{
		if (ActiveAmbience != null)
		{
			ActiveAmbience.MuteTracks();
		}
	}

	public void MuteAmbience(string trackName, float fadeDuration)
	{
		if (ActiveAmbience != null)
		{
			ActiveAmbience.MuteTrack(trackName, fadeDuration);
			return;
		}
		Waiters.Wait(() => ActiveAmbience != null, delegate
		{
			ActiveAmbience.MuteTrack(trackName, fadeDuration);
		});
	}

	public void UnmuteAmbience(string trackName, float fadeDuration)
	{
		if (ActiveAmbience != null)
		{
			ActiveAmbience.UnmuteTrack(trackName, fadeDuration);
			return;
		}
		Waiters.Wait(() => ActiveAmbience != null, delegate
		{
			ActiveAmbience.UnmuteTrack(trackName, fadeDuration);
		});
	}

	public SoundEmitter AddEmitter(SoundEmitter emitter)
	{
		if (!NoMoreSounds)
		{
			lock (Emitters)
			{
				Emitters.Add(emitter);
			}
		}
		return emitter;
	}

	public void UnshelfSong()
	{
		if (ShelvedSong != null)
		{
			ActiveSong = ShelvedSong;
			ShelvedSong = null;
		}
	}

	public void Stop()
	{
		if (ActiveSong != null)
		{
			ServiceHelper.RemoveComponent(ActiveSong);
		}
		if (ActiveAmbience != null)
		{
			ServiceHelper.RemoveComponent(ActiveAmbience);
		}
		ActiveSong = null;
		ActiveAmbience = null;
		MusicVolumeFactor = 1f;
	}

	public void ReloadVolumeLevels()
	{
		string cleanPath = SharedContentManager.GetCleanPath(Path.Combine(Path.Combine(CMProvider.Global.RootDirectory, "Sounds"), "SoundLevels.sdl"));
		FileStream fileStream;
		try
		{
			fileStream = new FileStream(cleanPath, FileMode.Open, FileAccess.Read);
		}
		catch (Exception ex)
		{
			if (!(ex is FileNotFoundException) && !(ex is DirectoryNotFoundException))
			{
				Logger.Log("Sound Levels", LogSeverity.Warning, ex.Message);
				return;
			}
			Logger.Log("Sound Levels", LogSeverity.Warning, "Could not find levels file, ignoring...");
			fileStream = null;
		}
		if (fileStream == null)
		{
			VolumeLevels = new VolumeLevels();
		}
		else
		{
			VolumeLevels = SdlSerializer.Deserialize<VolumeLevels>(new StreamReader(fileStream));
		}
	}

	public float GetVolumeLevelFor(string name)
	{
		if (VolumeLevels == null)
		{
			return 1f;
		}
		lock (VolumeLevels)
		{
			if (name.Contains("Gomez") && LevelManager.Name == "PYRAMID")
			{
				return 0f;
			}
			if (VolumeLevels.Sounds.TryGetValue(name.Replace("/", "\\"), out var value))
			{
				return value.Level;
			}
			return 1f;
		}
	}
}
