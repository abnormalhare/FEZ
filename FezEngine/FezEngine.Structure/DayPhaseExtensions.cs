using System;

namespace FezEngine.Structure;

public static class DayPhaseExtensions
{
	public static float StartTime(this DayPhase phase)
	{
		return phase switch
		{
			DayPhase.Dusk => 0.75f, 
			DayPhase.Day => 5f / 24f, 
			DayPhase.Dawn => 1f / 12f, 
			DayPhase.Night => 5f / 6f, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static float EndTime(this DayPhase phase)
	{
		return phase switch
		{
			DayPhase.Dusk => 11f / 12f, 
			DayPhase.Day => 5f / 6f, 
			DayPhase.Dawn => 0.25f, 
			DayPhase.Night => 1f / 6f, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static float MusicStartTime(this DayPhase phase)
	{
		return phase switch
		{
			DayPhase.Dusk => 19f / 24f, 
			DayPhase.Day => 5f / 24f, 
			DayPhase.Dawn => 1f / 12f, 
			DayPhase.Night => 0.875f, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static float MusicEndTime(this DayPhase phase)
	{
		return phase switch
		{
			DayPhase.Dusk => 0.875f, 
			DayPhase.Day => 19f / 24f, 
			DayPhase.Dawn => 5f / 24f, 
			DayPhase.Night => 1f / 12f, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static float Duration(this DayPhase phase)
	{
		float num = phase.EndTime();
		float num2 = phase.StartTime();
		if (num < num2)
		{
			num += 1f;
		}
		return num - num2;
	}
}
