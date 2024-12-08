using FezEngine.Components.Scripting;
using FezEngine.Services;
using FezEngine.Services.Scripting;
using FezEngine.Tools;
using Microsoft.Xna.Framework;

namespace FezGame.Services.Scripting;

internal class PlaneService : IPlaneService, IScriptingBase
{
	[ServiceDependency]
	public ILevelManager LevelManager { get; set; }

	public void ResetEvents()
	{
	}

	public LongRunningAction FadeIn(int id, float seconds)
	{
		float wasOpacity = LevelManager.BackgroundPlanes[id].Opacity;
		return new LongRunningAction(delegate(float _, float elapsedSeconds)
		{
			if (!LevelManager.BackgroundPlanes.TryGetValue(id, out var value))
			{
				return true;
			}
			value.Opacity = MathHelper.Lerp(wasOpacity, 1f, FezMath.Saturate(elapsedSeconds / seconds));
			return value.Opacity == 1f;
		});
	}

	public LongRunningAction FadeOut(int id, float seconds)
	{
		float wasOpacity = LevelManager.BackgroundPlanes[id].Opacity;
		return new LongRunningAction(delegate(float _, float elapsedSeconds)
		{
			if (!LevelManager.BackgroundPlanes.TryGetValue(id, out var value))
			{
				return true;
			}
			value.Opacity = MathHelper.Lerp(wasOpacity, 0f, FezMath.Saturate(elapsedSeconds / seconds));
			return value.Opacity == 0f;
		});
	}

	public LongRunningAction Flicker(int id, float factor)
	{
		Vector3 baseScale = LevelManager.BackgroundPlanes[id].Scale;
		return new LongRunningAction(delegate
		{
			if (RandomHelper.Probability(0.25))
			{
				if (!LevelManager.BackgroundPlanes.TryGetValue(id, out var value))
				{
					return true;
				}
				value.Scale = baseScale + new Vector3(RandomHelper.Centered(factor));
			}
			return false;
		});
	}
}
