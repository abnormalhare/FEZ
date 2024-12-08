using FezEngine.Structure;
using FezEngine.Tools;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FezEngine.Components;

public sealed class PlaneParticleSystemSettings
{
	public float SpawningSpeed { get; set; }

	public bool RandomizeSpawnTime { get; set; }

	public int SpawnBatchSize { get; set; }

	public bool NoLightDraw { get; set; }

	public BoundingBox SpawnVolume { get; set; }

	public VaryingVector3 SizeBirth { get; set; }

	public VaryingVector3 SizeDeath { get; set; }

	public Vector3 Gravity { get; set; }

	public Vector3? EnergySource { get; set; }

	public VaryingVector3 Velocity { get; set; }

	public float Acceleration { get; set; }

	public float SystemLifetime { get; set; }

	public float ParticleLifetime { get; set; }

	public VaryingColor ColorBirth { get; set; }

	public VaryingColor ColorLife { get; set; }

	public VaryingColor ColorDeath { get; set; }

	public float FadeInDuration { get; set; }

	public float FadeOutDuration { get; set; }

	public Texture2D Texture { get; set; }

	public BlendingMode BlendingMode { get; set; }

	public bool FullBright { get; set; }

	public bool Billboarding { get; set; }

	public bool Doublesided { get; set; }

	public FaceOrientation? Orientation { get; set; }

	public bool UseCallback { get; set; }

	public bool ClampToTrixels { get; set; }

	public StencilMask? StencilMask { get; set; }

	public PlaneParticleSystemSettings()
	{
		SizeBirth = new Vector3(0.0625f);
		SpawningSpeed = 1f;
		ParticleLifetime = 1f;
		BlendingMode = BlendingMode.Alphablending;
		FadeInDuration = 0.1f;
		FadeOutDuration = 0.9f;
		SpawnBatchSize = 1;
		ColorLife = Color.White;
		NoLightDraw = false;
		SizeDeath = new Vector3(-1f);
		Velocity = new VaryingVector3();
	}

	public PlaneParticleSystemSettings Clone()
	{
		return new PlaneParticleSystemSettings
		{
			Acceleration = Acceleration,
			Billboarding = Billboarding,
			BlendingMode = BlendingMode,
			ColorBirth = ColorBirth,
			ColorLife = ColorLife,
			ColorDeath = ColorDeath,
			Doublesided = Doublesided,
			EnergySource = EnergySource,
			FadeInDuration = FadeInDuration,
			FadeOutDuration = FadeOutDuration,
			FullBright = FullBright,
			Gravity = Gravity,
			ParticleLifetime = ParticleLifetime,
			RandomizeSpawnTime = RandomizeSpawnTime,
			SizeBirth = SizeBirth,
			SizeDeath = SizeDeath,
			SpawnBatchSize = SpawnBatchSize,
			SpawningSpeed = SpawningSpeed,
			SpawnVolume = SpawnVolume,
			SystemLifetime = SystemLifetime,
			Texture = Texture,
			Velocity = Velocity,
			UseCallback = UseCallback,
			ClampToTrixels = ClampToTrixels,
			Orientation = Orientation,
			StencilMask = StencilMask,
			NoLightDraw = NoLightDraw
		};
	}
}
