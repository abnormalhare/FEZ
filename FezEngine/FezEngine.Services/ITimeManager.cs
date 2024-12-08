using System;
using FezEngine.Structure;
using Microsoft.Xna.Framework;

namespace FezEngine.Services;

public interface ITimeManager
{
	DateTime CurrentTime { get; set; }

	float TimeFactor { get; set; }

	float DayFraction { get; }

	float NightContribution { get; }

	float DawnContribution { get; }

	float DuskContribution { get; }

	float CurrentAmbientFactor { get; set; }

	Color CurrentFogColor { get; set; }

	float DefaultTimeFactor { get; }

	event Action Tick;

	void OnTick();

	bool IsDayPhase(DayPhase phase);

	bool IsDayPhaseForMusic(DayPhase phase);

	float DayPhaseFraction(DayPhase phase);
}
