using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework.Audio;

namespace FezGame.Services.Scripting;

public class SoundService : ISoundService, IScriptingBase
{
	private readonly Dictionary<string, Mutable<int>> soundIndices = new Dictionary<string, Mutable<int>>();

	public static bool ImmediateEffect;

	[ServiceDependency]
	public IContentManagerProvider CMProvider { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	public void ResetEvents()
	{
		soundIndices.Clear();
	}

	public void Play(string soundName)
	{
		try
		{
			CMProvider.Global.Load<SoundEffect>("Sounds/" + soundName).Emit();
		}
		catch (Exception ex)
		{
			Logger.Log("Sounds Service", LogSeverity.Warning, ex.Message);
		}
	}

	public void PlayNext(string soundPrefix)
	{
		if (!soundIndices.TryGetValue(soundPrefix, out var value))
		{
			soundIndices.Add(soundPrefix, value = new Mutable<int>());
		}
		value.Value++;
		try
		{
			CMProvider.CurrentLevel.Load<SoundEffect>("Sounds/" + soundPrefix + value.Value).Emit();
		}
		catch (Exception ex)
		{
			Logger.Log("Sounds Service", LogSeverity.Warning, ex.Message);
		}
	}

	public void SetMusicVolume(float volume)
	{
		SoundManager.MusicVolumeFactor = volume;
	}

	public void FadeMusicOut(float overSeconds)
	{
		SoundManager.FadeVolume(SoundManager.MusicVolumeFactor, 0f, overSeconds);
	}

	public void FadeMusicTo(float to, float overSeconds)
	{
		SoundManager.FadeVolume(SoundManager.MusicVolumeFactor, to, overSeconds);
	}

	public void ResetIndices(string soundPrefix)
	{
		soundIndices.Remove(soundPrefix);
	}

	public void ChangeMusic(string newMusic)
	{
		if (SoundManager.CurrentlyPlayingSong == null || SoundManager.CurrentlyPlayingSong.Name != newMusic)
		{
			SoundManager.PlayNewSong(newMusic, 8f);
		}
		SoundManager.ScriptChangedSong = true;
	}

	public void ChangePhases(string trackName, bool dawn, bool day, bool dusk, bool night)
	{
		Loop loop = LevelManager.Song.Loops.FirstOrDefault((Loop x) => x.Name == trackName);
		if (loop == null)
		{
			Logger.Log("Sound Service", "Track not found for ChangePhases ('" + trackName + "')");
			return;
		}
		loop.Day = day;
		loop.Dawn = dawn;
		loop.Dusk = dusk;
		loop.Night = night;
	}

	public void UnmuteTrack(string trackName, float fadeDuration)
	{
		if (LevelManager.MutedLoops.Remove(trackName))
		{
			SoundManager.UpdateSongActiveTracks(ImmediateEffect ? 0f : fadeDuration);
		}
	}

	public void MuteTrack(string trackName, float fadeDuration)
	{
		if (!LevelManager.MutedLoops.Contains(trackName))
		{
			LevelManager.MutedLoops.Add(trackName);
			SoundManager.UpdateSongActiveTracks(ImmediateEffect ? 0f : fadeDuration);
		}
		else
		{
			Logger.Log("SoundService", LogSeverity.Warning, "No such track or track is already muted : '" + trackName + "'");
		}
	}

	public void UnmuteAmbience(string trackName, float fadeDuration)
	{
		SoundManager.UnmuteAmbience(trackName, ImmediateEffect ? 0f : fadeDuration);
	}

	public void MuteAmbience(string trackName, float fadeDuration)
	{
		SoundManager.MuteAmbience(trackName, ImmediateEffect ? 0f : fadeDuration);
	}
}
