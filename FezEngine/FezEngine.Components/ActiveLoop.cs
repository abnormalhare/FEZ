using System;
using System.Collections.Generic;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;

namespace FezEngine.Components;

internal class ActiveLoop
{
	public readonly Loop Loop;

	private float fractionalBar;

	private int barsToCount;

	private int loopsToPlay;

	private float barsBeforePlay;

	private bool nextCuePrecached;

	private bool playing;

	private readonly List<OggStream> strayCues = new List<OggStream>();

	private OggStream currentCue;

	private OggStream nextCue;

	private float volume;

	private IWaiter transitionWaiter;

	public bool WaitedForDelay;

	public bool ActiveForDayPhase { get; set; }

	public bool Muted { get; set; }

	public bool ActiveForOoaTs { get; set; }

	public Action CycleLink { get; set; }

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	public ActiveLoop(Loop loop, bool muted, bool activeForDayPhase, bool dontStart)
	{
		ServiceHelper.InjectServices(this);
		Muted = muted;
		volume = ((!muted && activeForDayPhase) ? 1 : 0);
		ActiveForDayPhase = activeForDayPhase;
		OnMuteStateChanged();
		Loop = loop;
		nextCuePrecached = true;
		nextCue = SoundManager.GetCue(loop.Name);
		barsBeforePlay = Loop.Delay;
		if (!dontStart && Loop.Delay == 0)
		{
			FirstPlay();
		}
	}

	public void UpdateFractional(float totalBars)
	{
		if (Loop.OneAtATime && !ActiveForOoaTs)
		{
			return;
		}
		fractionalBar += totalBars;
		if (fractionalBar >= 1f)
		{
			OnBar();
			fractionalBar -= 1f;
		}
		if (!playing && barsBeforePlay <= fractionalBar)
		{
			barsBeforePlay -= fractionalBar;
			fractionalBar = 0f;
			if (Loop.OneAtATime)
			{
				CycleLink();
			}
			WaitedForDelay = false;
			FirstPlay();
		}
	}

	public void UpdatePrecache()
	{
		if (!nextCuePrecached && strayCues.Count == 0)
		{
			nextCue = SoundManager.GetCue(Loop.Name, asyncPrecache: true);
			nextCuePrecached = true;
		}
	}

	public void OnBar()
	{
		if (Loop.OneAtATime && !ActiveForOoaTs)
		{
			return;
		}
		if (playing)
		{
			barsToCount--;
			if (barsToCount == 0)
			{
				loopsToPlay--;
				if (loopsToPlay == 0)
				{
					playing = false;
					if (Loop.FractionalTime)
					{
						barsBeforePlay = RandomHelper.Between(Loop.TriggerFrom, Loop.TriggerTo);
					}
					else
					{
						barsBeforePlay = RandomHelper.Random.Next(Loop.TriggerFrom, Loop.TriggerTo + 1);
					}
					if (barsBeforePlay == 0f)
					{
						FirstPlay();
					}
				}
				else
				{
					Play();
				}
			}
		}
		else
		{
			barsBeforePlay -= 1f;
			if (barsBeforePlay <= 0f)
			{
				if (Loop.OneAtATime)
				{
					CycleLink();
				}
				WaitedForDelay = false;
				FirstPlay();
			}
		}
		for (int num = strayCues.Count - 1; num >= 0; num--)
		{
			if (strayCues[num].IsStopped)
			{
				strayCues[num].Dispose();
				strayCues.RemoveAt(num);
			}
		}
	}

	public void SchedulePlay()
	{
		playing = false;
		if (Loop.FractionalTime)
		{
			barsBeforePlay = RandomHelper.Between(Loop.TriggerFrom, Loop.TriggerTo);
		}
		else
		{
			barsBeforePlay = RandomHelper.Random.Next(Loop.TriggerFrom, Loop.TriggerTo + 1);
		}
	}

	public void ForcePlay()
	{
		barsBeforePlay = Loop.Delay;
		if (Loop.Delay == 0)
		{
			FirstPlay();
		}
		else
		{
			WaitedForDelay = true;
		}
	}

	private void FirstPlay()
	{
		playing = true;
		loopsToPlay = RandomHelper.Random.Next(Loop.LoopTimesFrom, Loop.LoopTimesTo + 1);
		Play();
	}

	private void Play()
	{
		if (!nextCuePrecached)
		{
			nextCue = SoundManager.GetCue(Loop.Name);
		}
		nextCue.Volume = volume;
		nextCue.Play();
		if (currentCue != null)
		{
			strayCues.Add(currentCue);
		}
		currentCue = nextCue;
		barsToCount = Loop.Duration;
		nextCuePrecached = false;
	}

	public void Dispose()
	{
		if (currentCue != null)
		{
			currentCue.Stop();
			currentCue.Dispose();
			currentCue = null;
		}
		nextCue.Stop();
		nextCue.Dispose();
		nextCue = null;
		foreach (OggStream strayCue in strayCues)
		{
			strayCue.Stop();
			strayCue.Dispose();
		}
		strayCues.Clear();
		CycleLink = null;
	}

	public void Pause()
	{
		if (currentCue != null && !currentCue.IsDisposed)
		{
			currentCue.Pause();
		}
	}

	public void Resume()
	{
		if (currentCue != null && !currentCue.IsDisposed)
		{
			currentCue.Resume();
		}
	}

	public void CutOff()
	{
		if (currentCue != null)
		{
			currentCue.Stop();
			currentCue.Dispose();
		}
		currentCue = null;
	}

	public void OnMuteStateChanged()
	{
		OnMuteStateChanged(2f);
	}

	public void OnMuteStateChanged(float fadeDuration)
	{
		if (ActiveForDayPhase && !Muted && volume != 1f)
		{
			float originalVolume = volume;
			IWaiter thisWaiter = null;
			volume -= 0.001f;
			transitionWaiter = (thisWaiter = Waiters.Interpolate(fadeDuration * (1f - volume), delegate(float s)
			{
				if (transitionWaiter == thisWaiter)
				{
					volume = Easing.EaseOut(originalVolume + s * (1f - originalVolume), EasingType.Sine);
					if (currentCue != null && !currentCue.IsDisposed)
					{
						currentCue.Volume = volume;
					}
				}
			}, delegate
			{
				if (transitionWaiter == thisWaiter)
				{
					volume = 1f;
					if (currentCue != null && !currentCue.IsDisposed)
					{
						currentCue.Volume = volume;
					}
				}
			}));
		}
		else
		{
			if ((ActiveForDayPhase && !Muted) || volume == 0f)
			{
				return;
			}
			float originalVolume2 = volume;
			IWaiter thisWaiter2 = null;
			volume += 0.001f;
			transitionWaiter = (thisWaiter2 = Waiters.Interpolate(fadeDuration * volume, delegate(float s)
			{
				if (transitionWaiter == thisWaiter2)
				{
					volume = Easing.EaseOut(originalVolume2 * (1f - s), EasingType.Sine);
					if (currentCue != null && !currentCue.IsDisposed)
					{
						currentCue.Volume = volume;
					}
				}
			}, delegate
			{
				if (transitionWaiter == thisWaiter2)
				{
					volume = 0f;
					if (currentCue != null && !currentCue.IsDisposed)
					{
						currentCue.Volume = volume;
					}
				}
			}));
		}
	}
}
