using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Common;
using FezEngine.Services;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

public class ActiveTrackedSong : GameComponent
{
	public const float DefaultFadeDuration = 2f;

	private IList<string> mutedLoops;

	public TrackedSong Song;

	private long BeatsCounted;

	private long BarsCounted;

	private double LastTotalMinutes;

	private readonly Stopwatch Watch = new Stopwatch();

	private readonly List<ActiveLoop> ActiveLoops = new List<ActiveLoop>();

	private ActiveLoop[] AllOaaTs;

	private ActiveLoop CurrentOaaT;

	private ActiveLoop NextOaaT;

	private int OoaTIndex = -1;

	private bool cancelPause;

	private bool resumeRequested;

	public bool IgnoreDayPhase { get; set; }

	public int CurrentBeat => (int)(BeatsCounted % Song.TimeSignature);

	public int CurrentBar => (int)BarsCounted;

	public TimeSpan PlayPosition => Watch.Elapsed;

	public IList<string> MutedLoops
	{
		get
		{
			return mutedLoops;
		}
		set
		{
			mutedLoops = value;
			foreach (ActiveLoop activeLoop in ActiveLoops)
			{
				if (!activeLoop.Muted && mutedLoops.Contains(activeLoop.Loop.Name))
				{
					activeLoop.Muted = true;
					activeLoop.OnMuteStateChanged();
				}
			}
			foreach (ActiveLoop activeLoop2 in ActiveLoops)
			{
				if (activeLoop2.Muted && !mutedLoops.Contains(activeLoop2.Loop.Name))
				{
					activeLoop2.Muted = false;
					activeLoop2.OnMuteStateChanged();
				}
			}
		}
	}

	[ServiceDependency]
	public ISoundManager SoundManager { private get; set; }

	[ServiceDependency]
	public ILevelManager LevelManager { private get; set; }

	[ServiceDependency]
	public ITimeManager TimeManager { private get; set; }

	public event Action Beat = Util.NullAction;

	public event Action Bar = Util.NullAction;

	public ActiveTrackedSong(Game game)
		: base(game)
	{
	}

	public ActiveTrackedSong(Game game, TrackedSong song, IList<string> mutedLoops)
		: this(game)
	{
		Song = song;
		MutedLoops = mutedLoops;
	}

	public override void Initialize()
	{
		if (Song == null)
		{
			Song = LevelManager.Song;
		}
		if (MutedLoops == null)
		{
			MutedLoops = LevelManager.MutedLoops;
		}
		if (Song == null)
		{
			base.Enabled = false;
			ServiceHelper.RemoveComponent(this);
			return;
		}
		BarsCounted = (BeatsCounted = 0L);
		base.Enabled = false;
		Waiters.Wait(0.1, delegate
		{
			foreach (Loop loop in Song.Loops)
			{
				bool flag = IgnoreDayPhase;
				if (!IgnoreDayPhase)
				{
					flag |= TimeManager.IsDayPhaseForMusic(DayPhase.Day) && loop.Day;
					flag |= TimeManager.IsDayPhaseForMusic(DayPhase.Night) && loop.Night;
					flag |= TimeManager.IsDayPhaseForMusic(DayPhase.Dawn) && loop.Dawn;
					flag |= TimeManager.IsDayPhaseForMusic(DayPhase.Dusk) && loop.Dusk;
				}
				ActiveLoop activeLoop = new ActiveLoop(loop, MutedLoops.Contains(loop.Name), flag, loop.OneAtATime);
				if (loop.OneAtATime)
				{
					activeLoop.CycleLink = CycleOaaTs;
				}
				ActiveLoops.Add(activeLoop);
			}
			AllOaaTs = ActiveLoops.Where((ActiveLoop x) => x.Loop.OneAtATime).ToArray();
			if (Song.Loops.Count > 0 && AllOaaTs.Length != 0)
			{
				CycleOaaTs();
			}
			base.Enabled = true;
			Watch.Start();
		});
	}

	private void CycleOaaTs()
	{
		if (CurrentOaaT != null && CurrentOaaT.Loop.CutOffTail && !CurrentOaaT.WaitedForDelay)
		{
			CurrentOaaT.CutOff();
		}
		CurrentOaaT = NextOaaT;
		if (CurrentOaaT != null)
		{
			CurrentOaaT.ActiveForOoaTs = false;
		}
		int num;
		if (!Song.RandomOrdering)
		{
			num = ((Song.CustomOrdering != null && Song.CustomOrdering.Length != 0) ? (Song.CustomOrdering[OoaTIndex = (OoaTIndex + 1) % Song.CustomOrdering.Length] - 1) : ((ActiveLoops.IndexOf(CurrentOaaT) + 1) % ActiveLoops.Count));
		}
		else
		{
			num = RandomHelper.Random.Next(0, AllOaaTs.Length);
			if (CurrentOaaT != null && num == ActiveLoops.IndexOf(CurrentOaaT))
			{
				num = RandomHelper.Random.Next(0, AllOaaTs.Length);
			}
		}
		NextOaaT = AllOaaTs[num];
		NextOaaT.ActiveForOoaTs = true;
		NextOaaT.SchedulePlay();
		if (CurrentOaaT == null)
		{
			NextOaaT.ForcePlay();
			if (NextOaaT.Loop.Delay == 0)
			{
				CycleOaaTs();
			}
		}
	}

	public override void Update(GameTime gameTime)
	{
		double totalMinutes = Watch.Elapsed.TotalMinutes;
		foreach (ActiveLoop activeLoop in ActiveLoops)
		{
			if (activeLoop.Loop.FractionalTime)
			{
				float totalBars = (float)((totalMinutes - LastTotalMinutes) * (double)Song.Tempo / (double)Song.TimeSignature);
				activeLoop.UpdateFractional(totalBars);
			}
			activeLoop.UpdatePrecache();
		}
		LastTotalMinutes = totalMinutes;
		double num = Math.Floor(totalMinutes * (double)Song.Tempo);
		if (num > (double)BeatsCounted)
		{
			BeatsCounted = (int)num;
			OnBeat();
			long num2 = BeatsCounted / Song.TimeSignature;
			if (num2 > BarsCounted)
			{
				BarsCounted = num2;
				OnBar();
			}
		}
		if (IgnoreDayPhase)
		{
			return;
		}
		bool flag = TimeManager.IsDayPhaseForMusic(DayPhase.Day);
		bool flag2 = TimeManager.IsDayPhaseForMusic(DayPhase.Dawn);
		bool flag3 = TimeManager.IsDayPhaseForMusic(DayPhase.Dusk);
		bool flag4 = TimeManager.IsDayPhaseForMusic(DayPhase.Night);
		foreach (ActiveLoop activeLoop2 in ActiveLoops)
		{
			bool activeForDayPhase = activeLoop2.ActiveForDayPhase;
			activeLoop2.ActiveForDayPhase = false;
			activeLoop2.ActiveForDayPhase |= flag && activeLoop2.Loop.Day;
			activeLoop2.ActiveForDayPhase |= flag4 && activeLoop2.Loop.Night;
			activeLoop2.ActiveForDayPhase |= flag2 && activeLoop2.Loop.Dawn;
			activeLoop2.ActiveForDayPhase |= flag3 && activeLoop2.Loop.Dusk;
			if (activeForDayPhase != activeLoop2.ActiveForDayPhase)
			{
				activeLoop2.OnMuteStateChanged(16f);
			}
		}
	}

	public void Pause()
	{
		if (!base.Enabled)
		{
			return;
		}
		resumeRequested = false;
		Waiters.Interpolate(0.25, delegate(float step)
		{
			cancelPause |= resumeRequested || !base.Enabled;
			if (!cancelPause)
			{
				float num = Easing.EaseOut(step, EasingType.Sine);
				SoundManager.MusicVolumeFactor = FezMath.Saturate(1f - num);
			}
		}, delegate
		{
			if (!cancelPause && !resumeRequested)
			{
				Watch.Stop();
				foreach (ActiveLoop activeLoop in ActiveLoops)
				{
					activeLoop.Pause();
				}
				base.Enabled = false;
				SoundManager.MusicVolumeFactor = 1f;
			}
			if (resumeRequested)
			{
				SoundManager.MusicVolumeFactor = 1f;
			}
			cancelPause = (resumeRequested = false);
		});
	}

	public void Resume()
	{
		if (base.Enabled)
		{
			resumeRequested = true;
		}
		else
		{
			if (Watch == null)
			{
				return;
			}
			Watch.Start();
			foreach (ActiveLoop activeLoop in ActiveLoops)
			{
				activeLoop.Resume();
			}
			base.Enabled = true;
			Waiters.Interpolate(0.125, delegate(float step)
			{
				if (base.Enabled)
				{
					float value = Easing.EaseOut(step, EasingType.Sine);
					SoundManager.MusicVolumeFactor = FezMath.Saturate(value);
					resumeRequested = false;
				}
			}, delegate
			{
				SoundManager.MusicVolumeFactor = 1f;
			});
		}
	}

	private void OnBeat()
	{
		this.Beat();
	}

	private void OnBar()
	{
		foreach (ActiveLoop activeLoop in ActiveLoops)
		{
			if (!activeLoop.Loop.FractionalTime)
			{
				activeLoop.OnBar();
			}
		}
		this.Bar();
	}

	public void SetMutedLoops(IList<string> loops, float fadeDuration)
	{
		mutedLoops = loops;
		foreach (ActiveLoop activeLoop in ActiveLoops)
		{
			if (!activeLoop.Muted && mutedLoops.Contains(activeLoop.Loop.Name))
			{
				activeLoop.Muted = true;
				activeLoop.OnMuteStateChanged(fadeDuration);
			}
		}
		foreach (ActiveLoop activeLoop2 in ActiveLoops)
		{
			if (activeLoop2.Muted && !mutedLoops.Contains(activeLoop2.Loop.Name))
			{
				activeLoop2.Muted = false;
				activeLoop2.OnMuteStateChanged(fadeDuration);
			}
		}
	}

	public void ReInitialize(IList<string> newMutedLoops)
	{
		mutedLoops = newMutedLoops;
		Dispose(disposing: false);
		Initialize();
	}

	protected override void Dispose(bool disposing)
	{
		foreach (ActiveLoop activeLoop in ActiveLoops)
		{
			activeLoop.Dispose();
		}
		ActiveLoops.Clear();
		base.Enabled = false;
		if (disposing)
		{
			this.Beat = null;
			this.Bar = null;
		}
	}

	public void FadeOutAndRemoveComponent()
	{
		FadeOutAndRemoveComponent(2f);
	}

	public void FadeOutAndRemoveComponent(float fadeDuration)
	{
		if (!base.Enabled)
		{
			return;
		}
		foreach (ActiveLoop activeLoop in ActiveLoops)
		{
			activeLoop.Muted = true;
			activeLoop.OnMuteStateChanged(fadeDuration);
		}
		base.Enabled = false;
		Waiters.Wait(fadeDuration, delegate
		{
			ServiceHelper.RemoveComponent(this);
		});
	}
}
