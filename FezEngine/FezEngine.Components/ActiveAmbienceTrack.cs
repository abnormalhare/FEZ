using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;

namespace FezEngine.Components;

internal class ActiveAmbienceTrack
{
	public AmbienceTrack Track;

	private readonly OggStream cue;

	private float volume;

	private IWaiter transitionWaiter;

	public bool ActiveForDayPhase { get; set; }

	public bool ForceMuted { get; set; }

	public bool WasMuted { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	public ActiveAmbienceTrack(AmbienceTrack track, bool activeForDayPhase)
	{
		ServiceHelper.InjectServices(this);
		volume = 0f;
		ActiveForDayPhase = activeForDayPhase;
		OnMuteStateChanged();
		Track = track;
		cue = SoundManager.GetCue(Track.Name);
		cue.Volume = volume;
		cue.Play();
	}

	public void Dispose()
	{
		if (cue != null && !cue.IsDisposed)
		{
			cue.Stop();
			cue.Dispose();
		}
	}

	public void Pause()
	{
		if (cue != null && !cue.IsDisposed)
		{
			cue.Pause();
		}
	}

	public void Resume()
	{
		if (cue != null && !cue.IsDisposed)
		{
			cue.Resume();
		}
	}

	public void OnMuteStateChanged()
	{
		OnMuteStateChanged(2f);
	}

	public void OnMuteStateChanged(float fadeDuration)
	{
		if (ActiveForDayPhase && !ForceMuted && volume != 1f)
		{
			float originalVolume = volume;
			IWaiter thisWaiter = null;
			volume += 0.001f;
			transitionWaiter = (thisWaiter = Waiters.Interpolate(fadeDuration * (1f - volume), delegate(float s)
			{
				if (transitionWaiter == thisWaiter)
				{
					volume = originalVolume + s * (1f - originalVolume);
					if (cue != null && !cue.IsDisposed)
					{
						cue.Volume = volume;
					}
				}
			}, delegate
			{
				if (transitionWaiter == thisWaiter)
				{
					volume = 1f;
					if (cue != null && !cue.IsDisposed)
					{
						cue.Volume = volume;
					}
				}
			}));
		}
		else
		{
			if ((ActiveForDayPhase && !ForceMuted) || volume == 0f)
			{
				return;
			}
			float originalVolume2 = volume;
			IWaiter thisWaiter2 = null;
			volume -= 0.001f;
			transitionWaiter = (thisWaiter2 = Waiters.Interpolate(fadeDuration * volume, delegate(float s)
			{
				if (transitionWaiter == thisWaiter2)
				{
					volume = originalVolume2 * (1f - s);
					if (cue != null && !cue.IsDisposed)
					{
						cue.Volume = volume;
					}
				}
			}, delegate
			{
				if (transitionWaiter == thisWaiter2)
				{
					volume = 0f;
					if (cue != null && !cue.IsDisposed)
					{
						cue.Volume = volume;
					}
				}
			}));
		}
	}
}
