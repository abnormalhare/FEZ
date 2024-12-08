using System;
using Common;
using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public class TimeManager : ITimeManager
{
	private const float TransitionDivider = 3f;

	public static DateTime InitialTime = DateTime.Today.AddHours(12.0);

	public DateTime CurrentTime { get; set; }

	public float DefaultTimeFactor => 260f;

	public float DayFraction => (float)CurrentTime.TimeOfDay.TotalDays;

	public float TimeFactor { get; set; }

	public float NightContribution { get; private set; }

	public float DawnContribution { get; private set; }

	public float DuskContribution { get; private set; }

	public float CurrentAmbientFactor { get; set; }

	public Color CurrentFogColor { get; set; }

	public event Action Tick = Util.NullAction;

	public TimeManager()
	{
		TimeFactor = DefaultTimeFactor;
		CurrentTime = InitialTime;
		Tick += delegate
		{
			DawnContribution = Ease(DayFraction, DayPhase.Dawn.StartTime(), DayPhase.Dawn.Duration());
			DuskContribution = Ease(DayFraction, DayPhase.Dusk.StartTime(), DayPhase.Dusk.Duration());
			NightContribution = Ease(DayFraction, DayPhase.Night.StartTime(), DayPhase.Night.Duration());
			NightContribution = Math.Max(NightContribution, Ease(DayFraction, DayPhase.Night.StartTime() - 1f, DayPhase.Night.Duration()));
		};
	}

	private static float Ease(float value, float start, float duration)
	{
		float num = value - start;
		float num2 = duration / 3f;
		if (num < num2)
		{
			return FezMath.Saturate(num / num2);
		}
		if (num > 2f * num2)
		{
			return 1f - FezMath.Saturate((num - 2f * num2) / num2);
		}
		if (num < 0f || num > duration)
		{
			return 0f;
		}
		return 1f;
	}

	public void OnTick()
	{
		this.Tick();
	}

	public bool IsDayPhase(DayPhase phase)
	{
		float dayFraction = DayFraction;
		float num = phase.StartTime();
		float num2 = phase.EndTime();
		if (num < num2)
		{
			if (dayFraction >= num)
			{
				return dayFraction <= num2;
			}
			return false;
		}
		if (!(dayFraction >= num))
		{
			return dayFraction <= num2;
		}
		return true;
	}

	public bool IsDayPhaseForMusic(DayPhase phase)
	{
		float dayFraction = DayFraction;
		float num = phase.MusicStartTime();
		float num2 = phase.MusicEndTime();
		if (num < num2)
		{
			if (dayFraction >= num)
			{
				return dayFraction <= num2;
			}
			return false;
		}
		if (!(dayFraction >= num))
		{
			return dayFraction <= num2;
		}
		return true;
	}

	public float DayPhaseFraction(DayPhase phase)
	{
		float num = DayFraction - phase.StartTime();
		if (num < 1f)
		{
			num += 1f;
		}
		return num / phase.Duration();
	}
}
