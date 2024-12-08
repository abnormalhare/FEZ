using System.Collections.Generic;
using System.Linq;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

internal class ActiveAmbience : GameComponent
{
	private List<AmbienceTrack> Tracks;

	private readonly List<ActiveAmbienceTrack> ActiveTracks = new List<ActiveAmbienceTrack>();

	private bool cancelPause;

	private bool resumeRequested;

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	public ActiveAmbience(Game game, IEnumerable<AmbienceTrack> tracks)
		: base(game)
	{
		Tracks = new List<AmbienceTrack>(tracks);
	}

	public override void Initialize()
	{
		base.Enabled = false;
		foreach (AmbienceTrack track in Tracks)
		{
			bool flag = TimeManager.IsDayPhaseForMusic(DayPhase.Day) && track.Day;
			flag |= TimeManager.IsDayPhaseForMusic(DayPhase.Night) && track.Night;
			flag |= TimeManager.IsDayPhaseForMusic(DayPhase.Dawn) && track.Dawn;
			flag |= TimeManager.IsDayPhaseForMusic(DayPhase.Dusk) && track.Dusk;
			ActiveTracks.Add(new ActiveAmbienceTrack(track, flag));
		}
		Tracks.Clear();
		Tracks = null;
		base.Enabled = true;
	}

	public override void Update(GameTime gameTime)
	{
		bool flag = TimeManager.IsDayPhaseForMusic(DayPhase.Day);
		bool flag2 = TimeManager.IsDayPhaseForMusic(DayPhase.Dawn);
		bool flag3 = TimeManager.IsDayPhaseForMusic(DayPhase.Dusk);
		bool flag4 = TimeManager.IsDayPhaseForMusic(DayPhase.Night);
		foreach (ActiveAmbienceTrack activeTrack in ActiveTracks)
		{
			bool activeForDayPhase = activeTrack.ActiveForDayPhase;
			activeTrack.ActiveForDayPhase = false;
			activeTrack.ActiveForDayPhase |= flag && activeTrack.Track.Day;
			activeTrack.ActiveForDayPhase |= flag4 && activeTrack.Track.Night;
			activeTrack.ActiveForDayPhase |= flag2 && activeTrack.Track.Dawn;
			activeTrack.ActiveForDayPhase |= flag3 && activeTrack.Track.Dusk;
			if (activeForDayPhase != activeTrack.ActiveForDayPhase && !activeTrack.ForceMuted)
			{
				activeTrack.OnMuteStateChanged(16f);
			}
		}
	}

	public void Pause()
	{
		if (!base.Enabled)
		{
			return;
		}
		Waiters.Interpolate(0.25, delegate
		{
			cancelPause |= resumeRequested || !base.Enabled;
		}, delegate
		{
			if (!cancelPause && !resumeRequested)
			{
				foreach (ActiveAmbienceTrack activeTrack in ActiveTracks)
				{
					activeTrack.Pause();
				}
				base.Enabled = false;
			}
			cancelPause = (resumeRequested = false);
		});
	}

	public void Resume()
	{
		if (base.Enabled)
		{
			return;
		}
		foreach (ActiveAmbienceTrack activeTrack in ActiveTracks)
		{
			activeTrack.Resume();
		}
		base.Enabled = true;
		resumeRequested = true;
		Waiters.Interpolate(0.125, delegate
		{
			if (base.Enabled)
			{
				resumeRequested = false;
			}
		});
	}

	public void ChangeTracks(IEnumerable<AmbienceTrack> tracks)
	{
		foreach (ActiveAmbienceTrack activeTrack in ActiveTracks)
		{
			foreach (AmbienceTrack track in tracks)
			{
				if (track.Name == activeTrack.Track.Name)
				{
					activeTrack.Track = track;
					bool num = activeTrack.ActiveForDayPhase && !activeTrack.ForceMuted && !activeTrack.WasMuted;
					activeTrack.WasMuted = false;
					bool flag = TimeManager.IsDayPhaseForMusic(DayPhase.Day) && track.Day;
					flag |= TimeManager.IsDayPhaseForMusic(DayPhase.Night) && track.Night;
					flag |= TimeManager.IsDayPhaseForMusic(DayPhase.Dawn) && track.Dawn;
					flag |= TimeManager.IsDayPhaseForMusic(DayPhase.Dusk) && track.Dusk;
					activeTrack.ActiveForDayPhase = flag;
					if (num != activeTrack.ActiveForDayPhase && !activeTrack.ForceMuted)
					{
						activeTrack.OnMuteStateChanged(2f);
					}
					break;
				}
			}
		}
		foreach (ActiveAmbienceTrack item in ActiveTracks.Where((ActiveAmbienceTrack x) => !tracks.Any((AmbienceTrack y) => y.Name == x.Track.Name)))
		{
			item.ForceMuted = true;
			item.OnMuteStateChanged(2f);
			ActiveAmbienceTrack t1 = item;
			Waiters.Wait(2.0, delegate
			{
				t1.Dispose();
				ActiveTracks.Remove(t1);
			});
		}
		foreach (AmbienceTrack item2 in tracks.Where((AmbienceTrack x) => !ActiveTracks.Any((ActiveAmbienceTrack y) => y.Track.Name == x.Name)))
		{
			bool flag2 = TimeManager.IsDayPhaseForMusic(DayPhase.Day) && item2.Day;
			flag2 |= TimeManager.IsDayPhaseForMusic(DayPhase.Night) && item2.Night;
			flag2 |= TimeManager.IsDayPhaseForMusic(DayPhase.Dawn) && item2.Dawn;
			flag2 |= TimeManager.IsDayPhaseForMusic(DayPhase.Dusk) && item2.Dusk;
			ActiveTracks.Add(new ActiveAmbienceTrack(item2, flag2));
		}
	}

	public void MuteTrack(string name, float fadeDuration)
	{
		foreach (ActiveAmbienceTrack activeTrack in ActiveTracks)
		{
			if (activeTrack.Track.Name == name)
			{
				activeTrack.ForceMuted = true;
				activeTrack.OnMuteStateChanged(fadeDuration);
				break;
			}
		}
	}

	public void UnmuteTrack(string name, float fadeDuration)
	{
		foreach (ActiveAmbienceTrack activeTrack in ActiveTracks)
		{
			if (activeTrack.Track.Name == name)
			{
				activeTrack.ForceMuted = false;
				activeTrack.OnMuteStateChanged(fadeDuration);
				break;
			}
		}
	}

	public void UnmuteTracks(bool apply)
	{
		foreach (ActiveAmbienceTrack activeTrack in ActiveTracks)
		{
			if (activeTrack.ForceMuted)
			{
				activeTrack.WasMuted = true;
			}
			activeTrack.ForceMuted = false;
			if (apply)
			{
				activeTrack.OnMuteStateChanged();
			}
		}
	}

	public void MuteTracks()
	{
		foreach (ActiveAmbienceTrack activeTrack in ActiveTracks)
		{
			activeTrack.ForceMuted = true;
			activeTrack.OnMuteStateChanged();
		}
	}

	protected override void Dispose(bool disposing)
	{
		foreach (ActiveAmbienceTrack activeTrack in ActiveTracks)
		{
			activeTrack.Dispose();
		}
		ActiveTracks.Clear();
		base.Enabled = false;
	}
}
