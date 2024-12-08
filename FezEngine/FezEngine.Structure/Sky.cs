using System.Collections.Generic;
using System.Linq;
using ContentSerialization.Attributes;

namespace FezEngine.Structure;

public class Sky
{
	public string Name { get; set; }

	public float WindSpeed { get; set; }

	public float Density { get; set; }

	public float FogDensity { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public List<string> Clouds { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public List<SkyLayer> Layers { get; set; }

	public string Background { get; set; }

	[Serialization(Optional = true)]
	public string Shadows { get; set; }

	[Serialization(Optional = true)]
	public string Stars { get; set; }

	[Serialization(Optional = true)]
	public string CloudTint { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool VerticalTiling { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool HorizontalScrolling { get; set; }

	[Serialization(Optional = true)]
	public float InterLayerVerticalDistance { get; set; }

	[Serialization(Optional = true)]
	public float InterLayerHorizontalDistance { get; set; }

	[Serialization(Optional = true)]
	public float HorizontalDistance { get; set; }

	[Serialization(Optional = true)]
	public float VerticalDistance { get; set; }

	[Serialization(Optional = true)]
	public float LayerBaseHeight { get; set; }

	[Serialization(Optional = true)]
	public float LayerBaseSpacing { get; set; }

	[Serialization(Optional = true)]
	public float WindParallax { get; set; }

	[Serialization(Optional = true)]
	public float WindDistance { get; set; }

	[Serialization(Optional = true)]
	public float CloudsParallax { get; set; }

	[Serialization(Optional = true)]
	public float ShadowOpacity { get; set; }

	[Serialization(Optional = true)]
	public bool FoliageShadows { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public bool NoPerFaceLayerXOffset { get; set; }

	[Serialization(Optional = true, DefaultValueOptional = true)]
	public float LayerBaseXOffset { get; set; }

	public Sky()
	{
		Name = "Default";
		Background = "SkyBack";
		Clouds = new List<string>();
		Layers = new List<SkyLayer>();
		WindSpeed = 1f;
		Density = 1f;
		FogDensity = 0.02f;
		LayerBaseHeight = 0.5f;
		CloudsParallax = 1f;
		ShadowOpacity = 0.7f;
	}

	public Sky ShallowCopy()
	{
		return new Sky
		{
			Name = Name,
			Background = Background,
			Clouds = new List<string>(Clouds),
			Layers = Layers.Select((SkyLayer x) => x.ShallowCopy()).ToList(),
			WindSpeed = WindSpeed,
			Density = Density,
			Shadows = Shadows,
			Stars = Stars,
			FogDensity = FogDensity,
			VerticalTiling = VerticalTiling,
			HorizontalScrolling = HorizontalScrolling,
			LayerBaseHeight = LayerBaseHeight,
			InterLayerVerticalDistance = InterLayerVerticalDistance,
			InterLayerHorizontalDistance = InterLayerHorizontalDistance,
			HorizontalDistance = HorizontalDistance,
			VerticalDistance = VerticalDistance,
			LayerBaseSpacing = LayerBaseSpacing,
			WindParallax = WindParallax,
			WindDistance = WindDistance,
			CloudsParallax = CloudsParallax,
			FoliageShadows = FoliageShadows,
			ShadowOpacity = ShadowOpacity,
			NoPerFaceLayerXOffset = NoPerFaceLayerXOffset,
			LayerBaseXOffset = LayerBaseXOffset
		};
	}

	public void UpdateFromCopy(Sky copy)
	{
		Name = copy.Name;
		Background = copy.Background;
		Clouds = new List<string>(copy.Clouds);
		Layers = copy.Layers.Select((SkyLayer x) => x.ShallowCopy()).ToList();
		WindSpeed = copy.WindSpeed;
		Density = copy.Density;
		Shadows = copy.Shadows;
		Stars = copy.Stars;
		FogDensity = copy.FogDensity;
		VerticalTiling = copy.VerticalTiling;
		HorizontalScrolling = copy.HorizontalScrolling;
		InterLayerVerticalDistance = copy.InterLayerVerticalDistance;
		InterLayerHorizontalDistance = copy.InterLayerHorizontalDistance;
		HorizontalDistance = copy.HorizontalDistance;
		VerticalDistance = copy.VerticalDistance;
		LayerBaseHeight = copy.LayerBaseHeight;
		LayerBaseSpacing = copy.LayerBaseSpacing;
		WindParallax = copy.WindParallax;
		WindDistance = copy.WindDistance;
		CloudsParallax = copy.CloudsParallax;
		FoliageShadows = copy.FoliageShadows;
		ShadowOpacity = copy.ShadowOpacity;
		NoPerFaceLayerXOffset = copy.NoPerFaceLayerXOffset;
		LayerBaseXOffset = copy.LayerBaseXOffset;
	}
}
