using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Services;

public class FarawayTransitionSettings
{
	public bool InTransition;

	public bool LoadingAllowed;

	public float TransitionStep;

	public float OriginFadeOutStep;

	public float DestinationCrossfadeStep;

	public float InterpolatedFakeRadius;

	public float DestinationRadius;

	public float DestinationPixelsPerTrixel;

	public Vector2 DestinationOffset;

	public RenderTarget2D SkyRt;

	public void Reset()
	{
		OriginFadeOutStep = 0f;
		DestinationCrossfadeStep = 0f;
		TransitionStep = 0f;
		LoadingAllowed = (InTransition = false);
		InterpolatedFakeRadius = 0f;
		DestinationRadius = 0f;
		DestinationPixelsPerTrixel = 0f;
		DestinationOffset = Vector2.Zero;
		SkyRt = null;
	}
}
