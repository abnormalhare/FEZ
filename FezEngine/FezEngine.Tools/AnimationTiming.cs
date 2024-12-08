using System;
using System.Linq;

namespace FezEngine.Tools;

public class AnimationTiming
{
	public readonly float[] FrameTimings;

	public readonly int InitialFirstFrame;

	public readonly int InitialEndFrame;

	private readonly float stepPerFrame;

	private float startStep;

	private float endStep;

	private int startFrame;

	private int endFrame;

	public bool Loop { get; set; }

	public bool Paused { get; set; }

	public float Step { get; set; }

	public float NormalizedStep => (Step - startStep) / endStep;

	public int StartFrame
	{
		get
		{
			return startFrame;
		}
		set
		{
			startFrame = value;
			startStep = (float)startFrame / (float)FrameTimings.Length;
		}
	}

	public int EndFrame
	{
		get
		{
			return endFrame;
		}
		set
		{
			endFrame = value;
			endStep = (float)(endFrame + 1) / (float)FrameTimings.Length;
		}
	}

	public float StartStep => startStep;

	public float EndStep => endStep;

	public bool Ended
	{
		get
		{
			if (!Loop)
			{
				return FezMath.AlmostEqual(Step, endStep);
			}
			return false;
		}
	}

	public int Frame
	{
		get
		{
			return (int)Math.Floor(Step * (float)FrameTimings.Length);
		}
		set
		{
			Step = (float)value / (float)FrameTimings.Length;
		}
	}

	public float NextFrameContribution => FezMath.Frac(Step * (float)FrameTimings.Length);

	public AnimationTiming(float[] frameTimings)
		: this(0, frameTimings.Length - 1, loop: false, frameTimings)
	{
	}

	public AnimationTiming(int startFrame, float[] frameTimings)
		: this(startFrame, frameTimings.Length - 1, loop: false, frameTimings)
	{
	}

	public AnimationTiming(int startFrame, int endFrame, float[] frameTimings)
		: this(startFrame, endFrame, loop: false, frameTimings)
	{
	}

	public AnimationTiming(int startFrame, int endFrame, bool loop, float[] frameTimings)
	{
		Loop = loop;
		FrameTimings = frameTimings.Select((float x) => (x != 0f) ? x : 0.1f).ToArray();
		stepPerFrame = 1f / (float)frameTimings.Length;
		InitialFirstFrame = (StartFrame = startFrame);
		InitialEndFrame = (EndFrame = endFrame);
	}

	public void Restart()
	{
		Step = startStep;
		Paused = false;
	}

	public void Update(TimeSpan elapsed)
	{
		Update(elapsed, 1f);
	}

	public void Update(TimeSpan elapsed, float timeFactor)
	{
		if (Paused || Ended)
		{
			return;
		}
		int num = (int)Math.Floor(Step * (float)FrameTimings.Length);
		Step += (float)elapsed.TotalSeconds * timeFactor / FrameTimings[num] * stepPerFrame;
		while (Step >= endStep)
		{
			if (Loop)
			{
				Step -= endStep - startStep;
			}
			else
			{
				Step = endStep - 0.001f;
			}
		}
		while (Step < startStep)
		{
			if (Loop)
			{
				Step += endStep - startStep - 0.001f;
			}
			else
			{
				Step = startStep;
			}
		}
	}

	public void RandomizeStep()
	{
		Step = RandomHelper.Between(startStep, endStep);
	}

	public AnimationTiming Clone()
	{
		return new AnimationTiming(StartFrame, EndFrame, Loop, FrameTimings);
	}
}
