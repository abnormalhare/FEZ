using FezEngine.Services;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezEngine.Components;

public class FpsMeasurer : DrawableGameComponent
{
	private double accumulatedTime;

	private int framesCounter;

	[ServiceDependency]
	public IEngineStateManager EngineState { private get; set; }

	public FpsMeasurer(Game game)
		: base(game)
	{
	}

	public override void Draw(GameTime gameTime)
	{
		accumulatedTime += gameTime.ElapsedGameTime.TotalSeconds;
		framesCounter++;
		if (accumulatedTime >= 1.0)
		{
			EngineState.FramesPerSecond = (float)((double)framesCounter / accumulatedTime);
			accumulatedTime -= 1.0;
			framesCounter = 0;
		}
	}
}
