namespace FezEngine.Components;

internal static class CloudLayerExtensions
{
	public static float SpeedFactor(this Layer layer)
	{
		return layer switch
		{
			Layer.Near => 1f, 
			Layer.Middle => 0.6f, 
			_ => 0.2f, 
		};
	}

	public static float DistanceFactor(this Layer layer)
	{
		return layer switch
		{
			Layer.Near => 0f, 
			Layer.Middle => 0.5f, 
			_ => 1f, 
		};
	}

	public static float ParallaxFactor(this Layer layer)
	{
		return layer switch
		{
			Layer.Near => 0.2f, 
			Layer.Middle => 0.4f, 
			_ => 0.6f, 
		};
	}

	public static float Opacity(this Layer layer)
	{
		return layer switch
		{
			Layer.Near => 1f, 
			Layer.Middle => 0.6f, 
			_ => 0.3f, 
		};
	}
}
